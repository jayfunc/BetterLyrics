using Microsoft.UI.Dispatching;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public static class SystemVolumeHelper
    {
        private static MMDeviceEnumerator? _deviceEnumerator;
        private static MMDevice? _defaultDevice;
        private static DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        /// <summary>
        /// 当系统音量或静音状态改变时触发。
        /// </summary>
        public static event EventHandler<int>? VolumeNotification;

        static SystemVolumeHelper()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            if (_defaultDevice != null)
            {
                _defaultDevice.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
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
