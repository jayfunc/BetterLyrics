using System;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using Vanara.PInvoke;

namespace BetterLyrics.WinUI3.Hooks;

public class AudioMixerHook
{
    private static readonly ILogger<AudioMixerHook> _logger;

    private static MMDeviceEnumerator? _deviceEnumerator;
    private static MMDevice? _defaultDevice;

    static AudioMixerHook()
    {
        _logger = Ioc.Default.GetRequiredService<ILogger<AudioMixerHook>>();
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
            _logger.LogError(ex, "InitializeAudioDevice");
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
            Kernel32.GetApplicationUserModelId(hProcess, ref length);

            if (length == 0) return null;

            var sb = new StringBuilder((int)length);
            var result = Kernel32.GetApplicationUserModelId(hProcess, ref length, sb);

            if (result == Win32Error.NO_ERROR) return sb.ToString();
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

        var targetVol = Math.Clamp(volume, 0, 100) / 100f;

        RunOnAudioSessions(processId, session =>

        {
            session.SimpleAudioVolume.Volume = targetVol;
            if (session.SimpleAudioVolume.Mute) session.SimpleAudioVolume.Mute = false;
        });
    }

    public static void SetApplicationVolume(string? processNameOrAumid, int volume)
    {
        if (string.IsNullOrEmpty(processNameOrAumid)) return;

        if (!processNameOrAumid.Contains("!"))
        {
            var procName = processNameOrAumid;
            if (procName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                procName = procName.Substring(0, procName.Length - 4);

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

        var targetVol = Math.Clamp(volume, 0, 100) / 100f;

        try
        {
            var sessionManager = _defaultDevice.AudioSessionManager;
            sessionManager.RefreshSessions();

            for (var i = 0; i < sessionManager.Sessions.Count; i++)
            {
                var session = sessionManager.Sessions[i];
                try
                {
                    var pid = session.GetProcessID;
                    if (pid == 0) continue;

                    var currentAumid = GetProcessAumid(pid);

                    if (string.Equals(currentAumid, processNameOrAumid, StringComparison.OrdinalIgnoreCase))
                    {
                        session.SimpleAudioVolume.Volume = targetVol;

                        if (session.SimpleAudioVolume.Mute) session.SimpleAudioVolume.Mute = false;
                    }
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetApplicationVolume");
        }
    }

    public static int GetApplicationVolume(int processId)
    {
        if (_defaultDevice == null) return -1;

        var result = -1;

        RunOnAudioSessions(processId, session => { result = (int)(session.SimpleAudioVolume.Volume * 100); }, true);

        return result;
    }

    public static int GetApplicationVolume(string? processNameOrAumid)
    {
        if (string.IsNullOrEmpty(processNameOrAumid)) return -1;

        if (!processNameOrAumid.Contains("!"))
        {
            var procName = processNameOrAumid;
            if (procName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                procName = procName.Substring(0, procName.Length - 4);

            var processes = Process.GetProcessesByName(procName);

            if (processes.Length > 0)
                try
                {
                    foreach (var p in processes)
                        try
                        {
                            var vol = GetApplicationVolume(p.Id);
                            if (vol != -1) return vol;
                        }
                        finally
                        {
                            p.Dispose();
                        }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GetApplicationVolume");
                }
        }

        if (_defaultDevice == null) return -1;

        try
        {
            var sessionManager = _defaultDevice.AudioSessionManager;
            sessionManager.RefreshSessions();

            for (var i = 0; i < sessionManager.Sessions.Count; i++)
            {
                var session = sessionManager.Sessions[i];

                try
                {
                    var pid = session.GetProcessID;
                    if (pid == 0) continue;

                    var currentAumid = GetProcessAumid(pid);

                    if (string.Equals(currentAumid, processNameOrAumid, StringComparison.OrdinalIgnoreCase))
                        return (int)(session.SimpleAudioVolume.Volume * 100);
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetApplicationVolume");
        }

        return -1;
    }

    private static void RunOnAudioSessions(int targetPid, Action<AudioSessionControl> action,
        bool stopAfterFirst = false)
    {
        if (_defaultDevice == null) return;

        try
        {
            var sessionManager = _defaultDevice.AudioSessionManager;
            sessionManager.RefreshSessions();

            for (var i = 0; i < sessionManager.Sessions.Count; i++)
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
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RunOnAudioSessions");
        }
    }
}