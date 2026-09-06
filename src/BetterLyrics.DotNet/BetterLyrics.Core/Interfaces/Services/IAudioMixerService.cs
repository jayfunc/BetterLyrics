namespace BetterLyrics.Core.Interfaces.Services;

public interface IAudioMixerService
{
    void SetApplicationVolume(int processId, int volume);
    void SetApplicationVolume(string? processNameOrAumid, int volume);
    int GetApplicationVolume(int processId);
    int GetApplicationVolume(string? processNameOrAumid);
    void SetSystemVolume(int volume);
    int GetSystemVolume();
}
