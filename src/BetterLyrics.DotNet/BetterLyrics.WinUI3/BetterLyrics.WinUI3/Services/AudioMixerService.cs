using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using System;
using System.Diagnostics;
using System.Text;
using Vanara.PInvoke;

namespace BetterLyrics.WinUI3.Services;

public class AudioMixerService : IAudioMixerService, IDisposable
{
    private readonly ILogger<AudioMixerService> _logger;

    private MMDeviceEnumerator? _deviceEnumerator;

    public AudioMixerService(ILogger<AudioMixerService> logger)
    {
        _logger = logger;
        try
        {
            _deviceEnumerator = new MMDeviceEnumerator();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MMDeviceEnumerator");
        }
    }

    private MMDevice? GetDefaultDevice()
    {
        try
        {
            _deviceEnumerator ??= new MMDeviceEnumerator();
            return _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDefaultDevice failed");
            return null;
        }
    }

    public void Dispose()
    {
        _deviceEnumerator?.Dispose();
        _deviceEnumerator = null;
        GC.SuppressFinalize(this);
    }

    private string? GetProcessAumid(uint pid)
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

    public void SetApplicationVolume(int processId, int volume)
    {
        var targetVol = Math.Clamp(volume, 0, 100) / 100f;

        RunOnAudioSessions(processId, session =>
        {
            session.SimpleAudioVolume.Volume = targetVol;
            if (session.SimpleAudioVolume.Mute) session.SimpleAudioVolume.Mute = false;
        });
    }

    public void SetApplicationVolume(string? processNameOrAumid, int volume)
    {
        if (string.IsNullOrEmpty(processNameOrAumid)) return;

        if (PlayerIdHelper.IsBetterLyrics(processNameOrAumid))
        {
            SetApplicationVolume(Environment.ProcessId, volume);
            return;
        }

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

        try
        {
            using var defaultDevice = GetDefaultDevice();
            if (defaultDevice == null) return;

            var targetVol = Math.Clamp(volume, 0, 100) / 100f;
            var sessionManager = defaultDevice.AudioSessionManager;
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

    public int GetApplicationVolume(int processId)
    {
        var result = -1;

        RunOnAudioSessions(processId, session => { result = (int)(session.SimpleAudioVolume.Volume * 100); }, true);

        return result;
    }

    public int GetApplicationVolume(string? processNameOrAumid)
    {
        if (string.IsNullOrEmpty(processNameOrAumid)) return -1;

        if (PlayerIdHelper.IsBetterLyrics(processNameOrAumid))
        {
            var vol = GetApplicationVolume(Environment.ProcessId);
            if (vol != -1) return vol;
        }

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

        try
        {
            using var defaultDevice = GetDefaultDevice();
            if (defaultDevice == null) return -1;

            var sessionManager = defaultDevice.AudioSessionManager;
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

    private void RunOnAudioSessions(int targetPid, Action<AudioSessionControl> action,
        bool stopAfterFirst = false)
    {
        try
        {
            using var defaultDevice = GetDefaultDevice();
            if (defaultDevice == null) return;

            var sessionManager = defaultDevice.AudioSessionManager;
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

    public void SetSystemVolume(int volume)
    {
        try
        {
            using var defaultDevice = GetDefaultDevice();
            if (defaultDevice == null) return;

            var targetVol = Math.Clamp(volume, 0, 100) / 100f;
            defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = targetVol;
            if (defaultDevice.AudioEndpointVolume.Mute)
                defaultDevice.AudioEndpointVolume.Mute = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetSystemVolume");
        }
    }

    public int GetSystemVolume()
    {
        try
        {
            using var defaultDevice = GetDefaultDevice();
            if (defaultDevice == null) return -1;

            return (int)(defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSystemVolume");
            return -1;
        }
    }
}