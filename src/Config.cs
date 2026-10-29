using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace UniversalSteamIdler
{
    public class GameItem
    {
        public string AppId { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public GameItem()
        {
            AppId = "";
            Name = "";
            IsActive = false;
        }

        public GameItem(string appId, string name, bool isActive)
        {
            AppId = appId;
            Name = name;
            IsActive = isActive;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, AppId);
        }
    }

    public class AppConfig
    {
        public List<GameItem> Games { get; set; }
        public bool AutoStart { get; set; }
        public int StartupDelaySeconds { get; set; }
        public bool StartMinimized { get; set; }
        public bool ShowNotifications { get; set; }

        public AppConfig()
        {
            Games = new List<GameItem>();
            AutoStart = true;
            StartupDelaySeconds = 300; // 5 minutes default
            StartMinimized = true;
            ShowNotifications = true;
        }

        public static AppConfig GetDefaults()
        {
            AppConfig cfg = new AppConfig();
            cfg.Games.Add(new GameItem("2696480", "Nothing", true));
            cfg.Games.Add(new GameItem("2923300", "Banana", false));
            cfg.Games.Add(new GameItem("480", "Spacewar", false));
            cfg.Games.Add(new GameItem("730", "Counter-Strike 2", false));
            cfg.Games.Add(new GameItem("440", "Team Fortress 2", false));
            cfg.Games.Add(new GameItem("570", "Dota 2", false));
            cfg.Games.Add(new GameItem("252490", "Rust", false));
            return cfg;
        }

        public static AppConfig Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    AppConfig cfg = js.Deserialize<AppConfig>(json);
                    if (cfg != null && cfg.Games != null)
                    {
                        return cfg;
                    }
                }
            }
            catch { }

            AppConfig def = GetDefaults();
            def.Save(path);
            return def;
        }

        public void Save(string path)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                JavaScriptSerializer js = new JavaScriptSerializer();
                string json = js.Serialize(this);
                File.WriteAllText(path, FormatJson(json));
            }
            catch { }
        }

        private static string FormatJson(string json)
        {
            // Simple pretty-printer for readability
            int indent = 0;
            bool inQuotes = false;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < json.Length; i++)
            {
                char ch = json[i];
                if (ch == '\"')
                {
                    bool isEscaped = false;
                    for (int j = i - 1; j >= 0 && json[j] == '\\'; j--)
                    {
                        isEscaped = !isEscaped;
                    }
                    if (!isEscaped)
                    {
                        inQuotes = !inQuotes;
                    }
                }

                if (inQuotes)
                {
                    sb.Append(ch);
                    continue;
                }

                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        sb.AppendLine();
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                        break;
                    case '}':
                    case ']':
                        sb.AppendLine();
                        indent = Math.Max(0, indent - 1);
                        sb.Append(new string(' ', indent * 2));
                        sb.Append(ch);
                        break;
                    case ',':
                        sb.Append(ch);
                        sb.AppendLine();
                        sb.Append(new string(' ', indent * 2));
                        break;
                    case ':':
                        sb.Append(": ");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }

            return sb.ToString();
        }
    }
}
