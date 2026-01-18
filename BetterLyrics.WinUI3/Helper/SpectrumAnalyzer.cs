using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Runtime.InteropServices;
using System.Linq;

namespace BetterLyrics.WinUI3.Helper
{
    public partial class SpectrumAnalyzer : IDisposable
    {
        private readonly object _lock = new();
        private WasapiLoopbackCapture? _capture;

        private int _sampleRate = 48000;
        private readonly int _fftLength = 2048;
        private readonly int _m; // FFT Log2 n

        // Buffers
        private readonly float[] _fftLeftBuffer;
        private readonly float[] _fftRightBuffer;
        private readonly Complex[] _fftLeftData;
        private readonly Complex[] _fftRightData;

        // Windowing & Compensation
        private readonly double[] _hammingWindow;
        private float[]? _compensationMap; // 预计算的补偿表

        // Spectrum Data
        private float[]? _fullSpectrumData; // 存储合并后的数据
        private float[]? _currentSpectrum;
        public float[]? SmoothSpectrum { get; private set; }

        private bool _disposed = false;

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
        public bool IsCapturing { get; private set; } = false;

        public SpectrumAnalyzer()
        {
            _m = (int)Math.Log(_fftLength, 2);
            _fftLeftBuffer = new float[_fftLength];
            _fftLeftData = new Complex[_fftLength];
            _fftRightBuffer = new float[_fftLength];
            _fftRightData = new Complex[_fftLength];
            _hammingWindow = new double[_fftLength];

            // 预计算汉明窗
            for (int i = 0; i < _fftLength; i++)
            {
                _hammingWindow[i] = 0.54 - 0.46 * Math.Cos((2 * Math.PI * i) / (_fftLength - 1));
            }
        }

        public void StartCapture()
        {
            if (IsCapturing) return;

            try
            {
                _capture = new WasapiLoopbackCapture();
                _sampleRate = _capture.WaveFormat.SampleRate;

                // 初始化数组
                lock (_lock)
                {
                    _currentSpectrum = new float[BarCount];
                    SmoothSpectrum = new float[BarCount];

                    // 计算有效频率范围的数据长度 (这里保留你原本的逻辑，取一半FFT长度作为单声道有效数据)
                    // Nyquist频率是 SampleRate / 2。FFT结果的后半部分是镜像，通常只需要前一半。
                    int effectiveLength = _fftLength / 2;

                    // Left + Right 拼接后的总长度
                    _fullSpectrumData = new float[effectiveLength * 2];

                    // 预计算频率补偿表 (Lookup Table)
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
                System.Diagnostics.Debug.WriteLine($"StartCapture Failed: {ex.Message}");
            }
        }

        public void StopCapture()
        {
            if (_capture != null)
            {
                _capture.DataAvailable -= OnDataAvailable;
                _capture.RecordingStopped -= OnRecordingStopped;
                _capture.StopRecording();
                _capture.Dispose();
                _capture = null;
            }
            IsCapturing = false;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_disposed || e.BytesRecorded == 0) return;

            // 快速将 byte[] 转为 float[] (性能优化：Span/Cast)
            // WasapiLoopback 默认通常是 IEEE Float (32bit)
            var bufferSpan = e.Buffer.AsSpan(0, e.BytesRecorded);
            var floatSpan = MemoryMarshal.Cast<byte, float>(bufferSpan);

            // 确保数据足够
            // 每次步进通道数 (Stereo = 2)
            int frameCount = floatSpan.Length / 2;
            if (frameCount < _fftLength) return;

            // 填充数据并应用窗函数
            // 注意：这里我们只取最近的 _fftLength 个样本，或者处理环形缓冲区。
            // 简单起见，取最新的 _fftLength 个数据
            int offset = (frameCount - _fftLength) * 2;

            for (int i = 0; i < _fftLength; i++)
            {
                // 此时 floatSpan[offset + i * 2] 是左声道，+1 是右声道
                float sampleL = floatSpan[offset + i * 2];
                float sampleR = floatSpan[offset + i * 2 + 1];

                double window = _hammingWindow[i];

                _fftLeftData[i].X = sampleL * (float)window;
                _fftLeftData[i].Y = 0;

                _fftRightData[i].X = sampleR * (float)window;
                _fftRightData[i].Y = 0;
            }

            // 执行 FFT (使用缓存的 m)
            FastFourierTransform.FFT(true, _m, _fftLeftData);
            FastFourierTransform.FFT(true, _m, _fftRightData);

            // 计算幅值并应用补偿 (使用预计算表)
            if (_fullSpectrumData == null || _compensationMap == null) return;

            int halfLen = _fftLength / 2;

            // 直接操作 _fullSpectrumData，避免中间数组分配
            // 逻辑：[左声道反向 (0...halfLen)] + [右声道正向 (halfLen...End)]

            for (int i = 0; i < halfLen; i++)
            {
                // 计算 Left 幅值
                float realL = (float)_fftLeftData[i].X;
                float imgL = (float)_fftLeftData[i].Y;
                float magL = (float)Math.Sqrt(realL * realL + imgL * imgL);

                // 计算 Right 幅值
                float realR = (float)_fftRightData[i].X;
                float imgR = (float)_fftRightData[i].Y;
                float magR = (float)Math.Sqrt(realR * realR + imgR * imgR);

                // 应用补偿
                float compensation = _compensationMap[i];
                magL *= compensation;
                magR *= compensation;

                // 填充到全谱图数组
                // 左声道放在前半部分，且反转 (Index: halfLen - 1 - i)
                _fullSpectrumData[halfLen - 1 - i] = magL;

                // 右声道放在后半部分 (Index: halfLen + i)
                _fullSpectrumData[halfLen + i] = magR;
            }

            // 映射到 BarCount (抽样)
            lock (_lock)
            {
                if (_currentSpectrum == null || _currentSpectrum.Length != BarCount) return;

                int dataLen = _fullSpectrumData.Length;

                for (int i = 0; i < BarCount; i++)
                {
                    // 使用简单的对数映射尝试 (让低频占更多格子)
                    // 如果想要原本的线性，用注释掉的那行

                    // 稍微优化一点的线性（防止最后越界）
                    int index = Math.Min(dataLen - 1, i * dataLen / BarCount);

                    _currentSpectrum[i] = _fullSpectrumData[index] * Sensitivity;
                }
            }
        }

