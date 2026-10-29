using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace UniversalSteamIdler
{
    public static class SteamNative
    {
        [DllImport("steam_api.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_Init();

        [DllImport("steam_api.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_IsSteamRunning();

        [DllImport("steam_api.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SteamAPI_RunCallbacks();

        [DllImport("steam_api.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SteamAPI_Shutdown();
    }

    public static class WorkerProcess
    {
        public static void Run(string appId, int parentPid)
        {
            try
            {
                Environment.SetEnvironmentVariable("SteamAppId", appId);
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                if (!string.IsNullOrEmpty(exeDir))
                {
                    Directory.SetCurrentDirectory(exeDir);
                }

                if (!SteamNative.SteamAPI_IsSteamRunning())
                {
                    // Wait up to 30 seconds for Steam to start
                    for (int i = 0; i < 30; i++)
                    {
                        Thread.Sleep(1000);
                        if (SteamNative.SteamAPI_IsSteamRunning()) break;
                    }
                }

                if (!SteamNative.SteamAPI_Init())
                {
                    // Retry once after 2 seconds
                    Thread.Sleep(2000);
                    if (!SteamNative.SteamAPI_Init())
                    {
                        return;
                    }
                }

                Process parent = null;
                if (parentPid > 0)
                {
                    try { parent = Process.GetProcessById(parentPid); } catch { }
                }

                while (true)
                {
                    Thread.Sleep(1000);

                    // Check parent process
                    if (parent != null)
                    {
                        try
                        {
                            if (parent.HasExited) break;
                        }
                        catch
                        {
                            break;
                        }
                    }

                    // Check Steam process
                    if (!SteamNative.SteamAPI_IsSteamRunning())
                    {
                        break;
                    }

                    try
                    {
                        SteamNative.SteamAPI_RunCallbacks();
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            finally
            {
                try { SteamNative.SteamAPI_Shutdown(); } catch { }
            }
        }
    }

    public class WorkerManager
    {
        private readonly Dictionary<string, Process> _workers = new Dictionary<string, Process>();
        private readonly object _lock = new object();
        public bool IsPaused { get; private set; }

        public void Sync(List<GameItem> activeGames, string exePath)
        {
            lock (_lock)
            {
                if (IsPaused)
                {
                    StopAllWorkers();
                    return;
                }

                HashSet<string> desiredAppIds = new HashSet<string>();
                int myPid = Process.GetCurrentProcess().Id;

                foreach (GameItem g in activeGames)
                {
                    if (g.IsActive && !string.IsNullOrEmpty(g.AppId))
                    {
                        desiredAppIds.Add(g.AppId);

                        // If not running, start worker
                        if (!_workers.ContainsKey(g.AppId) || HasProcessExited(_workers[g.AppId]))
                        {
                            try
                            {
                                ProcessStartInfo psi = new ProcessStartInfo
                                {
                                    FileName = exePath,
                                    Arguments = string.Format("--worker {0} {1}", g.AppId, myPid),
                                    CreateNoWindow = true,
                                    UseShellExecute = false,
                                    WindowStyle = ProcessWindowStyle.Hidden,
                                    WorkingDirectory = Path.GetDirectoryName(exePath)
                                };
                                Process p = Process.Start(psi);
                                if (p != null)
                                {
                                    _workers[g.AppId] = p;
                                }
                            }
                            catch { }
                        }
                    }
                }

                // Stop workers that are no longer active
                List<string> toRemove = new List<string>();
                foreach (KeyValuePair<string, Process> kvp in _workers)
                {
                    if (!desiredAppIds.Contains(kvp.Key))
                    {
                        StopWorker(kvp.Value);
                        toRemove.Add(kvp.Key);
                    }
                }
                foreach (string appId in toRemove)
                {
                    _workers.Remove(appId);
                }
            }
        }

        public void Pause()
        {
            lock (_lock)
            {
                IsPaused = true;
                StopAllWorkers();
            }
        }

        public void Resume(List<GameItem> activeGames, string exePath)
        {
            lock (_lock)
            {
                IsPaused = false;
                Sync(activeGames, exePath);
            }
        }

        public void StopAll()
        {
            lock (_lock)
            {
                StopAllWorkers();
            }
        }

        public List<string> GetRunningAppIds()
        {
            lock (_lock)
            {
                List<string> list = new List<string>();
                foreach (KeyValuePair<string, Process> kvp in _workers)
                {
                    if (!HasProcessExited(kvp.Value))
                    {
                        list.Add(kvp.Key);
                    }
                }
                return list;
            }
        }

        private void StopAllWorkers()
        {
            foreach (KeyValuePair<string, Process> kvp in _workers)
            {
                StopWorker(kvp.Value);
            }
            _workers.Clear();
        }

        private void StopWorker(Process p)
        {
            if (p == null) return;
            try
            {
                if (!p.HasExited)
                {
                    p.Kill();
                    p.WaitForExit(1000);
                }
            }
            catch { }
        }

        private bool HasProcessExited(Process p)
        {
            if (p == null) return true;
            try
            {
                return p.HasExited;
            }
            catch
            {
                return true;
            }
        }
    }
}
