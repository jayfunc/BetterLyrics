using System;

namespace BetterLyrics.Core.Interfaces.Services;

public interface ISpectrumAnalyzer : IDisposable
{
    float[]? SmoothSpectrum { get; }
    float CurrentBassEnergy { get; }
    int BarCount { get; set; }
    int Sensitivity { get; set; }
    int DelayMs { get; set; }
    float SmoothingFactor { get; set; }
    bool IsCapturing { get; }

    void StartCapture();
    void StopCapture();
    void UpdateSmoothSpectrum();
}
