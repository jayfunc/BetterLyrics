using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

namespace BetterLyrics.WinUI3.Helper
{
    public class KugouMemoryReader
    {
        private const string PROCESS_NAME = "KuGou"; // 酷狗进程名

        // === 1. 当前进度 (Double) ===
        private const string MODULE_TIME = "kgplayer.dll";
        private const int OFFSET_TIME = 0x407418;

        // === 2. 总时长 (5级指针, Int32 毫秒) ===
        private const string MODULE_DURATION = "kugou.dll"; // 注意：截图显示这个是在 kugou.dll
        private const int BASE_OFFSET_DURATION = 0x01983DE8; // 截图最下面那个大数
        // 偏移量列表：从下往上填 (不包含最后一个 23C)
        private readonly int[] _pointerOffsets = { 0x0, 0x20, 0x8, 0x2C };
        private const int FINAL_OFFSET_DURATION = 0x23C;     // 截图最上面那个偏移

        public event Action<double, double>? OnProgressChanged; // 修改事件：同时返回 当前时间 和 总时长
        private DispatcherTimer _timer;

        public KugouMemoryReader()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, object e)
        {
            // 获取酷狗进程
            var process = Process.GetProcessesByName(PROCESS_NAME).FirstOrDefault();
            if (process == null) return;

            // 打开句柄
            var access = ACCESS_MASK.GENERIC_ALL;
            using SafeHPROCESS hProcess = OpenProcess(access, false, (uint)process.Id);
            if (hProcess.IsInvalid) return;

            // === A. 读取当前时间 (秒) ===
            double currentTime = -1;
            IntPtr timeModule = GetModuleBaseAddress(hProcess, MODULE_TIME);
            if (timeModule != IntPtr.Zero)
            {
                currentTime = ReadDouble(hProcess, timeModule + OFFSET_TIME);
            }

            // === B. 读取总时长 (秒) ===
            double totalDuration = 0;
            IntPtr durationModule = GetModuleBaseAddress(hProcess, MODULE_DURATION);
            if (durationModule != IntPtr.Zero)
            {
                // 1. 先拿到基地址: kugou.dll + 01983DE8
                IntPtr basePtrAddress = durationModule + BASE_OFFSET_DURATION;

                // 2. 执行多级指针跳跃
                IntPtr finalAddress = ReadMultilevelPointer(hProcess, basePtrAddress, _pointerOffsets);

                // 3. 读取最终数值 (注意：截图显示它是 4 Bytes 的毫秒数)
                if (finalAddress != IntPtr.Zero)
                {
                    int durationMs = ReadInt32(hProcess, finalAddress + FINAL_OFFSET_DURATION);
                    if (durationMs > 0)
                    {
                        totalDuration = durationMs / 1000.0; // 毫秒转秒
                    }
                }
            }

            // 触发事件 (当前时间, 总时长)
            if (currentTime >= 0)
            {
                OnProgressChanged?.Invoke(currentTime, totalDuration);
            }
        }

        // --- 核心工具方法 ---

        /// <summary>
        /// 多级指针读取器
        /// </summary>
        private IntPtr ReadMultilevelPointer(SafeHPROCESS hProcess, IntPtr baseAddress, int[] offsets)
        {
            IntPtr currentPtr = baseAddress;

            // 第一次读取：读取基址里的值
            // 注意：酷狗是32位程序，指针长度是 4字节 (Int32)
            int ptrValue = ReadInt32(hProcess, currentPtr);
            if (ptrValue == 0) return IntPtr.Zero;

            currentPtr = (IntPtr)ptrValue;

            // 循环遍历中间的偏移量
            foreach (var offset in offsets)
            {
                // 下一级地址 = 当前指针值 + 偏移量
                IntPtr nextAddress = currentPtr + offset;

                // 读取该地址指向的新指针
                ptrValue = ReadInt32(hProcess, nextAddress);

                // 如果链条断了（读到了0），就直接退出
                if (ptrValue == 0) return IntPtr.Zero;

                currentPtr = (IntPtr)ptrValue;
            }

            // 返回计算完所有中间偏移后的最终基址
            return currentPtr;
        }

        private int ReadInt32(SafeHPROCESS hProcess, IntPtr address)
        {
            try
            {
                using var pBuffer = new SafeHGlobalHandle(sizeof(int));
                if (ReadProcessMemory(hProcess, address, pBuffer, sizeof(int), out _))
                {
                    return pBuffer.ToStructure<int>();
                }
            }
            catch { }
            return 0;
        }

        private double ReadDouble(SafeHPROCESS hProcess, IntPtr address)
        {
            try
            {
                using var pBuffer = new SafeHGlobalHandle(sizeof(double));
                if (ReadProcessMemory(hProcess, address, pBuffer, sizeof(double), out _))
                {
                    return pBuffer.ToStructure<double>();
                }
            }
            catch { }
            return 0;
        }

        // ... GetModuleBaseAddress 保持你之前改好的 EnumProcessModulesEx 版本 ...
        private IntPtr GetModuleBaseAddress(SafeHPROCESS hProcess, string moduleName)
        {
            var hModules = new HINSTANCE[1024];
            var filterFlag = LIST_MODULES.LIST_MODULES_ALL; // 必须用 ALL

            if (EnumProcessModulesEx(hProcess, hModules, (uint)(hModules.Length * IntPtr.Size), out var cbNeeded, filterFlag))
            {
                int moduleCount = (int)(cbNeeded / IntPtr.Size);
                var sb = new StringBuilder(256);

                for (int i = 0; i < moduleCount; i++)
                {
                    if (GetModuleBaseName(hProcess, hModules[i], sb, (uint)sb.Capacity) > 0)
                    {
                        if (sb.ToString().Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                        {
                            return hModules[i].DangerousGetHandle();
                        }
                    }
                }
            }
            return IntPtr.Zero;
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();
    }
}