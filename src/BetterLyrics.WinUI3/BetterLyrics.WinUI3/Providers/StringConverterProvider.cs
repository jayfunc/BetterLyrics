using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.WinUI3.Hooks;
using WanaKanaNet;

namespace BetterLyrics.WinUI3.Providers;

public class StringConverterProvider : IStringConverterProvider
{
    public string RomajiToKanji(string romaji)
    {
        var hiragana = WanaKana.ToKana(romaji, new WanaKanaOptions { ImeMode = ImeMode.ToHiragana })
            .Replace(" ", "");

        ImeHook.IFELanguage? ife = null;
        var resultPtr = IntPtr.Zero;

        try
        {
            var imeType = Type.GetTypeFromProgID("MSIME.Japan");
            if (imeType == null) return romaji; // 没装日文输入法，原样返回

            ife = (ImeHook.IFELanguage?)Activator.CreateInstance(imeType);
            if (ife?.Open() != 0) return romaji;

            var hr = ife.GetJMorphResult(
                (uint)ImeHook.ConversionRequest.Conversion,
                (uint)ImeHook.ConversionMode.HiraganaOut,
                hiragana.Length,
                hiragana,
                IntPtr.Zero,
                out resultPtr
            );

            if (hr == 0 && resultPtr != IntPtr.Zero)
            {
                var result = Marshal.PtrToStructure<ImeHook.MorphResult>(resultPtr);

                string? bestString = null;

                if (result.PtrToOutputString != IntPtr.Zero)
                    bestString = Marshal.PtrToStringUni(result.PtrToOutputString, result.OutputLength);

                Marshal.FreeCoTaskMem(resultPtr);

                return string.IsNullOrEmpty(bestString) ? romaji : bestString;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("IME Error: " + ex.Message);
        }
        finally
        {
            ife?.Close();
        }

        return romaji;
    }
}