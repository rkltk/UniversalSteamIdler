using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace UniversalSteamIdler
{
    public static class SteamScanner
    {
        public static string GetSteamInstallPath()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("SteamPath");
                        if (val != null)
                        {
                            string p = val.ToString().Replace('/', '\\');
                            if (Directory.Exists(p)) return p;
                        }
                    }
                }
            }
            catch { }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("InstallPath");
                        if (val != null)
                        {
                            string p = val.ToString().Replace('/', '\\');
                            if (Directory.Exists(p)) return p;
                        }
                    }
                }
            }
            catch { }

            string defaultPath = @"C:\Program Files (x86)\Steam";
            if (Directory.Exists(defaultPath)) return defaultPath;

            return null;
        }

        public static List<string> GetLibraryFolders()
        {
            List<string> list = new List<string>();
            string steamPath = GetSteamInstallPath();
            if (string.IsNullOrEmpty(steamPath)) return list;

            list.Add(steamPath);

            try
            {
                string vdf = Path.Combine(steamPath, @"steamapps\libraryfolders.vdf");
                if (File.Exists(vdf))
                {
                    string text = File.ReadAllText(vdf);
                    MatchCollection matches = Regex.Matches(text, @"""path""\s+""([^""]+)""");
                    foreach (Match m in matches)
                    {
                        if (m.Groups.Count > 1)
                        {
                            string p = m.Groups[1].Value.Replace(@"\\", @"\");
                            if (Directory.Exists(p) && !list.Contains(p))
                            {
                                list.Add(p);
                            }
                        }
                    }
                }
            }
            catch { }

            return list;
        }

        public static List<GameItem> ScanInstalledGames()
        {
            List<GameItem> games = new List<GameItem>();
            HashSet<string> seenAppIds = new HashSet<string>();
            List<string> libraries = GetLibraryFolders();

            foreach (string lib in libraries)
            {
                try
                {
                    string steamApps = Path.Combine(lib, "steamapps");
                    if (!Directory.Exists(steamApps)) continue;

                    string[] files = Directory.GetFiles(steamApps, "appmanifest_*.acf");
                    foreach (string file in files)
                    {
                        try
                        {
                            string content = File.ReadAllText(file);
                            Match matchId = Regex.Match(content, @"""appid""\s+""(\d+)""");
                            Match matchName = Regex.Match(content, @"""name""\s+""([^""]+)""");

                            if (matchId.Success && matchName.Success)
                            {
                                string appId = matchId.Groups[1].Value;
                                string name = matchName.Groups[1].Value;

                                if (IsSystemOrTool(appId, name)) continue;

                                if (!seenAppIds.Contains(appId))
                                {
                                    seenAppIds.Add(appId);
                                    games.Add(new GameItem(appId, name, false));
                                }
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }

            games.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return games;
        }

        private static bool IsSystemOrTool(string appId, string name)
        {
            string n = name.ToLowerInvariant();
            if (n.Contains("steamworks common redistributables") ||
                n.Contains("steam linux runtime") ||
                n.Contains("proton") ||
                n.Contains("dedicated server") ||
                n.Contains("shared depot") ||
                n.Contains("soundtrack"))
            {
                return true;
            }
            return false;
        }
    }
}
