using Microsoft.UI.Dispatching;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Diagnostics;
using System.Linq;

namespace BetterLyrics.WinUI3.Hooks
{
    public static class AudioMixerHook
    {
        private static MMDeviceEnumerator? _deviceEnumerator;
        private static MMDevice? _defaultDevice;

        static AudioMixerHook()
        {
            InitializeAudioDevice();
        }

        private static void InitializeAudioDevice()
        {
            try
            {
                _deviceEnumerator = new MMDeviceEnumerator();
                _defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Audio init failed: {ex.Message}");
            }
        }

        public static void SetApplicationVolume(int processId, int volume)
        {
            if (_defaultDevice == null) return;

            float targetVol = Math.Clamp(volume, 0, 100) / 100f;

            RunOnAudioSessions(processId, (session) =>
            {
                session.SimpleAudioVolume.Volume = targetVol;
                if (session.SimpleAudioVolume.Mute)
                    session.SimpleAudioVolume.Mute = false;
            });
        }

        public static void SetApplicationVolume(string? processName, int volume)
        {
            if (processName == null) return;

            if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                processName = processName.Substring(0, processName.Length - 4);
            }

            var processes = Process.GetProcessesByName(processName);

            if (processes.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine($"未找到名为 {processName} 的进程");
                return;
            }

            foreach (var p in processes)
            {
                SetApplicationVolume(p.Id, volume);

                p.Dispose();
            }
        }

        public static int GetApplicationVolume(int processId)
        {
            if (_defaultDevice == null) return -1;

            int result = -1;

            RunOnAudioSessions(processId, (session) =>
            {
                result = (int)(session.SimpleAudioVolume.Volume * 100);
            }, true);

            return result;
        }

        public static int GetApplicationVolume(string? processName)
        {
            if (processName == null) return -1;

            if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                processName = processName.Substring(0, processName.Length - 4);
            }

            var processes = Process.GetProcessesByName(processName);

            if (processes.Length == 0) return -1;

            try
            {
                foreach (var p in processes)
                {
                    try
                    {
                        int vol = GetApplicationVolume(p.Id);

                        if (vol != -1)
                        {
                            return vol;
                        }
                    }
                    finally
                    {
                        p.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting volume for {processName}: {ex.Message}");
            }

            return -1;
        }

        public static void SetApplicationMute(int processId, bool isMuted)
        {
            if (_defaultDevice == null) return;

            RunOnAudioSessions(processId, (session) =>
            {
                session.SimpleAudioVolume.Mute = isMuted;
            });
        }

        private static void RunOnAudioSessions(int targetPid, Action<AudioSessionControl> action, bool stopAfterFirst = false)
        {
            if (_defaultDevice == null) return;

            try
            {
                var sessionManager = _defaultDevice.AudioSessionManager;
                sessionManager.RefreshSessions();

                for (int i = 0; i < sessionManager.Sessions.Count; i++)
                {
                    var session = sessionManager.Sessions[i];

                    try
                    {
                        if (session.GetProcessID == targetPid)
                        {
                            action(session);
                            if (stopAfterFirst) return;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error accessing audio sessions: {ex.Message}");
            }
        }
    }
}