using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace UniversalSteamIdler
{
    static class Program
    {
        private const string MutexName = "Global\\UniversalSteamIdler_SingleInstance_App";

        [STAThread]
        static void Main(string[] args)
        {
            // 1. Worker Mode: Headless sub-process for idling a specific AppID
            if (args.Length >= 2 && args[0].ToLowerInvariant() == "--worker")
            {
                string appId = args[1];
                int parentPid = 0;
                if (args.Length >= 3)
                {
                    int.TryParse(args[2], out parentPid);
                }
                WorkerProcess.Run(appId, parentPid);
                return;
            }

            // 2. Main Application Mode: Single Instance Check
            bool isNewInstance = false;
            using (Mutex mutex = new Mutex(true, MutexName, out isNewInstance))
            {
                if (!isNewInstance)
                {
                    MessageBox.Show(
                        "Universal Steam Idler уже запущен и находится в системном трее (возле часов)!\n\nКликните дважды по иконке в трее, чтобы открыть настройки.",
                        "Universal Steam Idler",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                string exePath = Application.ExecutablePath;
                string exeDir = Path.GetDirectoryName(exePath);

                // Ensure steam_api.dll is in place
                EnsureSteamApiDll(exeDir);

                // Config location: portable first (if next to exe), otherwise %APPDATA%\UniversalSteamIdler
                string portableConfig = Path.Combine(exeDir, "config.json");
                string appDataConfig = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "UniversalSteamIdler",
                    "config.json");

                string configPath = File.Exists(portableConfig) ? portableConfig : appDataConfig;
                AppConfig config = AppConfig.Load(configPath);

                bool startupMode = false;
                for (int i = 0; i < args.Length; i++)
                {
                    string a = args[i].ToLowerInvariant();
                    if (a == "--startup" || a == "-s")
                    {
                        startupMode = true;
                    }
                    else if (a == "--settings")
                    {
                        config.StartMinimized = false;
                    }
                }

                // If user enabled autostart in config, ensure registry matches
                TrayApp app = new TrayApp(config, configPath, exePath, startupMode);
                if (config.AutoStart)
                {
                    app.SetAutoStartRegistry(true);
                }

                Application.Run(app);
            }
        }

        private static void EnsureSteamApiDll(string exeDir)
        {
            try
            {
                string targetDll = Path.Combine(exeDir, "steam_api.dll");
                if (File.Exists(targetDll)) return;

                // Check lib subfolder
                string libDll = Path.Combine(exeDir, "lib", "steam_api.dll");
                if (File.Exists(libDll))
                {
                    File.Copy(libDll, targetDll, true);
                    return;
                }

                // Check Steam client folder
                string steamDir = SteamScanner.GetSteamInstallPath();
                if (!string.IsNullOrEmpty(steamDir))
                {
                    string clientDll = Path.Combine(steamDir, "steam_api.dll");
                    if (File.Exists(clientDll))
                    {
                        File.Copy(clientDll, targetDll, true);
                    }
                }
            }
            catch { }
        }
    }
}
