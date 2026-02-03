using Microsoft.UI.Dispatching;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Vanara.PInvoke;

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

        private static string? GetProcessAumid(uint pid)
        {
            Kernel32.SafeHPROCESS? hProcess = null;
            try
            {
                hProcess = Kernel32.OpenProcess(ACCESS_MASK.GENERIC_ALL, false, pid);
                if (hProcess == IntPtr.Zero) return null;

                uint length = 0;
                Kernel32.GetApplicationUserModelId(hProcess, ref length, null);

                if (length == 0) return null;

                StringBuilder sb = new StringBuilder((int)length);
                Win32Error result = Kernel32.GetApplicationUserModelId(hProcess, ref length, sb);

                if (result == Win32Error.NO_ERROR)
                {
                    return sb.ToString();
                }
            }
            catch
            {
                // 忽略权限不足或其他错误
            }
            finally
            {
                hProcess?.Close();
            }
            return null;
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

        public static void SetApplicationVolume(string? processNameOrAumid, int volume)
        {
            if (string.IsNullOrEmpty(processNameOrAumid)) return;

            if (!processNameOrAumid.Contains("!"))
            {
                string procName = processNameOrAumid;
                if (procName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    procName = procName.Substring(0, procName.Length - 4);
                }

                var processes = Process.GetProcessesByName(procName);
                if (processes.Length > 0)
                {
                    foreach (var p in processes)
                    {
                        SetApplicationVolume(p.Id, volume);
                        p.Dispose();
                    }
                    return;
                }
            }

            if (_defaultDevice == null) return;

            float targetVol = Math.Clamp(volume, 0, 100) / 100f;

            try
            {
                var sessionManager = _defaultDevice.AudioSessionManager;
                sessionManager.RefreshSessions();

                for (int i = 0; i < sessionManager.Sessions.Count; i++)
                {
                    var session = sessionManager.Sessions[i];
                    try
                    {
                        uint pid = session.GetProcessID;
                        if (pid == 0) continue;

                        string? currentAumid = GetProcessAumid(pid);

                        if (string.Equals(currentAumid, processNameOrAumid, StringComparison.OrdinalIgnoreCase))
                        {
                            session.SimpleAudioVolume.Volume = targetVol;

                            if (session.SimpleAudioVolume.Mute)
                            {
                                session.SimpleAudioVolume.Mute = false;
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing audio sessions: {ex.Message}");
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

        public static int GetApplicationVolume(string? processNameOrAumid)
        {
            if (string.IsNullOrEmpty(processNameOrAumid)) return -1;

            if (!processNameOrAumid.Contains("!"))
            {
                string procName = processNameOrAumid;
                if (procName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    procName = procName.Substring(0, procName.Length - 4);
                }

                var processes = Process.GetProcessesByName(procName);

                if (processes.Length > 0)
                {
                    try
                    {
                        foreach (var p in processes)
                        {
                            try
                            {
                                int vol = GetApplicationVolume(p.Id);
                                if (vol != -1) return vol;
                            }
                            finally
                            {
                                p.Dispose();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error getting Win32 volume for {procName}: {ex.Message}");
                    }
                }
            }

            if (_defaultDevice == null) return -1;

            try
            {
                var sessionManager = _defaultDevice.AudioSessionManager;
                sessionManager.RefreshSessions();

                for (int i = 0; i < sessionManager.Sessions.Count; i++)
                {
                    var session = sessionManager.Sessions[i];

                    try
                    {
                        uint pid = session.GetProcessID;
                        if (pid == 0) continue;

                        string? currentAumid = GetProcessAumid(pid);

                        if (string.Equals(currentAumid, processNameOrAumid, StringComparison.OrdinalIgnoreCase))
                        {
                            return (int)(session.SimpleAudioVolume.Volume * 100);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scanning AUMID sessions: {ex.Message}");
            }

            return -1;
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