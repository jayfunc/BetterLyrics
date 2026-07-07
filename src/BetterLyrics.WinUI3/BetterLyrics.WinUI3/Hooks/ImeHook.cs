using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BetterLyrics.WinUI3.Hooks;

/// <summary>
///     Ref:
///     https://github.com/huanlin/nchinese/blob/b706c08b81eb301fe08dc282b0c00da639dece1d/src/obsolete/NChinese.Imm/WinApi/MsIme.cs
/// </summary>
public class ImeHook
{
    public static class Constants
    {
        public const string IID_IFELanguage = "019F7152-E6DB-11D0-83C3-00C04FDDB82E";
        public const string IID_IFELanguage2 = "21164102-C24A-11D1-851A-00C04FCC6B14";
        public const string IID_IFECommon = "019F7151-E6DB-11D0-83C3-00C04FDDB82E";
        public const string IID_IFEDictionary = "019F7153-E6DB-11D0-83C3-00C04FDDB82E";
    }

    /// <summary>
    ///     Request for conversion (dwRequest).
    /// </summary>
    public enum ConversionRequest
    {
        Conversion = 0x00010000,
        Reconversion = 0x00020000,
        ReverseConversion = 0x00030000
    }

    /// <summary>
    ///     Conversion mode (dwCMode).
    /// </summary>
    [Flags]
    public enum ConversionMode
    {
        MonoRuby = 0x00000002, // Mono-ruby
        NoPruning = 0x00000004, // No pruning
        KatakanaOut = 0x00000008, // 片假名
        HiraganaOut = 0x00000000, // 平假名 (default)
        HalfWidthOut = 0x00000010, // 半形輸出
        FullWidthOut = 0x00000020, // 全形輸出
        Bopomofo = 0x00000040, // ㄅㄆㄇㄈ（台灣）
        Hangul = 0x00000080, // 諺文（韓語字母）
        Pinyin = 0x00000100, // 中國拼音
        PreConv = 0x00000200, // Do conversion as follows:

        // - roma-ji to kana
        // - autocorrect before conversion
        // - periods, comma, and brackets
        Radical = 0x00000400,
        UnknownReading = 0x00000800,
        MergeCand = 0x00001000, // Merge display with same candidate
        Roman = 0x00002000,
        BestFirst = 0x00004000, // Only make 1st best
        UseNoRevWords = 0x00008000, // Use invalid revword on REV/RECONV.
        None = 0x01000000, // IME_SMODE_NONE
        PlauralClause = 0x02000000, // IME_SMODE_PLAURALCLAUSE
        SingleConvert = 0x04000000, // IME_SMODE_SINGLECONVERT
        Automatic = 0x08000000, // IME_SMODE_AUTOMATIC
        PhrasePredict = 0x1000000, // IME_SMODE_PHRASEPREDICT
        Conversation = 0x20000000, // IME_SMODE_CONVERSATION
        Name = PhrasePredict, // Name mode (MSKKIME)
        NoInvisibleChar = 0x40000000 // Remove invisible chars (e.g. tone mark)
    }

    /// <summary>
    ///     Error code.
    /// </summary>
    public enum Error
    {
        NoCand = 0x30, // not enough candidates
        NotEnoughtBuffer = 0x31, // out of string buffer
        NotEnoughWdd = 0x32, // out of WDD buffer
        LatgeInput = 0x33 // large input string
    }

    /// <summary>
    ///     IFELanguage interface.
    ///     Ref: http://msdn.microsoft.com/en-us/library/ms970145.aspx
    /// </summary>
    /// <remarks>The order of methods is relevant!</remarks>
    [ComImport]
    [Guid(Constants.IID_IFELanguage)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFELanguage
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Open();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Close();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetJMorphResult(
            [In] uint dwRequest,
            [In] uint dwCMode,
            [In] int cwchInput,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string pwchInput,
            [In] IntPtr pfCInfo,
            [Out] out IntPtr ppResult
        );

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetConversionModeCaps(
            [Out] out IntPtr ppResult
        );

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetPhonetic(
            [In] string str,
            [In] long start,
            [In] long length,
            [Out] out IntPtr result
        );
    }

#if X86 || ANYCPU
	    /// <summary>
	    /// Managed version of MORRSLT structure (see msime.h).
	    /// </summary>
	    [StructLayout(LayoutKind.Explicit, Size = 48, Pack = 1)]
	    public struct MorphResult
	    {
		    [FieldOffset(0)]	public uint Size;						// Total size of this block.
		    [FieldOffset(4)]	public IntPtr PtrToOutputString;
		    [FieldOffset(8)]	public ushort OutputLength;
		    [FieldOffset(10)]	public IntPtr PtrToReadingString;
		    [FieldOffset(14)]	public ushort ReadingStringLength;		
		    [FieldOffset(16)]	public IntPtr PtrToInputPosArray;		
		    [FieldOffset(20)]	public IntPtr PtrToOutputIdxWDDArray;
		    [FieldOffset(24)]	public IntPtr PtrToReadingIdxWDDArray;
		    [FieldOffset(28)]	public IntPtr PtrToMonoRubyPosArray;	// Pointer to an array of position of monoruby
		    [FieldOffset(32)]	public IntPtr PtrToWDDArray;
		    [FieldOffset(36)]	public int WDDArrayLength;
		    [FieldOffset(40)]	public IntPtr PtrToPrivateDataBuffer;
		    [FieldOffset(44)]	public IntPtr PtrToBlkBuffer;
	    }
#else
	/// <summary>
	///     64-bit managed version of MORRSLT structure (see msime.h).
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 84, Pack = 1)]
    public struct MorphResult
    {
        //TODO: Test on 64-bit Windows platform.
        [FieldOffset(0)] public uint Size; // Total size of this block.
        [FieldOffset(4)] public IntPtr PtrToOutputString;
        [FieldOffset(12)] public ushort OutputLength;
        [FieldOffset(14)] public IntPtr PtrToReadingString;
        [FieldOffset(22)] public ushort ReadingStringLength;
        [FieldOffset(24)] public IntPtr PtrToInputPosArray;
        [FieldOffset(32)] public IntPtr PtrToOutputIdxWDDArray;
        [FieldOffset(40)] public IntPtr PtrToReadingIdxWDDArray;
        [FieldOffset(48)] public IntPtr PtrToMonoRubyPosArray; // Pointer to an array of position of monoruby
        [FieldOffset(56)] public IntPtr PtrToWDDArray;
        [FieldOffset(64)] public int WDDArrayLength;
        [FieldOffset(68)] public IntPtr PtrToPrivateDataBuffer;
        [FieldOffset(76)] public IntPtr PtrToBlkBuffer;
    }
#endif
}

// TODO: Managed version of WDD struct (Do we really need it?)