using Microsoft.UI.Dispatching;
using System;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.CoreAudio;

namespace BetterLyrics.WinUI3.Helper
{
    public static class SystemVolumeHelper
    {
        private readonly static IMMDeviceEnumerator _deviceEnumerator = new();
        private static IAudioEndpointVolume? _endpointVolume = null;
        private static VolumeCallbackImpl? _callbackImpl;
        private static int _masterVolume = 0;
        private static DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public static event Action<int>? VolumeChanged;

        static SystemVolumeHelper()
        {
            var device = _deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
            if (device != null)
            {
                device.Activate(typeof(IAudioEndpointVolume).GUID, 0, null, out var obj);
                if (obj is IAudioEndpointVolume endpointVolume)
                {
                    _endpointVolume = endpointVolume;
                    _callbackImpl = new VolumeCallbackImpl();
                    _endpointVolume.RegisterControlChangeNotify(_callbackImpl);
                }
            }
        }

        /// <summary>
        /// 获取当前系统主音量（0~100）。
        /// </summary>
        public static int GetMasterVolume()
        {
            if (_endpointVolume != null)
            {
                float level = _endpointVolume.GetMasterVolumeLevelScalar();
                _masterVolume = (int)(level * 100);
            }

            return _masterVolume;
        }

        /// <summary>
        /// 设置当前系统主音量（0~100）。
        /// </summary>
        public static void SetMasterVolume(int volume)
        {
            if (_masterVolume == volume) return;

            _masterVolume = volume;
            _endpointVolume?.SetMasterVolumeLevelScalar(_masterVolume / 100f, Guid.Empty);
        }

        // 内部回调实现
        private class VolumeCallbackImpl : IAudioEndpointVolumeCallback
        {
            HRESULT IAudioEndpointVolumeCallback.OnNotify(nint pNotify)
            {
                var data = pNotify.ToStructure<AUDIO_VOLUME_NOTIFICATION_DATA>();
                _masterVolume = (int)(data.fMasterVolume * 100);
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    VolumeChanged?.Invoke(_masterVolume);
                });
                return HRESULT.S_OK;
            }
        }
    }
}