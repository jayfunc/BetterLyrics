using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Memory;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using Timer = System.Timers.Timer;

namespace BetterLyrics.WinUI3.Providers;

public class UniversalMemoryReaderProvider : IUniversalMemoryReaderProvider
{
    private readonly object _lock = new();
    private readonly Timer _timer;

    public UniversalMemoryReaderProvider()
    {
        _timer = new Timer
        {
            Interval = 1000
        };
        _timer.Elapsed += Timer_Elapsed;
        _timer.AutoReset = true;
    }

    public MemoryReaderConfig Config { get; set; } = new();

    public event Action<double, double>? OnProgressChanged;

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (!Monitor.TryEnter(_lock)) return;

        try
        {
            var process = Process.GetProcessesByName(Config.ProcessName).FirstOrDefault();
            if (process == null) return;

            var access = ACCESS_MASK.GENERIC_ALL;
            using var hProcess = OpenProcess(access, false, (uint)process.Id);
            if (hProcess.IsInvalid) return;

            var currentTime = ReadValueFromConfig(hProcess, Config.CurrentTime);

            var totalDuration = ReadValueFromConfig(hProcess, Config.TotalDuration);

            if (currentTime >= 0 && totalDuration > 0)
                OnProgressChanged?.Invoke(currentTime, totalDuration);
            else if (currentTime >= 0) OnProgressChanged?.Invoke(currentTime, 0);
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    private double ReadValueFromConfig(SafeHPROCESS hProcess, MemoryAddressDefinition def)
    {
        if (string.IsNullOrEmpty(def.ModuleName)) return -1;

        // A. 获取模块基址
        var moduleBase = GetModuleBaseAddress(hProcess, def.ModuleName);
        if (moduleBase == IntPtr.Zero) return -1;

        // B. 计算最终读取地址
        IntPtr targetAddress;
        var startPtr = moduleBase + def.BaseOffset;

        if (def.PointerOffsets == null || def.PointerOffsets.Length == 0)
            // 直接读取模式
            targetAddress = startPtr;
        else
            // 多级指针模式
            targetAddress = GetAddressFromPointerChain(hProcess, startPtr, def.PointerOffsets);

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

    private IntPtr GetAddressFromPointerChain(SafeHPROCESS hProcess, IntPtr baseAddress, int[] offsets)
    {
        var currentPtr = baseAddress;

        // 如果是直读（没有偏移），直接返回
        if (offsets == null || offsets.Length == 0) return baseAddress;

        // 1. 读取基址存放的第一个指针
        long firstPtrValue;
        if (Config.Is64Bit)
            firstPtrValue = ReadInt64(hProcess, baseAddress);
        else
            firstPtrValue = ReadInt32(hProcess, baseAddress);

        if (firstPtrValue == 0) return IntPtr.Zero;
        currentPtr = (IntPtr)firstPtrValue;

        // 2. 循环遍历中间的偏移
        for (var i = 0; i < offsets.Length; i++)
        {
            // 计算目标地址
            var nextAddress = currentPtr + offsets[i];

            // 如果是最后一个偏移，直接返回该地址（不需要再读指针了，这已经是存数值的地址了）
            if (i == offsets.Length - 1) return nextAddress;

            // 读取下一级指针
            long nextPtrValue;
            if (Config.Is64Bit)
                nextPtrValue = ReadInt64(hProcess, nextAddress);
            else
                nextPtrValue = ReadInt32(hProcess, nextAddress);

            if (nextPtrValue == 0) return IntPtr.Zero;
            currentPtr = (IntPtr)nextPtrValue;
        }

        return IntPtr.Zero;
    }

    private static int ReadInt32(SafeHPROCESS hProcess, IntPtr address)
    {
        return ReadStruct<int>(hProcess, address);
    }

    private static long ReadInt64(SafeHPROCESS hProcess, IntPtr address)
    {
        return ReadStruct<long>(hProcess, address);
    }

    private static float ReadFloat(SafeHPROCESS hProcess, IntPtr address)
    {
        return ReadStruct<float>(hProcess, address);
    }

    private static double ReadDouble(SafeHPROCESS hProcess, IntPtr address)
    {
        return ReadStruct<double>(hProcess, address);
    }

    private static T ReadStruct<T>(SafeHPROCESS hProcess, IntPtr address) where T : struct
    {
        try
        {
            using var pBuffer = new SafeHGlobalHandle(Marshal.SizeOf<T>());
            if (ReadProcessMemory(hProcess, address, pBuffer, (nuint)Marshal.SizeOf<T>(), out _))
                return pBuffer.ToStructure<T>();
        }
        catch
        {
        }

        return default;
    }

    private static IntPtr GetModuleBaseAddress(SafeHPROCESS hProcess, string moduleName)
    {
        var hModules = new HINSTANCE[1024];
        var filterFlag = LIST_MODULES.LIST_MODULES_ALL;

        if (EnumProcessModulesEx(hProcess, hModules, (uint)(hModules.Length * IntPtr.Size), out var cbNeeded,
                filterFlag))
        {
            var moduleCount = (int)(cbNeeded / IntPtr.Size);
            var sb = new StringBuilder(256);

            for (var i = 0; i < moduleCount; i++)
                if (GetModuleBaseName(hProcess, hModules[i], sb, (uint)sb.Capacity) > 0)
                    if (sb.ToString().Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                        return hModules[i].DangerousGetHandle();
        }

        return IntPtr.Zero;
    }
}