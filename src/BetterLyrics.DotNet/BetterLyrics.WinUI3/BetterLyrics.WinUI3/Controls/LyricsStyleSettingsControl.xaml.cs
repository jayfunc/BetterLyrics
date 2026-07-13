using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class LyricsStyleSettingsControl : UserControl
{
    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus),
            typeof(LyricsStyleSettingsControl), new PropertyMetadata(null));

    private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

    public LyricsStyleSettingsControl()
    {
        InitializeComponent();
    }

    public LyricsWindowStatus LyricsWindowStatus
    {
        get => (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        LyricsCardStyleComboBox.Items.Clear();

        // 经典设计
        AddGroupHeader("LyricsSharePageGroupClassic");
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleMinimal"),
            Tag = "LyricsCardMinimalStyle", IsSelected = true
        });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleSwiss"), Tag = "LyricsCardSwissStyle"
        });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleMagazine"),
            Tag = "LyricsCardMagazineStyle"
        });

        // 实体质感
        AddGroupHeader("LyricsSharePageGroupPhysical");
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleVinyl"), Tag = "LyricsCardVinylStyle"
        });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
            { Content = _localizationService.GetLocalizedString("LyricsSharePageStyleCD"), Tag = "LyricsCardCDStyle" });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStylePolaroid"),
            Tag = "LyricsCardPolaroidStyle"
        });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleTicket"),
            Tag = "LyricsCardTicketStyle"
        });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleReceipt"),
            Tag = "LyricsCardReceiptStyle"
        });

        // 时光印记
        AddGroupHeader("LyricsSharePageGroupTracesOfTime");
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleJournal"),
            Tag = "LyricsCardJournalStyle"
        });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleStickyNote"),
            Tag = "LyricsCardStickyNoteStyle"
        });

        // 数码怀旧
        AddGroupHeader("LyricsSharePageGroupRetro");
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem { Content = "iPod", Tag = "LyricsCardPodStyle" });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
            { Content = "Windows Phone", Tag = "LyricsCardWindowsPhoneStyle" });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleRetroQQ"),
            Tag = "LyricsCardRetroQQStyle"
        });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleRetroMSN"),
            Tag = "LyricsCardRetroMSNStyle"
        });

        // 现代视窗
        AddGroupHeader("LyricsSharePageGroupModernVision");
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem { Content = "QQ", Tag = "LyricsCardQQStyle" });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem { Content = "微信", Tag = "LyricsCardWeChatStyle" });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem { Content = "WhatsApp", Tag = "LyricsCardWhatsAppStyle" });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem { Content = "Telegram", Tag = "LyricsCardTelegramStyle" });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem { Content = "LINE", Tag = "LyricsCardLINEStyle" });

        // 氛围创意
        AddGroupHeader("LyricsSharePageGroupAtmosphere");
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleCinematic"),
            Tag = "LyricsCardCinematicStyle"
        });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleCyberpunk"),
            Tag = "LyricsCardCyberpunkStyle"
        });

        // 国风雅韵
        AddGroupHeader("LyricsSharePageGroupChineseElegance");
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleInkWash"),
            Tag = "LyricsCardInkWashStyle"
        });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleAncientBook"),
            Tag = "LyricsCardAncientBookStyle"
        });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleBambooSlips"),
            Tag = "LyricsCardBambooSlipsStyle"
        });
        LyricsCardStyleComboBox.Items.Add(new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString("LyricsSharePageStyleDunhuang"),
            Tag = "LyricsCardDunhuangStyle"
        });
    }

    private void AddGroupHeader(string localizationKey)
    {
        var header = new ComboBoxItem
        {
            Content = _localizationService.GetLocalizedString(localizationKey),
            IsEnabled = false,
            Margin = new Thickness(0, 4, 0, 0)
        };
        LyricsCardStyleComboBox.Items.Add(header);
    }

    private void LyricsCardStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LyricsCardStyleComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            var styleKey = selectedItem.Tag?.ToString();
            if (!string.IsNullOrEmpty(styleKey)) LyricsWindowStatus.LyricsCardStyleKey = styleKey;
        }
    }

    private void LyricsLayerOrderListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        LyricsWindowStatus.LyricsStyleSettings.LyricsLayerOrder.Refresh();
    }
}