using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class MediaFieldCorrectionControl : BaseUserControl
{
    private readonly ILocalizationService _localizationService =
        Ioc.Default.GetRequiredService<ILocalizationService>();

    public PlaybackSettingsControlViewModel ViewModel => (PlaybackSettingsControlViewModel)DataContext;

    public MediaFieldCorrectionControl()
    {
        InitializeComponent();
    }

    protected override void BindGeneratedControls()
    {
        Bindings?.Update();
    }

    protected override void UnbindGeneratedControls()
    {
        Bindings?.StopTracking();
    }

    private void SnippetListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        InsertSnippetButton.Flyout?.Hide();

        if (e.ClickedItem is not MediaFieldCorrectionSnippetItemViewModel snippet) return;

        ViewModel.AddFieldCorrectionSnippet(snippet);
    }

    private async void AddCustomRule_Click(object sender, RoutedEventArgs e)
    {
        var rule = ViewModel.CreateFieldCorrectionRule();
        var edited = await ShowRuleDialogAsync(rule, true);

        if (edited != null) ViewModel.AddFieldCorrectionRule(edited);
    }

    private async void EditRule_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not MediaFieldRuleItemViewModel item) return;

        var edited = await ShowRuleDialogAsync(item.Rule, false);
        if (edited == null) return;

        item.Rule.CopyFrom(edited);
        ViewModel.RefreshFieldCorrectionPreview();
    }

    private void DeleteRule_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not MediaFieldRuleItemViewModel item) return;

        ViewModel.RemoveFieldCorrectionRule(item);
    }

    private void FieldCorrectionRulesListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        ViewModel.PersistFieldCorrectionRuleOrder();
        ViewModel.RefreshFieldCorrectionRules();
    }

    private async void ImportRules_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmAsync("MediaFieldCorrectionImportConfirmTitle",
                "MediaFieldCorrectionImportConfirmDescription"))
        {
            return;
        }

        ViewModel.ImportFieldCorrectionRulesCommand.Execute(null);
    }

    private void ExportRules_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ExportFieldCorrectionRulesCommand.Execute(null);
    }

    private void ResetRules_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ResetFieldCorrectionRules();
    }

    private async void ClearRules_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmAsync("MediaFieldCorrectionClearConfirmTitle",
                "MediaFieldCorrectionClearConfirmDescription"))
        {
            return;
        }

        ViewModel.ClearFieldCorrectionRules();
    }

    private async Task<bool> ConfirmAsync(string titleKey, string descriptionKey)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            Title = _localizationService.GetLocalizedString(titleKey),
            Content = _localizationService.GetLocalizedString(descriptionKey),
            PrimaryButtonText = _localizationService.GetLocalizedString("Confirm"),
            CloseButtonText = _localizationService.GetLocalizedString("Cancel"),
            DefaultButton = ContentDialogButton.Close
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private async Task<MediaFieldMappingRule?> ShowRuleDialogAsync(MediaFieldMappingRule rule, bool isNew)
    {
        var editor = ViewModel.CreateFieldCorrectionRuleEditor(rule);
        var dialog = new MediaFieldRuleDialog(editor, isNew) { XamlRoot = XamlRoot };

        return await dialog.ShowAsync() == ContentDialogResult.Primary ? editor.Rule : null;
    }
}
