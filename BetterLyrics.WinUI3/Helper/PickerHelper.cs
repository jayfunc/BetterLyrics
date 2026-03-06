using BetterLyrics.WinUI3.Hooks;
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
            foreach (var item in fileTypeFilter)
            {
                picker.FileTypeFilter.Add(item);
            }
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();

            return file;
        }

        public static async Task<StorageFile?> PickSaveFileAsync<T>(IDictionary<string, IList<string>> fileTypeChoices, string? suggestedFileName = null)
        {
            var window = WindowHook.GetWindow<T>();

            return await PickSaveFileAsync(window, fileTypeChoices, suggestedFileName);
        }

        public static async Task<StorageFile?> PickSaveFileAsync<T>(T? window, IDictionary<string, IList<string>> fileTypeChoices, string? suggestedFileName = null)
        {
            if (window == null) return null;

            var picker = new Windows.Storage.Pickers.FileSavePicker();
            foreach (var item in fileTypeChoices)
            {
                picker.FileTypeChoices.Add(item);
            }
            if (suggestedFileName != null)
            {
                picker.SuggestedFileName = suggestedFileName;
            }

            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();

            return file;
        }
    }
}
