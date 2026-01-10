using BetterLyrics.Core.Interfaces;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.PluginService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class PluginManagerControl : UserControl
    {
        public ObservableCollection<PluginDisplayModel> Plugins { get; } = new();

        public Visibility IsListEmpty => Plugins.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        private readonly IPluginService _pluginService;

        public PluginManagerControl()
        {
            this.InitializeComponent();

            _pluginService = Ioc.Default.GetRequiredService<IPluginService>();

            this.Loaded += (s, e) =>
            {
                RefreshPluginList();
            };
        }

        private void RefreshPluginList()
        {
            Plugins.Clear();

            var allPlugins = _pluginService.Plugins;

            foreach (var plugin in allPlugins)
            {
                Plugins.Add(new PluginDisplayModel(plugin));
            }

            Bindings.Update();
        }

        private async void OnInstallPluginClick(object sender, RoutedEventArgs e)
        {
            var file = await Helper.PickerHelper.PickSingleFileAsync<SettingsWindow>([".zip"]);

            if (file != null)
            {
                try
                {
                    // 显示加载条...

                    // 3. 调用我们在上一步写的 InstallPlugin 方法
                    _pluginService.InstallPlugin(file.Path);

                    // 4. 重新加载所有插件 (这会触发热重载)
                    _pluginService.LoadPlugins();

                    // 5. 刷新界面
                    RefreshPluginList();

                    ShowTip("安装成功", $"插件 {file.Name} 已安装。");
                }
                catch (Exception ex)
                {
                    ShowError("安装失败", ex.Message);
                }
            }
        }

        // 卸载按钮点击事件
        private async void OnUninstallClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is IPlugin plugin)
            {
                // 二次确认对话框
                ContentDialog deleteDialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = "卸载插件?",
                    Content = $"确定要删除 \"{plugin.Name}\" 吗？此操作无法撤销。",
                    PrimaryButtonText = "删除",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Close
                };

                var result = await deleteDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        // TODO: 在 PluginService 里加一个 UninstallPlugin 方法
                        // 逻辑：找到插件对应文件夹，Directory.Delete(path, true)
                        // _pluginService.UninstallPlugin(plugin.Id);

                        // 暂时我们只能刷新列表演示
                        RefreshPluginList();
                    }
                    catch (Exception ex)
                    {
                        ShowError("卸载失败", ex.Message);
                    }
                }
            }
        }

        // 简单的弹窗辅助方法
        private async void ShowTip(string title, string content)
        {
            ContentDialog dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = title,
                Content = content,
                CloseButtonText = "好"
            };
            await dialog.ShowAsync();
        }

        private async void ShowError(string title, string content)
        {
            ContentDialog dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = title,
                Content = content,
                CloseButtonText = "关闭"
            };
            await dialog.ShowAsync();
        }
    }
}
