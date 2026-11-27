using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class MediaFieldRuleDialog : ContentDialog
{
    public MediaFieldRuleEditorViewModel ViewModel { get; }

    public MediaFieldRuleDialog(MediaFieldRuleEditorViewModel viewModel, bool isNew)
    {
        ViewModel = viewModel;

        InitializeComponent();

        var localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();
        Title = localizationService.GetLocalizedString(
            isNew ? "MediaFieldRuleDialogAddTitle" : "MediaFieldRuleDialogEditTitle");
        PrimaryButtonText = localizationService.GetLocalizedString("Confirm");
        CloseButtonText = localizationService.GetLocalizedString("Cancel");

        IsPrimaryButtonEnabled = ViewModel.CanSave;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MediaFieldRuleEditorViewModel.CanSave))
        {
            IsPrimaryButtonEnabled = ViewModel.CanSave;
        }
    }
}
