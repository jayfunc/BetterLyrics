using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
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
    public class UniversalMemoryReader
    {
        private readonly MemoryReaderConfig _config;
        private readonly DispatcherTimer _timer;

        public event Action<double, double>? OnProgressChanged;

        public UniversalMemoryReader(MemoryReaderConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, object e)
        {
            var process = Process.GetProcessesByName(_config.ProcessName).FirstOrDefault();
            if (process == null) return;

            var access = ACCESS_MASK.GENERIC_ALL;
            using SafeHPROCESS hProcess = OpenProcess(access, false, (uint)process.Id);
            if (hProcess.IsInvalid) return;

            // 1. 读取当前进度
            double currentTime = ReadValueFromConfig(hProcess, _config.CurrentTime);

            // 2. 读取总时长
            double totalDuration = ReadValueFromConfig(hProcess, _config.TotalDuration);

            // 3. 触发事件 (过滤无效值)
            if (currentTime >= 0 && totalDuration > 0)
            {
                OnProgressChanged?.Invoke(currentTime, totalDuration);
            }
            else if (currentTime >= 0)
            {
                // 如果获取不到总时长，至少返回当前进度
                OnProgressChanged?.Invoke(currentTime, 0);
            }
        }

        /// <summary>
        /// 根据配置读取并计算最终数值
        /// </summary>
        private double ReadValueFromConfig(SafeHPROCESS hProcess, MemoryAddressDefinition def)
        {
            if (string.IsNullOrEmpty(def.ModuleName)) return -1;

            // A. 获取模块基址
            IntPtr moduleBase = GetModuleBaseAddress(hProcess, def.ModuleName);
            if (moduleBase == IntPtr.Zero) return -1;

            // B. 计算最终读取地址
            IntPtr targetAddress;
            IntPtr startPtr = moduleBase + def.BaseOffset;

            if (def.PointerOffsets == null || def.PointerOffsets.Length == 0)
            {
                // 直接读取模式
                targetAddress = startPtr;
            }
            else
            {
                // 多级指针模式
                targetAddress = GetAddressFromPointerChain(hProcess, startPtr, def.PointerOffsets);
            }

            if (targetAddress == IntPtr.Zero) return -1;

            // C. 读取原始数值
            double rawValue = 0;
            switch (def.ValueType)
            {
                case MemoryValueType.Double:
                    rawValue = ReadDouble(hProcess, targetAddress);
                    break;
                case MemoryValueType.Float:
                    rawValue = ReadFloat(hProcess, targetAddress);
                    break;
                case MemoryValueType.Int32:
                    rawValue = ReadInt32(hProcess, targetAddress);
                    break;
                case MemoryValueType.Int64:
                    rawValue = ReadInt64(hProcess, targetAddress);
                    break;
            }

            // D. 应用单位转换 (如毫秒转秒)
            return rawValue * def.UnitScale;
        }

        /// <summary>
        /// 指针链遍历核心逻辑
        /// </summary>
        private IntPtr GetAddressFromPointerChain(SafeHPROCESS hProcess, IntPtr baseAddress, int[] offsets)
        {
            IntPtr currentPtr = baseAddress;

            // 如果是直读（没有偏移），直接返回
            if (offsets == null || offsets.Length == 0) return baseAddress;

            // 1. 读取基址存放的第一个指针
            long firstPtrValue;
            if (_config.Is64Bit)
            {
                firstPtrValue = ReadInt64(hProcess, baseAddress);
            }
            else
            {
                firstPtrValue = ReadInt32(hProcess, baseAddress);
            }

            if (firstPtrValue == 0) return IntPtr.Zero;
            currentPtr = (IntPtr)firstPtrValue;

            // 2. 循环遍历中间的偏移
            for (int i = 0; i < offsets.Length; i++)
            {
                // 计算目标地址
                IntPtr nextAddress = currentPtr + offsets[i];

                // 如果是最后一个偏移，直接返回该地址（不需要再读指针了，这已经是存数值的地址了）
                if (i == offsets.Length - 1)
                {
                    return nextAddress;
                }

                // 读取下一级指针
                long nextPtrValue;
                if (_config.Is64Bit)
                {
                    nextPtrValue = ReadInt64(hProcess, nextAddress);
                }
                else
                {
                    nextPtrValue = ReadInt32(hProcess, nextAddress);
                }

                if (nextPtrValue == 0) return IntPtr.Zero;
                currentPtr = (IntPtr)nextPtrValue;
            }

            return IntPtr.Zero;
        }
        // === 基础内存读取方法 ===

        private int ReadInt32(SafeHPROCESS hProcess, IntPtr address) => ReadStruct<int>(hProcess, address);
        private long ReadInt64(SafeHPROCESS hProcess, IntPtr address) => ReadStruct<long>(hProcess, address);
        private float ReadFloat(SafeHPROCESS hProcess, IntPtr address) => ReadStruct<float>(hProcess, address);
        private double ReadDouble(SafeHPROCESS hProcess, IntPtr address) => ReadStruct<double>(hProcess, address);

        private T ReadStruct<T>(SafeHPROCESS hProcess, IntPtr address) where T : struct
        {
            try
            {
                using var pBuffer = new SafeHGlobalHandle(Marshal.SizeOf<T>());
                if (ReadProcessMemory(hProcess, address, pBuffer, (nuint)Marshal.SizeOf<T>(), out _))
                {
                    return pBuffer.ToStructure<T>();
                }
            }
            catch { }
            return default;
        }

        private IntPtr GetModuleBaseAddress(SafeHPROCESS hProcess, string moduleName)
        {
            var hModules = new HINSTANCE[1024];
            var filterFlag = LIST_MODULES.LIST_MODULES_ALL;

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