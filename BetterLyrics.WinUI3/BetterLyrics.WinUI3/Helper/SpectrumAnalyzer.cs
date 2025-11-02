using NAudio.Dsp;
using NAudio.Wave;
using System;

namespace BetterLyrics.WinUI3.Helper
{
    public partial class SpectrumAnalyzer : IDisposable
    {
        private WasapiLoopbackCapture? _capture;

        private int _sampleRate = 48000;
        private readonly int _fftLength = 2048;

        private readonly float[] _fftLeftBuffer;
        private readonly float[] _fftRightBuffer;

        private readonly Complex[] _fftLeftData;
        private readonly Complex[] _fftRightData;

        private float[]? _spectrumLeftData;
        private float[]? _spectrumRightData;
        private float[]? _spectrumData;

        private bool _disposed = false;

        private double[] _hammingWindow;

        private float[]? _currentSpectrum;
        public float[]? SmoothSpectrum { get; private set; }

        public int BarCount { get; set; } = 16;
        public int Sensitivity { get; set; } = 100;
        public float SmoothingFactor { get; set; } = 0.95f;
        public bool IsCapturing { get; private set; } = false;

        public SpectrumAnalyzer()
        {
            _fftLeftBuffer = new float[_fftLength];
            _fftLeftData = new Complex[_fftLength];
            _fftRightBuffer = new float[_fftLength];
            _fftRightData = new Complex[_fftLength];
            _hammingWindow = new double[_fftLength];
            //汉明窗
            for (int i = 0; i < _fftLength; i++)
            {
                _hammingWindow[i] = 0.54 - 0.46 * Math.Cos((2 * Math.PI * i) / (_fftLength - 1));
            }
        }

        public void StartCapture()
        {
            try
            {
                _currentSpectrum = new float[BarCount];
                SmoothSpectrum = new float[BarCount];

                _capture = new();

                _sampleRate = _capture.WaveFormat.SampleRate;
                _spectrumLeftData = new float[(int)(24000.0f / _sampleRate * _fftLength) / 2];
                _spectrumRightData = new float[(int)(24000.0f / _sampleRate * _fftLength) / 2];
                _spectrumData = new float[(int)(24000.0f / _sampleRate * _fftLength)];
                _capture.DataAvailable += OnDataAvailable;
                _capture.RecordingStopped += OnRecordingStopped;
                _capture.StartRecording();

                IsCapturing = true;
            }
            catch (Exception) { }
        }

        public void StopCapture()
        {
            _capture?.DataAvailable -= OnDataAvailable;
            _capture?.RecordingStopped -= OnRecordingStopped;
            _capture?.StopRecording();

            IsCapturing = false;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_disposed || e.BytesRecorded == 0) return;
            // 将字节转换为浮点数
            int samples = e.BytesRecorded / 8;
            if (samples < _fftLength) return;
            for (int i = 0; i < _fftLength; i++)
            {
                _fftLeftBuffer[i] = BitConverter.ToSingle(e.Buffer, i * 8);
                _fftRightBuffer[i] = BitConverter.ToSingle(e.Buffer, i * 8 + 4);
            }
            for (int i = 0; i < _fftLength; i++)
            {
                _fftLeftData[i].X = _fftLeftBuffer[i] * (float)_hammingWindow[i]; // Real part
                _fftLeftData[i].Y = 0;             // Imaginary part
                _fftRightData[i].X = _fftRightBuffer[i] * (float)_hammingWindow[i];
                _fftRightData[i].Y = 0;
            }

            if (_spectrumData == null || _spectrumRightData == null || _currentSpectrum == null)
            {
                return;
            }

            // FFT
            FastFourierTransform.FFT(true, (int)Math.Log(_fftLength, 2), _fftLeftData);
            FastFourierTransform.FFT(true, (int)Math.Log(_fftLength, 2), _fftRightData);
            for (int i = 0; i < _spectrumLeftData?.Length; i++)
            {
                float real = (float)_fftLeftData[i].X;
                float imaginary = (float)_fftLeftData[i].Y;
                float magnitude = (float)Math.Sqrt(real * real + imaginary * imaginary);
                float frequency = i * _sampleRate / _fftLength;
                float compensationFactor = GetCompensationFactor(frequency);
                _spectrumLeftData[i] = magnitude * compensationFactor;
                _spectrumRightData[i] = (float)Math.Sqrt((float)_fftRightData[i].X * (float)_fftRightData[i].X + (float)_fftRightData[i].Y * (float)_fftRightData[i].Y) * compensationFactor;
                for (int j = 0; j < _spectrumLeftData.Length; j++)
                {
                    _spectrumData[j] = _spectrumLeftData[_spectrumLeftData.Length - 1 - j];
                }
                Array.Copy(_spectrumRightData, 0, _spectrumData, _spectrumLeftData.Length, _spectrumRightData.Length);
            }

            for (int i = 0; i < BarCount; i++)
            {
                int index = (int)((float)i / BarCount * _spectrumData.Length);
                if (index < _spectrumData.Length)
                {
                    _currentSpectrum[i] = _spectrumData[index] * 250f * Sensitivity;
                }
            }

        }

        public void UpdateSmoothSpectrum()
        {
            if (SmoothSpectrum == null || _currentSpectrum == null)
            {
                return;
            }

            for (int i = 0; i < BarCount; i++)
            {
                SmoothSpectrum[i] = SmoothSpectrum[i] * SmoothingFactor +
                    _currentSpectrum[i] * (1 - SmoothingFactor);
            }
        }

        private float GetCompensationFactor(float freq)
        {
            // 补偿曲线
            float[] frequencies = { 20, 50, 100, 200, 500, 1000, 2000, 4000, 8000, 16000, 20000 };
            //float[] gains = { 0.5f, 0.3f, 0.4f, 0.6f, 0.8f, 1.0f, 1.2f, 1.3f, 1.1f, 0.9f, 0.8f };
            float[] gains = { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };
            if (freq <= frequencies[0])
            {
                return gains[0];
            }
            if (freq >= frequencies[frequencies.Length - 1])
            {
                return gains[gains.Length - 1];
            }
            int i = 0;
            while (freq > frequencies[i + 1])
            {
                i++;
            }
            // 线性插值
            float x1 = frequencies[i];
            float y1 = gains[i];
            float x2 = frequencies[i + 1];
            float y2 = gains[i + 1];
            return y1 + (freq - x1) * ((y2 - y1) / (x2 - x1));
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _capture?.Dispose();
                _disposed = true;
            }
        }
    }
}
