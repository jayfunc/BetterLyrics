using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.WinUI3.Hooks;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WanaKanaNet;

namespace BetterLyrics.WinUI3.Providers
{
    public class StringConverterProvider : IStringConverterProvider
    {
        public string RomajiToKanji(string romaji)
        {
            string hiragana = WanaKana.ToKana(romaji, new WanaKanaOptions() { ImeMode = ImeMode.ToHiragana }).Replace(" ", "");

            ImeHook.IFELanguage? ife = null;
            IntPtr resultPtr = IntPtr.Zero;

            try
            {
                Type? imeType = Type.GetTypeFromProgID("MSIME.Japan");
                if (imeType == null) return romaji; // 没装日文输入法，原样返回

                ife = (ImeHook.IFELanguage?)Activator.CreateInstance(imeType);
                if (ife?.Open() != 0) return romaji;

                int hr = ife.GetJMorphResult(
                    (uint)ImeHook.ConversionRequest.Conversion,
                    (uint)ImeHook.ConversionMode.HiraganaOut,
                    hiragana.Length,
                    hiragana,
                    IntPtr.Zero,
                    out resultPtr
                );

                if (hr == 0 && resultPtr != IntPtr.Zero)
                {
                    ImeHook.MorphResult result = Marshal.PtrToStructure<ImeHook.MorphResult>(resultPtr);

                    string? bestString = null;

                    if (result.PtrToOutputString != IntPtr.Zero)
                    {
                        bestString = Marshal.PtrToStringUni(result.PtrToOutputString, result.OutputLength);
                    }

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
}