using System;
using System.Runtime.InteropServices;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace BetterLyrics.Core.Helpers;

/// <summary>
/// A custom implementation of WasapiLoopbackCapture that fixes the unhandled 
/// InvalidCastException crash inside NAudio's background thread when an audio device is removed.
/// </summary>
public class SafeWasapiLoopbackCapture : IWaveIn
{
    private const long REFTIMES_PER_SEC = 10000000;
    private const long REFTIMES_PER_MILLISEC = 10000;
    private volatile bool _stopRequested;
    private volatile bool _captureThreadActive;
    private Thread? _captureThread;
    private AudioClient? _audioClient;
    private int _bytesPerFrame;
    private WaveFormat? _waveFormat;
    private readonly SynchronizationContext? _syncContext;
    private bool _isInitialized;
    private readonly MMDevice _captureDevice;
    private byte[]? _recordBuffer;

    public event EventHandler<WaveInEventArgs>? DataAvailable;
    public event EventHandler<StoppedEventArgs>? RecordingStopped;

    public SafeWasapiLoopbackCapture() : this(GetDefaultLoopbackCaptureDevice())
    {
    }

    public SafeWasapiLoopbackCapture(MMDevice captureDevice)
    {
        _syncContext = SynchronizationContext.Current;
        _captureDevice = captureDevice;
        _audioClient = captureDevice.AudioClient;
        _waveFormat = _audioClient.MixFormat;
    }

    public static MMDevice GetDefaultLoopbackCaptureDevice()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    public WaveFormat WaveFormat
    {
        get => _waveFormat!;
        set => throw new InvalidOperationException("WaveFormat cannot be set for WASAPI Loopback Capture");
    }

    public void StartRecording()
    {
        if (_captureThreadActive) return;

        if (!_isInitialized)
        {
            InitializeCaptureDevice();
        }

        _stopRequested = false;
        _captureThreadActive = true;
        _captureThread = new Thread(CaptureThread) { IsBackground = true };
        _captureThread.Start();
    }

    public void StopRecording()
    {
        _stopRequested = true;
    }

    private void InitializeCaptureDevice()
    {
        if (_isInitialized || _audioClient == null)
            return;

        long requestedDuration = REFTIMES_PER_MILLISEC * 100;

        if (!_audioClient.IsFormatSupported(AudioClientShareMode.Shared, WaveFormat))
        {
            throw new ArgumentException("Unsupported Wave Format");
        }

        // Using standard Loopback flag, polling mode
        var streamFlags = AudioClientStreamFlags.Loopback;

        _audioClient.Initialize(AudioClientShareMode.Shared, streamFlags, requestedDuration, 0, _waveFormat, Guid.Empty);

        _bytesPerFrame = _waveFormat!.Channels * _waveFormat.BitsPerSample / 8;
        _recordBuffer = new byte[_audioClient.BufferSize * _bytesPerFrame];
        _isInitialized = true;
    }

    private void CaptureThread()
    {
        Exception? exception = null;
        try
        {
            DoRecording();
        }
        catch (Exception e)
        {
            exception = e;
        }
        finally
        {
            _captureThreadActive = false;
            RaiseRecordingStopped(exception);
        }
    }

    private void DoRecording()
    {
        if (_audioClient == null) return;

        int bufferFrameCount = _audioClient.BufferSize;

        // Calculate actual duration
        long actualDuration = (long)((double)REFTIMES_PER_SEC * bufferFrameCount / _waveFormat!.SampleRate);
        int sleepMilliseconds = (int)(actualDuration / REFTIMES_PER_MILLISEC / 2);

        var captureClient = _audioClient.AudioCaptureClient;

        try
        {
            _audioClient.Start();
        }
        catch
        {
            // Fails to start if device already lost
            return; 
        }

        try
        {
            while (!_stopRequested)
            {
                Thread.Sleep(sleepMilliseconds);
                ReadNextPacket(captureClient);
            }
        }
        finally
        {
            try
            {
                // 🔥 CRITICAL FIX: Wrap Stop in try-catch to avoid unhandled COM exceptions 🔥
                // causing the whole application to crash when the device is disconnected.
                _audioClient.Stop();
            }
            catch
            {
                // Ignore InvalidCastException or any COM exception
            }
        }
    }

    private void ReadNextPacket(AudioCaptureClient captureClient)
    {
        try
        {
            int packetSize = captureClient.GetNextPacketSize();
            int recordBufferOffset = 0;

            while (packetSize != 0)
            {
                IntPtr buffer = captureClient.GetBuffer(out int framesAvailable, out AudioClientBufferFlags flags);
                int bytesAvailable = framesAvailable * _bytesPerFrame;

                int spaceRemaining = Math.Max(0, _recordBuffer!.Length - recordBufferOffset);
                if (spaceRemaining < bytesAvailable && recordBufferOffset > 0)
                {
                    DataAvailable?.Invoke(this, new WaveInEventArgs(_recordBuffer, recordBufferOffset));
                    recordBufferOffset = 0;
                }

                // Only copy data if it's not marked as silent
                if ((flags & AudioClientBufferFlags.Silent) != AudioClientBufferFlags.Silent)
                {
                    Marshal.Copy(buffer, _recordBuffer, recordBufferOffset, bytesAvailable);
                }
                else
                {
                    Array.Clear(_recordBuffer, recordBufferOffset, bytesAvailable);
                }

                recordBufferOffset += bytesAvailable;
                captureClient.ReleaseBuffer(framesAvailable);

                packetSize = captureClient.GetNextPacketSize();
            }

            if (recordBufferOffset > 0)
            {
                DataAvailable?.Invoke(this, new WaveInEventArgs(_recordBuffer, recordBufferOffset));
            }
        }
        catch
        {
            // Ignore read errors when device disconnects
            _stopRequested = true;
        }
    }

    private void RaiseRecordingStopped(Exception? e)
    {
        var handler = RecordingStopped;
        if (handler != null)
        {
            if (_syncContext == null)
            {
                handler(this, new StoppedEventArgs(e));
            }
            else
            {
                _syncContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
            }
        }
    }

    public void Dispose()
    {
        StopRecording();

        if (_captureThread != null)
        {
            _captureThread.Join(500); // Wait for thread to exit cleanly
            _captureThread = null;
        }

        if (_audioClient != null)
        {
            _audioClient.Dispose();
            _audioClient = null;
        }

        _captureDevice?.Dispose();
    }
}
