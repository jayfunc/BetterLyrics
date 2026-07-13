using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Dsp;
using NAudio.Wave;

namespace BetterLyrics.Core.Helpers;

public class SpectrumAnalyzer : IDisposable, IMMNotificationClient
{
    private readonly Debouncer _deviceDebouncer = new();
    private readonly MMDeviceEnumerator _deviceEnumerator;

    // Buffers
    private readonly float[] _fftLeftBuffer;
    private readonly Complex[] _fftLeftData;
    private readonly int _fftLength = 2048;
    private readonly float[] _fftRightBuffer;
    private readonly Complex[] _fftRightData;

    // Windowing & Compensation
    private readonly double[] _hammingWindow;
    private readonly object _lock = new();
    private readonly ILogger<SpectrumAnalyzer> _logger;
    private readonly int _m; // FFT Log2 n
    private WasapiLoopbackCapture? _capture;
    private float[]? _compensationMap; // 预计算的补偿表
    private float[]? _currentSpectrum;

    private bool _disposed;

    // Spectrum Data
    private float[]? _fullSpectrumData; // 存储合并后的数据

    // 用于记录最近一段时间检测到的最大音量
    private float _maxDetectedVolume = 0.1f;

    private int _sampleRate = 48000;

    public SpectrumAnalyzer()
    {
        _logger = Ioc.Default.GetRequiredService<ILogger<SpectrumAnalyzer>>();

        _deviceEnumerator = new MMDeviceEnumerator();
        _deviceEnumerator.RegisterEndpointNotificationCallback(this);

        _m = (int)Math.Log(_fftLength, 2);
        _fftLeftBuffer = new float[_fftLength];
        _fftLeftData = new Complex[_fftLength];
        _fftRightBuffer = new float[_fftLength];
        _fftRightData = new Complex[_fftLength];
        _hammingWindow = new double[_fftLength];

        // 预计算汉明窗
        for (var i = 0; i < _fftLength; i++)
            _hammingWindow[i] = 0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (_fftLength - 1));
    }

    public float[]? SmoothSpectrum { get; private set; }
    public float CurrentBassEnergy { get; private set; }

    public int BarCount
    {
        get => field;
        set
        {
            if (field == value || value < 2) return;

            // 因为 OnDataAvailable (后台线程) 和 UpdateSmoothSpectrum (UI线程) 
            // 都在频繁读写数组，不加锁会导致多线程冲突崩溃。
            lock (_lock)
            {
                field = value;

                // 如果当前正在捕获音频，需要立即重置数组大小
                // 如果没在捕获，StartCapture() 启动时会自己分配，所以不用管
                if (IsCapturing)
                {
                    _currentSpectrum = new float[field];
                    SmoothSpectrum = new float[field];
                }
            }
        }
    } = 64;

    public int Sensitivity { get; set; } = 100;
    public float SmoothingFactor { get; set; } = 0.92f; // 稍微降低一点，响应更快
    public bool IsCapturing { get; private set; }

    public void Dispose()
    {
        if (!_disposed)
        {
            _deviceEnumerator.UnregisterEndpointNotificationCallback(this);
            _deviceEnumerator.Dispose();

            _deviceDebouncer.Dispose();

            StopCapture();
            _disposed = true;
        }
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow == DataFlow.Render && role == Role.Multimedia)
        {
            _logger.LogInformation("System audio device is changing, waiting for stability...");

            _ = _deviceDebouncer.RunAsync(async token =>
            {
                _logger.LogInformation("Audio device stable, restarting capture...");

                StopCapture();

                await Task.Delay(500, token);

                StartCapture();
            }, 1000);
        }
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
    }

    public void OnDeviceRemoved(string deviceId)
    {
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
    }

    public void StartCapture()
    {
        if (IsCapturing) return;

        try
        {
            _capture = new WasapiLoopbackCapture();
            _sampleRate = _capture.WaveFormat.SampleRate;

            lock (_lock)
            {
                _currentSpectrum = new float[BarCount];
                SmoothSpectrum = new float[BarCount];

                var effectiveLength = _fftLength / 2;

                _fullSpectrumData = new float[effectiveLength * 2];

                PrecomputeCompensation(effectiveLength);
            }

            _capture.DataAvailable += OnDataAvailable;
            _capture.RecordingStopped += OnRecordingStopped;
            _capture.StartRecording();

            IsCapturing = true;
        }
        catch (Exception ex)
        {
            // 建议记录日志
            _logger.LogError(ex, "StartCapture");
        }
    }

    public void StopCapture()
    {
        if (_capture != null)
        {
            _capture?.DataAvailable -= OnDataAvailable;
            _capture?.RecordingStopped -= OnRecordingStopped;
            _capture?.StopRecording();
            _capture?.Dispose();
            _capture = null;
        }

        IsCapturing = false;
    }

    private static float CalculateCompensationFactor(float freq)
    {
        float[] frequencies = { 20, 50, 100, 200, 500, 1000, 2000, 4000, 8000, 16000, 20000 };
        float[] gains =
        {
            1.0f, // 20Hz 基频
            1.1f, // 50Hz 超低音
            1.1f, // 100Hz 鼓点核心
            1.2f, // 200Hz 军鼓基频
            1.4f, // 500Hz 人声厚度区 
            1.6f, // 1k 人声核心区 
            2.0f, // 2k 人声齿音   
            3.5f, // 4k 乐器临场感
            6.0f, // 8k 高频细节
            10.0f, // 16k 空气感   
            12.0f // 20k 极高频     
        };

        if (freq <= frequencies[0]) return gains[0];
        if (freq >= frequencies[frequencies.Length - 1]) return gains[gains.Length - 1];

        var i = 0;
        while (freq > frequencies[i + 1]) i++;

        var x1 = frequencies[i];
        var y1 = gains[i];
        var x2 = frequencies[i + 1];
        var y2 = gains[i + 1];

        return y1 + (freq - x1) * ((y2 - y1) / (x2 - x1));
    }

    public void UpdateSmoothSpectrum()
    {
        if (SmoothSpectrum == null || _currentSpectrum == null) return;

        lock (_lock)
        {
            // 这里可以用 SIMD 优化，但在 64-128 bar 级别下，普通循环足够快
            for (var i = 0; i < BarCount; i++)
            {
                // 简单的低通滤波
                var target = _currentSpectrum[i];
                var current = SmoothSpectrum[i];

                // 下落减速（上升快，下落慢）
                if (target > current)
                    SmoothSpectrum[i] = current * SmoothingFactor + target * (1 - SmoothingFactor);
                else
                    SmoothSpectrum[i] = current * 0.98f; // 下落慢一点
            }
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_disposed || e.BytesRecorded == 0) return;

        var bufferSpan = e.Buffer.AsSpan(0, e.BytesRecorded);
        var floatSpan = MemoryMarshal.Cast<byte, float>(bufferSpan);

        // 音量归一化
        var currentFramePeak = 0f;
        for (var i = 0; i < floatSpan.Length; i++)
        {
            var abs = Math.Abs(floatSpan[i]);
            if (abs > currentFramePeak) currentFramePeak = abs;
        }

        if (currentFramePeak > _maxDetectedVolume)
        {
            _maxDetectedVolume = currentFramePeak;
        }
        else
        {
            var ratio = currentFramePeak / _maxDetectedVolume;

            float decayRate;

            if (ratio < 0.2f)
                decayRate = 0.95f;
            else if (ratio < 0.5f)
                decayRate = 0.99f;
            else
                decayRate = 0.9995f;

            _maxDetectedVolume *= decayRate;
        }

        _maxDetectedVolume = Math.Max(0.02f, _maxDetectedVolume);

        var autoGainMultiplier = 1.0f / _maxDetectedVolume;

        // 填充 FFT
        var frameCount = floatSpan.Length / 2;
        if (frameCount < _fftLength) return;
        var offset = (frameCount - _fftLength) * 2;

        for (var i = 0; i < _fftLength; i++)
        {
            var sampleL = floatSpan[offset + i * 2] * autoGainMultiplier;
            var sampleR = floatSpan[offset + i * 2 + 1] * autoGainMultiplier;

            var window = _hammingWindow[i];

            _fftLeftData[i].X = sampleL * (float)window;
            _fftLeftData[i].Y = 0;

            _fftRightData[i].X = sampleR * (float)window;
            _fftRightData[i].Y = 0;
        }

        FastFourierTransform.FFT(true, _m, _fftLeftData);
        FastFourierTransform.FFT(true, _m, _fftRightData);

        if (_fullSpectrumData == null || _compensationMap == null) return;

        var halfLen = _fftLength / 2;

        for (var i = 0; i < halfLen; i++)
        {
            var realL = _fftLeftData[i].X;
            var imgL = _fftLeftData[i].Y;
            var magL = MathF.Sqrt(realL * realL + imgL * imgL);

            var realR = _fftRightData[i].X;
            var imgR = _fftRightData[i].Y;
            var magR = MathF.Sqrt(realR * realR + imgR * imgR);

            var compensation = _compensationMap[i];
            magL *= compensation;
            magR *= compensation;

            _fullSpectrumData[halfLen - 1 - i] = magL;
            _fullSpectrumData[halfLen + i] = magR;
        }

        // Bass
        var bassSum = 0f;

        var bassBinCount = 5;

        for (var k = 0; k < bassBinCount; k++)
            if (halfLen + k < _fullSpectrumData.Length && halfLen - 1 - k >= 0)
            {
                bassSum += _fullSpectrumData[halfLen + k];
                bassSum += _fullSpectrumData[halfLen - 1 - k];
            }

        // 归一化
        CurrentBassEnergy = Math.Clamp(bassSum / 1.0f, 0f, 1f);

        // 映射到 BarCount
        lock (_lock)
        {
            if (_currentSpectrum == null || _currentSpectrum.Length != BarCount) return;

            var dataLen = _fullSpectrumData.Length;

            for (var i = 0; i < BarCount; i++)
            {
                var index = Math.Min(dataLen - 1, i * dataLen / BarCount);

                _currentSpectrum[i] = _fullSpectrumData[index] * Sensitivity;
            }
        }
    }

    private void PrecomputeCompensation(int effectiveLength)
    {
        _compensationMap = new float[effectiveLength];

        for (var i = 0; i < effectiveLength; i++)
        {
            // 计算该 Bin 对应的频率
            var freq = (float)i * _sampleRate / _fftLength;
            _compensationMap[i] = CalculateCompensationFactor(freq);
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        IsCapturing = false;
    }
}