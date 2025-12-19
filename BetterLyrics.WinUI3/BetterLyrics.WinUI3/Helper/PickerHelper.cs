using BetterLyrics.WinUI3.Hooks;
using DevWinUI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.Helper
{
    public class PickerHelper
    {
        public static async Task<StorageFolder?> PickSingleFolderAsync<T>()
        {
            var window = WindowHook.GetWindow<T>();
            if (window == null) return null;

            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();

            return folder;
        }

        public static async Task<StorageFile?> PickSingleFileAsync<T>(string[] fileTypeFilter)
        {
            var window = WindowHook.GetWindow<T>();
            if (window == null) return null;

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.AddRange(fileTypeFilter);

            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();

            return file;
        }

        public static async Task<StorageFile?> PickSaveFileAsync<T>(IDictionary<string, IList<string>> fileTypeChoices)
        {
            var window = WindowHook.GetWindow<T>();

            return await PickSaveFileAsync(window, fileTypeChoices);
        }

        public static async Task<StorageFile?> PickSaveFileAsync<T>(T? window, IDictionary<string, IList<string>> fileTypeChoices)
        {
            if (window == null) return null;

            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.FileTypeChoices.AddRange(fileTypeChoices);

            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();

            return file;
        }
    }
}
