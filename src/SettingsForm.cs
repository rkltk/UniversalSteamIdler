using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace UniversalSteamIdler
{
    public class SettingsForm : Form
    {
        private readonly AppConfig _config;
        private readonly string _configPath;
        private readonly WorkerManager _workerManager;
        private readonly string _exePath;
        private readonly Action _onConfigChanged;

        private DataGridView _grid;
        private Label _lblStatus;
        private TextBox _txtAddName;
        private TextBox _txtAddAppId;
        private CheckBox _chkAutoStart;
        private NumericUpDown _numDelay;
        private CheckBox _chkNotifications;
        private Button _btnPauseResume;
        private Timer _statusTimer;

        public SettingsForm(AppConfig config, string configPath, WorkerManager workerManager, string exePath, Action onConfigChanged)
        {
            _config = config;
            _configPath = configPath;
            _workerManager = workerManager;
            _exePath = exePath;
            _onConfigChanged = onConfigChanged;

            InitializeComponent();
            RefreshGrid();
            UpdateStatusDisplay();

            _statusTimer = new Timer();
            _statusTimer.Interval = 2000;
            _statusTimer.Tick += (s, e) => UpdateStatusDisplay();
            _statusTimer.Start();
        }

        private void InitializeComponent()
        {
            this.Text = "Universal Steam Idler — Настройки и Игры";
            this.Size = new Size(720, 580);
            this.MinimumSize = new Size(680, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(23, 29, 37);
            this.ForeColor = Color.FromArgb(220, 225, 230);
            this.Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            this.ShowInTaskbar = true;

            // Header Panel
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(16, 21, 28),
                Padding = new Padding(16, 10, 16, 10)
            };

            Label lblTitle = new Label
            {
                Text = "🎮 Universal Steam Idler",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(102, 192, 244),
                AutoSize = true,
                Location = new Point(14, 8)
            };

            _lblStatus = new Label
            {
                Text = "● Статус: Проверка...",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(74, 222, 128),
                AutoSize = true,
                Location = new Point(16, 36)
            };

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(_lblStatus);

            // Toolbar Panel
            Panel toolbarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(28, 35, 45),
                Padding = new Padding(12, 6, 12, 6)
            };

            Button btnScan = CreateButton("🔍 Найти игры из Steam", 175, 28, Color.FromArgb(42, 71, 94));
            btnScan.Click += BtnScan_Click;
            btnScan.Location = new Point(12, 7);

            Button btnPresets = CreateButton("⭐ Добавить популярные", 175, 28, Color.FromArgb(42, 71, 94));
            btnPresets.Click += BtnPresets_Click;
            btnPresets.Location = new Point(195, 7);

            Button btnDelete = CreateButton("🗑 Удалить выбранную", 155, 28, Color.FromArgb(70, 35, 40));
            btnDelete.Click += BtnDelete_Click;
            btnDelete.Location = new Point(378, 7);

            Button btnOpenStore = CreateButton("🌐 Страница в Steam", 150, 28, Color.FromArgb(35, 55, 75));
            btnOpenStore.Click += BtnOpenStore_Click;
            btnOpenStore.Location = new Point(540, 7);

            toolbarPanel.Controls.Add(btnScan);
            toolbarPanel.Controls.Add(btnPresets);
            toolbarPanel.Controls.Add(btnDelete);
            toolbarPanel.Controls.Add(btnOpenStore);

            // Bottom Control Panel
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                BackColor = Color.FromArgb(18, 23, 30),
                Padding = new Padding(14, 8, 14, 8)
            };

            // Add manual game subpanel
            Panel addPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent
            };

            Label lblAdd = new Label { Text = "Добавить игру:", AutoSize = true, Location = new Point(0, 8), ForeColor = Color.FromArgb(180, 190, 205) };
            _txtAddName = new TextBox
            {
                Location = new Point(105, 5),
                Width = 230,
                BackColor = Color.FromArgb(32, 38, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            SetWatermark(_txtAddName, "Название игры (напр. Banana)");

            _txtAddAppId = new TextBox
            {
                Location = new Point(345, 5),
                Width = 110,
                BackColor = Color.FromArgb(32, 38, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            SetWatermark(_txtAddAppId, "AppID (2923300)");

            Button btnAdd = CreateButton("+ Добавить", 100, 26, Color.FromArgb(46, 107, 72));
            btnAdd.Location = new Point(465, 4);
            btnAdd.Click += BtnAdd_Click;

            addPanel.Controls.Add(lblAdd);
            addPanel.Controls.Add(_txtAddName);
            addPanel.Controls.Add(_txtAddAppId);
            addPanel.Controls.Add(btnAdd);

            // Settings subpanel (autostart, delay)
            Panel prefsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.Transparent
            };

            _chkAutoStart = new CheckBox
            {
                Text = "Автозапуск с Windows",
                Checked = _config.AutoStart,
                AutoSize = true,
                Location = new Point(0, 10),
                ForeColor = Color.FromArgb(220, 225, 235)
            };
            _chkAutoStart.CheckedChanged += (s, e) => { _config.AutoStart = _chkAutoStart.Checked; SaveConfig(); };

            Label lblDelay = new Label { Text = "Задержка старта:", AutoSize = true, Location = new Point(190, 12), ForeColor = Color.FromArgb(180, 190, 205) };
            _numDelay = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 1800,
                Value = _config.StartupDelaySeconds,
                Width = 65,
                Location = new Point(305, 9),
                BackColor = Color.FromArgb(32, 38, 48),
                ForeColor = Color.White
            };
            _numDelay.ValueChanged += (s, e) => { _config.StartupDelaySeconds = (int)_numDelay.Value; SaveConfig(); };

            Label lblDelaySec = new Label { Text = "сек. (5 мин = 300)", AutoSize = true, Location = new Point(375, 12), ForeColor = Color.FromArgb(140, 150, 165) };

            _chkNotifications = new CheckBox
            {
                Text = "Уведомления в трее",
                Checked = _config.ShowNotifications,
                AutoSize = true,
                Location = new Point(510, 10),
                ForeColor = Color.FromArgb(220, 225, 235)
            };
            _chkNotifications.CheckedChanged += (s, e) => { _config.ShowNotifications = _chkNotifications.Checked; SaveConfig(); };

            prefsPanel.Controls.Add(_chkAutoStart);
            prefsPanel.Controls.Add(lblDelay);
            prefsPanel.Controls.Add(_numDelay);
            prefsPanel.Controls.Add(lblDelaySec);
            prefsPanel.Controls.Add(_chkNotifications);

            // Buttons subpanel
            Panel actionButtonsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Color.Transparent
            };

            _btnPauseResume = CreateButton("⏸ Пауза", 120, 32, Color.FromArgb(60, 70, 85));
            _btnPauseResume.Location = new Point(0, 8);
            _btnPauseResume.Click += BtnPauseResume_Click;

            Button btnTray = CreateButton("💾 Свернуть в трей", 170, 32, Color.FromArgb(30, 95, 150));
            btnTray.Location = new Point(130, 8);
            btnTray.Click += (s, e) => { this.Hide(); };

            Button btnExit = CreateButton("❌ Выход (закрыть всё)", 165, 32, Color.FromArgb(85, 40, 45));
            btnExit.Location = new Point(520, 8);
            btnExit.Click += (s, e) => { Application.Exit(); };

            actionButtonsPanel.Controls.Add(_btnPauseResume);
            actionButtonsPanel.Controls.Add(btnTray);
            actionButtonsPanel.Controls.Add(btnExit);

            bottomPanel.Controls.Add(actionButtonsPanel);
            bottomPanel.Controls.Add(prefsPanel);
            bottomPanel.Controls.Add(addPanel);

            // DataGridView Grid
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(23, 29, 37),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                GridColor = Color.FromArgb(40, 50, 65),
                EnableHeadersVisualStyles = false,
                AutoGenerateColumns = false
            };

            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(16, 21, 28);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(102, 192, 244);
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _grid.ColumnHeadersHeight = 32;

            _grid.DefaultCellStyle.BackColor = Color.FromArgb(27, 34, 44);
            _grid.DefaultCellStyle.ForeColor = Color.White;
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 75, 105);
            _grid.DefaultCellStyle.SelectionForeColor = Color.White;
            _grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.25f);
            _grid.RowTemplate.Height = 28;

            DataGridViewCheckBoxColumn colActive = new DataGridViewCheckBoxColumn
            {
                HeaderText = "Фарм",
                Width = 60,
                DataPropertyName = "IsActive"
            };

            DataGridViewTextBoxColumn colName = new DataGridViewTextBoxColumn
            {
                HeaderText = "Название игры",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                DataPropertyName = "Name",
                ReadOnly = true
            };

            DataGridViewTextBoxColumn colAppId = new DataGridViewTextBoxColumn
            {
                HeaderText = "AppID",
                Width = 110,
                DataPropertyName = "AppId",
                ReadOnly = true
            };

            DataGridViewTextBoxColumn colStatus = new DataGridViewTextBoxColumn
            {
                HeaderText = "Состояние",
                Width = 130,
                ReadOnly = true
            };

            _grid.Columns.Add(colActive);
            _grid.Columns.Add(colName);
            _grid.Columns.Add(colAppId);
            _grid.Columns.Add(colStatus);

            _grid.CellValueChanged += Grid_CellValueChanged;
            _grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_grid.IsCurrentCellDirty)
                {
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

            // Main Controls Layout
            this.Controls.Add(_grid);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(toolbarPanel);
            this.Controls.Add(topPanel);

            this.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    this.Hide();
                }
            };
        }

        private Button CreateButton(string text, int width, int height, Color bgColor)
        {
            Button btn = new Button
            {
                Text = text,
                Width = width,
                Height = height,
                BackColor = bgColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.75f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SetWatermark(TextBox tb, string watermark)
        {
            tb.Text = watermark;
            tb.ForeColor = Color.Gray;

            tb.Enter += (s, e) =>
            {
                if (tb.Text == watermark)
                {
                    tb.Text = "";
                    tb.ForeColor = Color.White;
                }
            };

            tb.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Text = watermark;
                    tb.ForeColor = Color.Gray;
                }
            };
        }

        public void RefreshGrid()
        {
            _grid.Rows.Clear();
            List<string> running = _workerManager.GetRunningAppIds();

            foreach (GameItem g in _config.Games)
            {
                string status = "Остановлена";
                if (_workerManager.IsPaused)
                {
                    status = g.IsActive ? "Пауза" : "Выключена";
                }
                else if (running.Contains(g.AppId))
                {
                    status = "● Фармится";
                }
                else if (g.IsActive)
                {
                    status = "⏳ Ожидание Steam";
                }

                int rowIndex = _grid.Rows.Add(g.IsActive, g.Name, g.AppId, status);
                if (running.Contains(g.AppId) && !_workerManager.IsPaused)
                {
                    _grid.Rows[rowIndex].Cells[3].Style.ForeColor = Color.FromArgb(74, 222, 128); // Green
                }
                else if (g.IsActive)
                {
                    _grid.Rows[rowIndex].Cells[3].Style.ForeColor = Color.FromArgb(250, 204, 21); // Yellow
                }
                else
                {
                    _grid.Rows[rowIndex].Cells[3].Style.ForeColor = Color.FromArgb(150, 160, 175);
                }
            }
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                bool val = Convert.ToBoolean(_grid.Rows[e.RowIndex].Cells[0].Value);
                if (e.RowIndex < _config.Games.Count)
                {
                    _config.Games[e.RowIndex].IsActive = val;
                    SaveConfig();
                    _workerManager.Sync(_config.Games, _exePath);
                    UpdateStatusDisplay();
                }
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            string name = _txtAddName.Text.Trim();
            string appId = _txtAddAppId.Text.Trim();

            if (name == "Название игры (напр. Banana)") name = "";
            if (appId == "AppID (2923300)") appId = "";

            if (string.IsNullOrEmpty(appId))
            {
                MessageBox.Show("Пожалуйста, введите AppID игры (цифры со страницы в Steam)!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                name = "Игра #" + appId;
            }

            foreach (GameItem g in _config.Games)
            {
                if (g.AppId == appId)
                {
                    MessageBox.Show("Игра с таким AppID уже есть в списке!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            _config.Games.Add(new GameItem(appId, name, true));
            SaveConfig();
            RefreshGrid();
            _workerManager.Sync(_config.Games, _exePath);
            UpdateStatusDisplay();

            _txtAddName.Text = "";
            _txtAddAppId.Text = "";
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow != null && _grid.CurrentRow.Index >= 0 && _grid.CurrentRow.Index < _config.Games.Count)
            {
                GameItem item = _config.Games[_grid.CurrentRow.Index];
                if (MessageBox.Show(string.Format("Удалить \"{0}\" ({1}) из списка?", item.Name, item.AppId), "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _config.Games.RemoveAt(_grid.CurrentRow.Index);
                    SaveConfig();
                    RefreshGrid();
                    _workerManager.Sync(_config.Games, _exePath);
                    UpdateStatusDisplay();
                }
            }
        }

        private void BtnScan_Click(object sender, EventArgs e)
        {
            List<GameItem> scanned = SteamScanner.ScanInstalledGames();
            if (scanned.Count == 0)
            {
                MessageBox.Show("Не удалось найти установленные игры Steam на этом ПК.", "Поиск игр", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int addedCount = 0;
            HashSet<string> existing = new HashSet<string>();
            foreach (GameItem g in _config.Games) existing.Add(g.AppId);

            foreach (GameItem s in scanned)
            {
                if (!existing.Contains(s.AppId))
                {
                    _config.Games.Add(s);
                    existing.Add(s.AppId);
                    addedCount++;
                }
            }

            SaveConfig();
            RefreshGrid();
            MessageBox.Show(string.Format("Найдено {0} установленных игр в библиотеках Steam!\nДобавлено новых: {1}", scanned.Count, addedCount), "Сканирование Steam", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnPresets_Click(object sender, EventArgs e)
        {
            AppConfig defs = AppConfig.GetDefaults();
            HashSet<string> existing = new HashSet<string>();
            foreach (GameItem g in _config.Games) existing.Add(g.AppId);

            int added = 0;
            foreach (GameItem p in defs.Games)
            {
                if (!existing.Contains(p.AppId))
                {
                    p.IsActive = false;
                    _config.Games.Add(p);
                    existing.Add(p.AppId);
                    added++;
                }
            }

            SaveConfig();
            RefreshGrid();
            MessageBox.Show(string.Format("Популярные игры добавлены в список (+{0}).", added), "Популярные игры", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnOpenStore_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow != null && _grid.CurrentRow.Index >= 0 && _grid.CurrentRow.Index < _config.Games.Count)
            {
                GameItem item = _config.Games[_grid.CurrentRow.Index];
                try { Process.Start("https://store.steampowered.com/app/" + item.AppId + "/"); } catch { }
            }
        }

        private void BtnPauseResume_Click(object sender, EventArgs e)
        {
            if (_workerManager.IsPaused)
            {
                _workerManager.Resume(_config.Games, _exePath);
                _btnPauseResume.Text = "⏸ Пауза";
                _btnPauseResume.BackColor = Color.FromArgb(60, 70, 85);
            }
            else
            {
                _workerManager.Pause();
                _btnPauseResume.Text = "▶ Возобновить";
                _btnPauseResume.BackColor = Color.FromArgb(46, 107, 72);
            }
            RefreshGrid();
            UpdateStatusDisplay();
        }

        public void UpdateStatusDisplay()
        {
            List<string> running = _workerManager.GetRunningAppIds();
            if (_workerManager.IsPaused)
            {
                _lblStatus.Text = "○ На паузе (все фармы приостановлены)";
                _lblStatus.ForeColor = Color.FromArgb(250, 204, 21);
            }
            else if (running.Count > 0)
            {
                List<string> activeNames = new List<string>();
                foreach (GameItem g in _config.Games)
                {
                    if (running.Contains(g.AppId))
                    {
                        activeNames.Add(g.Name);
                    }
                }
                string namesStr = string.Join(", ", activeNames.ToArray());
                if (namesStr.Length > 45) namesStr = namesStr.Substring(0, 42) + "...";
                _lblStatus.Text = string.Format("● Активно: {0} {1} ({2})", running.Count, running.Count == 1 ? "игра" : "игр(ы)", namesStr);
                _lblStatus.ForeColor = Color.FromArgb(74, 222, 128);
            }
            else
            {
                int activeChecked = 0;
                foreach (GameItem g in _config.Games) if (g.IsActive) activeChecked++;

                if (activeChecked > 0)
                {
                    _lblStatus.Text = "⏳ Выбрано игр: " + activeChecked + " (ожидание подключения Steam...)";
                    _lblStatus.ForeColor = Color.FromArgb(250, 204, 21);
                }
                else
                {
                    _lblStatus.Text = "○ Ни одна игра не выбрана (поставьте галочку [V] в колонке «Фарм»)";
                    _lblStatus.ForeColor = Color.FromArgb(160, 170, 185);
                }
            }

            // Sync grid statuses
            if (_grid.Rows.Count == _config.Games.Count)
            {
                for (int i = 0; i < _config.Games.Count; i++)
                {
                    GameItem g = _config.Games[i];
                    string status = "Остановлена";
                    if (_workerManager.IsPaused)
                    {
                        status = g.IsActive ? "Пауза" : "Выключена";
                    }
                    else if (running.Contains(g.AppId))
                    {
                        status = "● Фармится";
                    }
                    else if (g.IsActive)
                    {
                        status = "⏳ Ожидание";
                    }

                    _grid.Rows[i].Cells[3].Value = status;
                    if (running.Contains(g.AppId) && !_workerManager.IsPaused)
                    {
                        _grid.Rows[i].Cells[3].Style.ForeColor = Color.FromArgb(74, 222, 128);
                    }
                    else if (g.IsActive)
                    {
                        _grid.Rows[i].Cells[3].Style.ForeColor = Color.FromArgb(250, 204, 21);
                    }
                    else
                    {
                        _grid.Rows[i].Cells[3].Style.ForeColor = Color.FromArgb(150, 160, 175);
                    }
                }
            }
        }

        private void SaveConfig()
        {
            _config.Save(_configPath);
            if (_onConfigChanged != null)
            {
                _onConfigChanged();
            }
        }
    }
}