        // 预计算频率补偿表，避免每帧计算
        private void PrecomputeCompensation(int effectiveLength)
        {
            _compensationMap = new float[effectiveLength];

            for (int i = 0; i < effectiveLength; i++)
            {
                // 计算该 Bin 对应的频率
                float freq = (float)i * _sampleRate / _fftLength;
                _compensationMap[i] = CalculateCompensationFactor(freq);
            }
        }

        // 原始的计算逻辑，提取出来只在初始化时调用
        private float CalculateCompensationFactor(float freq)
        {
            float[] frequencies = { 20, 50, 100, 200, 500, 1000, 2000, 4000, 8000, 16000, 20000 };
            // 低频保持，高频线性增加
            float[] gains = { 1.0f, 1.2f, 1.4f, 1.6f, 2.0f, 2.5f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f };
            // 通常低频需要衰减一点，高频需要增益一点，例如 { 0.8f, ... , 2.5f } 这种曲线，否则高频通常看起来很平。

            if (freq <= frequencies[0]) return gains[0];
            if (freq >= frequencies[frequencies.Length - 1]) return gains[gains.Length - 1];

            int i = 0;
            while (freq > frequencies[i + 1]) i++;

            float x1 = frequencies[i];
            float y1 = gains[i];
            float x2 = frequencies[i + 1];
            float y2 = gains[i + 1];

            return y1 + (freq - x1) * ((y2 - y1) / (x2 - x1));
        }

        public void UpdateSmoothSpectrum()
        {
            if (SmoothSpectrum == null || _currentSpectrum == null) return;

            lock (_lock)
            {
                // 这里可以用 SIMD 优化，但在 64-128 bar 级别下，普通循环足够快
                for (int i = 0; i < BarCount; i++)
                {
                    // 简单的低通滤波
                    float target = _currentSpectrum[i];
                    float current = SmoothSpectrum[i];

                    // 下落减速（上升快，下落慢）
                    if (target > current)
                        SmoothSpectrum[i] = current * SmoothingFactor + target * (1 - SmoothingFactor);
                    else
                        SmoothSpectrum[i] = current * 0.98f; // 下落慢一点
                }
            }
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            IsCapturing = false;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopCapture();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}