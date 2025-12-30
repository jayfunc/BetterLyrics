using Microsoft.UI.Dispatching;
using NAudio.CoreAudioApi;
using System;

namespace BetterLyrics.WinUI3.Hooks
{
    public static class SystemVolumeHook
    {
        private static MMDeviceEnumerator? _deviceEnumerator;
        private static MMDevice? _defaultDevice;
        private static DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        /// <summary>
        /// 当系统音量或静音状态改变时触发。
        /// </summary>
        public static event EventHandler<int>? VolumeNotification;

        static SystemVolumeHook()
        {
            try
            {
                _deviceEnumerator = new MMDeviceEnumerator();
                // 找不到设备会抛出异常，在这里截获它
                _defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                if (_defaultDevice != null)
                {
                    _defaultDevice.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
                }
            }
            catch (Exception ex)
            {
                _defaultDevice = null;
            }
        }

        private static void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                VolumeNotification?.Invoke(null, (int)(data.MasterVolume * 100));
            });
        }

        /// <summary>
        /// 获取或设置系统主音量 (0 到 100)。
        /// </summary>
        public static int MasterVolume
        {
            get
            {
                if (_defaultDevice == null)
                    return 0;

                return (int)(_defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            }
            set
            {
                if (_defaultDevice == null)
                    return;

                _defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = value / 100f;
            }
        }
    }
}
