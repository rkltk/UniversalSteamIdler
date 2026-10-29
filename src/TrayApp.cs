using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace UniversalSteamIdler
{
    public class TrayApp : ApplicationContext
    {
        private const string RegRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RegRunVal = "UniversalSteamIdler";

        private readonly AppConfig _config;
        private readonly string _configPath;
        private readonly string _exePath;
        private readonly WorkerManager _workerManager;

        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private ToolStripMenuItem _headerItem;
        private ToolStripMenuItem _statusItem;
        private ToolStripMenuItem _quickGamesMenu;
        private ToolStripMenuItem _actionPauseResume;
        private ToolStripMenuItem _autoStartItem;
        private SettingsForm _settingsForm;

        private Icon _iconActive;
        private Icon _iconWaiting;
        private Icon _iconPaused;

        private System.Windows.Forms.Timer _timer;
        private int _countdownSeconds = 0;
        private bool _isCountdownActive = false;

        public TrayApp(AppConfig config, string configPath, string exePath, bool startupMode)
        {
            _config = config;
            _configPath = configPath;
            _exePath = exePath;
            _workerManager = new WorkerManager();

            InitIcons();
            InitTray();

            if (startupMode && _config.StartupDelaySeconds > 0)
            {
                _countdownSeconds = _config.StartupDelaySeconds;
                _isCountdownActive = true;
            }
            else
            {
                _isCountdownActive = false;
                _workerManager.Sync(_config.Games, _exePath);
            }

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 1000;
            _timer.Tick += Timer_Tick;
            _timer.Start();

            UpdateTrayDisplay();

            if (!startupMode && !_config.StartMinimized)
            {
                OpenSettings();
            }
        }

        private void InitIcons()
        {
            _iconActive = CreateIcon(Color.FromArgb(16, 24, 38), Color.FromArgb(74, 222, 128), "S"); // Green
            _iconWaiting = CreateIcon(Color.FromArgb(16, 24, 38), Color.FromArgb(250, 204, 21), "S"); // Yellow
            _iconPaused = CreateIcon(Color.FromArgb(16, 24, 38), Color.FromArgb(156, 163, 175), "S"); // Gray
        }

        private Icon CreateIcon(Color bg, Color fg, string text)
        {
            try
            {
                using (Bitmap bmp = new Bitmap(32, 32))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    using (Brush bgBrush = new SolidBrush(bg))
                    {
                        g.FillEllipse(bgBrush, 1, 1, 30, 30);
                    }

                    using (Pen ringPen = new Pen(fg, 2f))
                    {
                        g.DrawEllipse(ringPen, 2, 2, 28, 28);
                    }

                    using (Font font = new Font("Segoe UI", 13.5f, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (Brush textBrush = new SolidBrush(fg))
                    {
                        StringFormat sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };
                        g.DrawString(text, font, textBrush, new RectangleF(0, 1, 32, 32), sf);
                    }

                    IntPtr hIcon = bmp.GetHicon();
                    return Icon.FromHandle(hIcon);
                }
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private void InitTray()
        {
            _contextMenu = new ContextMenuStrip();

            _headerItem = new ToolStripMenuItem("🎮 Universal Steam Idler");
            _headerItem.Enabled = false;
            _headerItem.Font = new Font(_headerItem.Font, FontStyle.Bold);

            _statusItem = new ToolStripMenuItem("Статус: Запуск...");
            _statusItem.Enabled = false;

            ToolStripMenuItem openSettingsItem = new ToolStripMenuItem("⚙ Настройки и список игр...");
            openSettingsItem.Font = new Font(openSettingsItem.Font, FontStyle.Bold);
            openSettingsItem.Click += (s, e) => OpenSettings();

            _quickGamesMenu = new ToolStripMenuItem("🎮 Быстрый выбор игры");

            ToolStripMenuItem scanItem = new ToolStripMenuItem("🔍 Сканировать игры Steam...");
            scanItem.Click += (s, e) =>
            {
                OpenSettings();
                // User can click scan in settings
            };

            _actionPauseResume = new ToolStripMenuItem("⏸ Поставить на паузу");
            _actionPauseResume.Click += ActionPauseResume_Click;

            _autoStartItem = new ToolStripMenuItem("✔ Автозапуск с Windows");
            _autoStartItem.CheckOnClick = false;
            _autoStartItem.Checked = IsAutoStartInRegistry();
            _autoStartItem.Click += AutoStartItem_Click;

            ToolStripMenuItem openFolderItem = new ToolStripMenuItem("📁 Открыть папку программы");
            openFolderItem.Click += (s, e) =>
            {
                try { Process.Start("explorer.exe", Path.GetDirectoryName(_exePath)); } catch { }
            };

            ToolStripMenuItem exitItem = new ToolStripMenuItem("❌ Выход (остановить всё)");
            exitItem.Click += (s, e) => ExitApplication();

            _contextMenu.Items.Add(_headerItem);
            _contextMenu.Items.Add(_statusItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(openSettingsItem);
            _contextMenu.Items.Add(_quickGamesMenu);
            _contextMenu.Items.Add(scanItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(_actionPauseResume);
            _contextMenu.Items.Add(_autoStartItem);
            _contextMenu.Items.Add(openFolderItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(exitItem);

            _notifyIcon = new NotifyIcon
            {
                ContextMenuStrip = _contextMenu,
                Icon = _iconWaiting,
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) => OpenSettings();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_isCountdownActive)
            {
                _countdownSeconds--;
                if (_countdownSeconds <= 0)
                {
                    _isCountdownActive = false;
                    _workerManager.Sync(_config.Games, _exePath);
                    if (_config.ShowNotifications)
                    {
                        _notifyIcon.ShowBalloonTip(4000, "Universal Steam Idler", "Автозапуск завершен, фарм запущен!", ToolTipIcon.Info);
                    }
                }
            }
            else if (!_workerManager.IsPaused)
            {
                _workerManager.Sync(_config.Games, _exePath);
            }

            UpdateTrayDisplay();
            if (_settingsForm != null && _settingsForm.Visible)
            {
                _settingsForm.UpdateStatusDisplay();
            }
        }

        private void UpdateTrayDisplay()
        {
            List<string> running = _workerManager.GetRunningAppIds();
            string tooltip = "";

            if (_isCountdownActive)
            {
                int mins = _countdownSeconds / 60;
                int secs = _countdownSeconds % 60;
                string timeStr = string.Format("{0:D2}:{1:D2}", mins, secs);

                _notifyIcon.Icon = _iconWaiting;
                _statusItem.Text = "⏳ Старт через: " + timeStr;
                _actionPauseResume.Text = "⚡ Запустить прямо сейчас";
                tooltip = "Steam Idler: запуск через " + timeStr;
            }
            else if (_workerManager.IsPaused)
            {
                _notifyIcon.Icon = _iconPaused;
                _statusItem.Text = "○ На паузе";
                _actionPauseResume.Text = "▶ Возобновить фарм";
                tooltip = "Universal Steam Idler: на паузе";
            }
            else if (running.Count > 0)
            {
                _notifyIcon.Icon = _iconActive;
                _statusItem.Text = string.Format("● Фармится: {0} {1}", running.Count, running.Count == 1 ? "игра" : "игр(ы)");
                _actionPauseResume.Text = "⏸ Поставить на паузу";

                List<string> names = new List<string>();
                foreach (GameItem g in _config.Games)
                {
                    if (running.Contains(g.AppId)) names.Add(g.Name);
                }
                string nStr = string.Join(", ", names.ToArray());
                tooltip = "Фарм: " + nStr;
            }
            else
            {
                _notifyIcon.Icon = _iconWaiting;
                _statusItem.Text = "⏳ Ожидание Steam или выбора игры";
                _actionPauseResume.Text = "⏸ Поставить на паузу";
                tooltip = "Steam Idler: ожидание запуска...";
            }

            if (tooltip.Length > 63)
            {
                tooltip = tooltip.Substring(0, 60) + "...";
            }
            _notifyIcon.Text = tooltip;

            // Rebuild Quick Games Submenu
            _quickGamesMenu.DropDownItems.Clear();
            foreach (GameItem g in _config.Games)
            {
                ToolStripMenuItem gameItem = new ToolStripMenuItem(string.Format("{0} ({1})", g.Name, g.AppId))
                {
                    Checked = g.IsActive
                };
                GameItem closure = g;
                gameItem.Click += (s, e) =>
                {
                    closure.IsActive = !closure.IsActive;
                    _config.Save(_configPath);
                    _workerManager.Sync(_config.Games, _exePath);
                    UpdateTrayDisplay();
                    if (_settingsForm != null && _settingsForm.Visible)
                    {
                        _settingsForm.RefreshGrid();
                    }
                };
                _quickGamesMenu.DropDownItems.Add(gameItem);
            }
        }

        private void ActionPauseResume_Click(object sender, EventArgs e)
        {
            if (_isCountdownActive)
            {
                _isCountdownActive = false;
                _countdownSeconds = 0;
                _workerManager.Sync(_config.Games, _exePath);
            }
            else if (_workerManager.IsPaused)
            {
                _workerManager.Resume(_config.Games, _exePath);
            }
            else
            {
                _workerManager.Pause();
            }
            UpdateTrayDisplay();
        }

        private void AutoStartItem_Click(object sender, EventArgs e)
        {
            bool current = IsAutoStartInRegistry();
            SetAutoStartRegistry(!current);
            _autoStartItem.Checked = !current;
            _config.AutoStart = !current;
            _config.Save(_configPath);

            string msg = !current ? "Автозапуск включен (через 5 минут после старта системы)." : "Автозапуск выключен.";
            _notifyIcon.ShowBalloonTip(3000, "Universal Steam Idler", msg, ToolTipIcon.Info);
        }

        private bool IsAutoStartInRegistry()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegRunKey, false))
                {
                    if (key != null)
                    {
                        return key.GetValue(RegRunVal) != null;
                    }
                }
            }
            catch { }
            return false;
        }

        public void SetAutoStartRegistry(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegRunKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string cmd = string.Format("\"{0}\" --startup", _exePath);
                            key.SetValue(RegRunVal, cmd);
                        }
                        else
                        {
                            key.DeleteValue(RegRunVal, false);
                        }
                    }
                }
            }
            catch { }
        }

        public void OpenSettings()
        {
            if (_settingsForm == null || _settingsForm.IsDisposed)
            {
                _settingsForm = new SettingsForm(_config, _configPath, _workerManager, _exePath, () =>
                {
                    UpdateTrayDisplay();
                    SetAutoStartRegistry(_config.AutoStart);
                });
            }

            _settingsForm.Show();
            _settingsForm.BringToFront();
            _settingsForm.Activate();
        }

        private void ExitApplication()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            _workerManager.StopAll();

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }

            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _workerManager.StopAll();
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
