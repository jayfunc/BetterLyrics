using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helpers;
using BetterLyrics.WinUI3.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace BetterLyrics.WinUI3.Controls;

[INotifyPropertyChanged]
public sealed partial class LayoutEditorControl : UserControl, IRecipient<PropertyChangedMessage<Rect>>
{
    public static readonly DependencyProperty LayoutProfileProperty =
        DependencyProperty.Register(nameof(LayoutProfile), typeof(LayoutProfile), typeof(LayoutEditorControl),
            new PropertyMetadata(null, OnLayoutProfileChanged));

    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus),
            typeof(LayoutEditorControl), new PropertyMetadata(null, OnWindowStatusChanged));

    private readonly Brush? _draggedBrush = null;
    private readonly DispatcherTimer _historyDebounceTimer;

    private readonly LayoutHistoryManager _historyManager = new();
    private readonly bool _isHoveringAddBtn = false;

    private readonly ILocalizationService _localizationService =
        Ioc.Default.GetRequiredService<ILocalizationService>();

    private int _dragCellOffsetRow, _dragCellOffsetCol;
    private double _dragExactOffsetX, _dragExactOffsetY;
    private HorizontalAlignment _draggedHAlign;
    private Thickness _draggedMargin;

    private int _draggedRowSpan = 1, _draggedColSpan = 1;
    private string _draggedText = "";
    private VerticalAlignment _draggedVAlign;

    private double _draggedWidth = 100, _draggedHeight = 40;
    private FrameworkElement? _dropPreviewGhost;
    private FrameworkElement? _floatingDragVisual;
    private string _hoveredHandle = "";

    private bool _isDraggingMinimap;
    private bool _isHandToolActive;
    private bool _isPanning;
    private bool _isResizing;
    private bool _isRestoringHistory;
    private bool _isScaleUpdatePending;
    private bool _isSpacePanning;

    private bool _isStartingDrag;


    private bool _isUpdatingUI;
    private Point _lastMinimapDragPos;
    private Point _panStartPos;
    private string _resizeDirection = "";
    private ComponentPlacement? _resizeTarget;
    private double _startScrollX;
    private double _startScrollY;
    private bool _wasHandToolActiveBeforeSpace;

    public LayoutEditorControl()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.RegisterAll(this);

        _historyDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _historyDebounceTimer.Tick += (s, e) =>
        {
            _historyDebounceTimer.Stop();
            if (!_isRestoringHistory) RecordState();
        };
    }

    [ObservableProperty] public partial ObservableCollection<ToolboxItem> AvailableToolboxItems { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(MaxRowIndex))]
    [NotifyPropertyChangedFor(nameof(MaxColIndex))]
    [NotifyPropertyChangedFor(nameof(MaxRowSpan))]
    [NotifyPropertyChangedFor(nameof(MaxColSpan))]
    public partial ComponentPlacement? SelectedPlacement { get; set; }

    [ObservableProperty] public partial double CurrentZoom { get; set; } = 1.0;

    [ObservableProperty] public partial double ScaledRowSpacing { get; set; }
    [ObservableProperty] public partial double ScaledColSpacing { get; set; }
    [ObservableProperty] public partial Thickness ScaledColPadding { get; set; }
    [ObservableProperty] public partial Thickness ScaledRowPadding { get; set; }

    [ObservableProperty] public partial int SelectedPropertyTabIndex { get; set; } = 0;

    public ObservableCollection<HeaderItemModel> ColumnHeaderItems { get; } = new();
    public ObservableCollection<HeaderItemModel> RowHeaderItems { get; } = new();

    public int MaxRowIndex => Math.Max(0, (LayoutProfile?.RowDefinitions?.Count ?? 1) - 1);
    public int MaxColIndex => Math.Max(0, (LayoutProfile?.ColumnDefinitions?.Count ?? 1) - 1);

    public int MaxRowSpan =>
        Math.Max(1, (LayoutProfile?.RowDefinitions?.Count ?? 1) - (SelectedPlacement?.Row ?? 0));

    public int MaxColSpan =>
        Math.Max(1, (LayoutProfile?.ColumnDefinitions?.Count ?? 1) - (SelectedPlacement?.Column ?? 0));

    public bool HasSelection => SelectedPlacement != null;

    public LayoutProfile LayoutProfile
    {
        get => (LayoutProfile)GetValue(LayoutProfileProperty);
        set => SetValue(LayoutProfileProperty, value);
    }

    public LyricsWindowStatus LyricsWindowStatus
    {
        get => (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    public void Receive(PropertyChangedMessage<Rect> message)
    {
        if (message.Sender == LyricsWindowStatus && message.PropertyName == nameof(LyricsWindowStatus.WindowBounds))
            UpdatePreviewAspectRatio();
    }

    public string FormatZoom(double zoom)
    {
        return $"{(int)(zoom * 100)}%";
    }

    private static void OnLayoutProfileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LayoutEditorControl control)
        {
            if (e.OldValue is INotifyPropertyChanged oldProfile)
                oldProfile.PropertyChanged -= control.OnModelPropertyChanged;

            if (e.NewValue is INotifyPropertyChanged newProfile)
                newProfile.PropertyChanged += control.OnModelPropertyChanged;

            control.UpdateToolbox();
            control.UpdateHeaders();

            control.RequestRender();

            if (!control._isRestoringHistory)
            {
                control._historyManager.Clear();
                control.RecordState();
            }
        }
    }

    private static void OnWindowStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LayoutEditorControl control)
        {
            control.UpdateToolbox();
            control.RequestRender();
        }
    }

    partial void OnSelectedPlacementChanging(ComponentPlacement? oldValue, ComponentPlacement? newValue)
    {
        // 临时把最大值放开到无限大，防止 NumberBox 在数据切换的瞬间对 Value 造成错误的截断
        if (PropRowBox != null) PropRowBox.Maximum = double.MaxValue;
        if (PropColBox != null) PropColBox.Maximum = double.MaxValue;
        if (PropRowSpanBox != null) PropRowSpanBox.Maximum = double.MaxValue;
        if (PropColSpanBox != null) PropColSpanBox.Maximum = double.MaxValue;
    }

    partial void OnSelectedPlacementChanged(ComponentPlacement? oldValue, ComponentPlacement? newValue)
    {
        if (oldValue is INotifyPropertyChanged oldModel)
            oldModel.PropertyChanged -= OnModelPropertyChanged;

        if (newValue is INotifyPropertyChanged newModel)
            newModel.PropertyChanged += OnModelPropertyChanged;

        UpdatePropertiesPanel();
        UpdateSelectionVisuals();

        SelectedPropertyTabIndex = newValue == null ? 0 : 1;
    }

    partial void OnCurrentZoomChanged(double value)
    {
        UpdateScaledLayout();
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isRestoringHistory || _isUpdatingUI) return;

        NotifyLimitsChanged();
        RequestRender();

        _historyDebounceTimer.Stop();
        _historyDebounceTimer.Start();
    }

    public void RequestRender()
    {
        if (_isStartingDrag) return;
        RenderPreviewGrid();
        UpdatePropertiesPanel();
        WeakReferenceMessenger.Default.Send(new LayoutChangedMessage());
    }

    private void UpdatePropertiesPanel()
    {
        _isUpdatingUI = true;

        SelectedComponentLabel.Text = SelectedPlacement != null
            ? SelectedPlacement.DisplayName
            : _localizationService.GetLocalizedString("LayoutEditorControlNoSelection");

        if (SelectedPlacement != null)
        {
            var isWidthAuto = double.IsNaN(SelectedPlacement.Width);
            PropWidthTypeBox.SelectedIndex = isWidthAuto ? 0 : 1;
            PropWidthBox.IsEnabled = !isWidthAuto;

            var isHeightAuto = double.IsNaN(SelectedPlacement.Height);
            PropHeightTypeBox.SelectedIndex = isHeightAuto ? 0 : 1;
            PropHeightBox.IsEnabled = !isHeightAuto;
        }

        _isUpdatingUI = false;
    }

    private void UpdateSelectionVisuals()
    {
        foreach (var child in PreviewGrid.Children)
            if (child is CanvasItemControl cic && cic.Placement != null)
                cic.IsSelected = cic.Placement == SelectedPlacement;
    }

    public void UpdateScaledLayout()
    {
        if (LayoutProfile == null) return;

        ScaledRowSpacing = LayoutProfile.RowSpacing * CurrentZoom;
        ScaledColSpacing = LayoutProfile.ColumnSpacing * CurrentZoom;
        ScaledColPadding = new Thickness(LayoutProfile.PaddingLeft * CurrentZoom, 0,
            LayoutProfile.PaddingRight * CurrentZoom, 0);
        ScaledRowPadding = new Thickness(0, LayoutProfile.PaddingTop * CurrentZoom, 0,
            LayoutProfile.PaddingBottom * CurrentZoom);

        for (var i = 0; i < ColumnHeaderItems.Count; i++)
        {
            ColumnHeaderItems[i].ItemSize = ColumnHeaderItems[i].BaseSize * CurrentZoom;
            ColumnHeaderItems[i].FollowingSpacing = i == ColumnHeaderItems.Count - 1 ? 0 : ScaledColSpacing;
        }

        for (var i = 0; i < RowHeaderItems.Count; i++)
        {
            RowHeaderItems[i].ItemSize = RowHeaderItems[i].BaseSize * CurrentZoom;
            RowHeaderItems[i].FollowingSpacing = i == RowHeaderItems.Count - 1 ? 0 : ScaledRowSpacing;
        }
    }

    public void UpdateToolbox()
    {
        if (LayoutProfile == null)
        {
            AvailableToolboxItems.Clear();
            return;
        }

        var expectedTypes = Enum.GetValues<ComponentType>().Where(t => t != ComponentType.None).ToList();
        var typesToShow = expectedTypes.Where(t => !LayoutProfile.Placements.Any(p => p.ComponentType == t))
            .ToList();

        for (var i = AvailableToolboxItems.Count - 1; i >= 0; i--)
            if (!typesToShow.Contains(AvailableToolboxItems[i].ComponentType))
                AvailableToolboxItems.RemoveAt(i);

        for (var i = 0; i < typesToShow.Count; i++)
        {
            var targetType = typesToShow[i];
            if (AvailableToolboxItems.Count <= i || AvailableToolboxItems[i].ComponentType != targetType)
            {
                var existingItem = AvailableToolboxItems.FirstOrDefault(x => x.ComponentType == targetType);
                if (existingItem != null)
                    AvailableToolboxItems.Move(AvailableToolboxItems.IndexOf(existingItem), i);
                else
                    AvailableToolboxItems.Insert(i, new ToolboxItem { ComponentType = targetType });
            }
        }
    }

    public void UpdateHeaders()
    {
        if (LayoutProfile == null) return;

        var colDefs = LayoutProfile.ColumnDefinitions;
        ColumnHeaderItems.Clear();
        for (var i = 0; i < colDefs.Count; i++)
            ColumnHeaderItems.Add(new HeaderItemModel
            {
                Index = i,
                Definition = colDefs[i],
                Parent = this,
                CanDelete = colDefs.Count > 1
            });

        var rowDefs = LayoutProfile.RowDefinitions;
        RowHeaderItems.Clear();
        for (var i = 0; i < rowDefs.Count; i++)
            RowHeaderItems.Add(new HeaderItemModel
            {
                Index = i,
                Definition = rowDefs[i],
                Parent = this,
                CanDelete = rowDefs.Count > 1
            });
    }

    [RelayCommand]
    private void AddRow()
    {
        LayoutProfile?.RowDefinitions.Add("1*");
        NotifyLimitsChanged();
        UpdateHeaders();
        RequestRender();
    }

    [RelayCommand]
    private void AddCol()
    {
        LayoutProfile?.ColumnDefinitions.Add("1*");
        NotifyLimitsChanged();
        UpdateHeaders();
        RequestRender();
    }

    [RelayCommand]
    private void RemoveRow()
    {
        if (LayoutProfile?.RowDefinitions.Count > 1)
        {
            var lastRow = LayoutProfile.RowDefinitions.Count - 1;
            for (var i = LayoutProfile.Placements.Count - 1; i >= 0; i--)
                if (LayoutProfile.Placements[i].Row >= lastRow)
                    LayoutProfile.Placements.RemoveAt(i);

            LayoutProfile.RowDefinitions.RemoveAt(lastRow);
            foreach (var p in LayoutProfile.Placements.Where(p => p.Row + p.RowSpan > lastRow))
                p.RowSpan = Math.Max(1, lastRow - p.Row);

            NotifyLimitsChanged();
            CheckSelectionValidity();
            UpdateToolbox();
            UpdateHeaders();
            RequestRender();
        }
    }

    [RelayCommand]
    private void RemoveCol()
    {
        if (LayoutProfile?.ColumnDefinitions.Count > 1)
        {
            var lastCol = LayoutProfile.ColumnDefinitions.Count - 1;
            for (var i = LayoutProfile.Placements.Count - 1; i >= 0; i--)
                if (LayoutProfile.Placements[i].Column >= lastCol)
                    LayoutProfile.Placements.RemoveAt(i);

            LayoutProfile.ColumnDefinitions.RemoveAt(lastCol);
            foreach (var p in LayoutProfile.Placements.Where(p => p.Column + p.ColumnSpan > lastCol))
                p.ColumnSpan = Math.Max(1, lastCol - p.Column);

            NotifyLimitsChanged();
            CheckSelectionValidity();
            UpdateToolbox();
            UpdateHeaders();
            RequestRender();
        }
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedPlacement != null)
        {
            LayoutProfile?.Placements.Remove(SelectedPlacement);
            SelectedPlacement = null;
            UpdateToolbox();
            RequestRender();
        }
    }

    [RelayCommand]
    private void InsertRowAction(int index)
    {
        InsertRowAt(index);
    }

    [RelayCommand]
    private void InsertRowAfterAction(int index)
    {
        InsertRowAt(index + 1);
    }

    [RelayCommand]
    private void DeleteRowAction(int index)
    {
        DeleteRowAt(index);
    }

    [RelayCommand]
    private void ToggleRowStarAction(int index)
    {
        var current = LayoutProfile.RowDefinitions[index];
        LayoutProfile.RowDefinitions[index] =
            current.Equals("Auto", StringComparison.OrdinalIgnoreCase) ? "1*" : "Auto";
        UpdateHeaders();
        RequestRender();
    }

    [RelayCommand]
    private void InsertColAction(int index)
    {
        InsertColAt(index);
    }

    [RelayCommand]
    private void InsertColAfterAction(int index)
    {
        InsertColAt(index + 1);
    }

    [RelayCommand]
    private void DeleteColAction(int index)
    {
        DeleteColAt(index);
    }

    [RelayCommand]
    private void ToggleColStarAction(int index)
    {
        var current = LayoutProfile.ColumnDefinitions[index];
        LayoutProfile.ColumnDefinitions[index] =
            current.Equals("Auto", StringComparison.OrdinalIgnoreCase) ? "1*" : "Auto";
        UpdateHeaders();
        RequestRender();
    }

    public void InsertRowAt(int index)
    {
        LayoutProfile.RowDefinitions.Insert(index, "1*");
        foreach (var p in LayoutProfile.Placements)
            if (p.Row >= index) p.Row++;
            else if (p.Row + p.RowSpan > index) p.RowSpan++;

        NotifyLimitsChanged();
        UpdateHeaders();
        RequestRender();
    }

    public void InsertColAt(int index)
    {
        LayoutProfile.ColumnDefinitions.Insert(index, "1*");
        foreach (var p in LayoutProfile.Placements)
            if (p.Column >= index) p.Column++;
            else if (p.Column + p.ColumnSpan > index) p.ColumnSpan++;

        NotifyLimitsChanged();
        UpdateHeaders();
        RequestRender();
    }

    public void DeleteRowAt(int index)
    {
        if (LayoutProfile.RowDefinitions.Count <= 1) return;

        var toRemove = LayoutProfile.Placements.Where(p => p.Row == index && p.RowSpan == 1).ToList();
        foreach (var p in toRemove) LayoutProfile.Placements.Remove(p);

        LayoutProfile.RowDefinitions.RemoveAt(index);
        foreach (var p in LayoutProfile.Placements)
            if (p.Row > index) p.Row--;
            else if (p.Row <= index && p.Row + p.RowSpan > index) p.RowSpan = Math.Max(1, p.RowSpan - 1);

        CheckSelectionValidity();
        UpdateToolbox();
        UpdateHeaders();
        RequestRender();
    }

    public void DeleteColAt(int index)
    {
        if (LayoutProfile.ColumnDefinitions.Count <= 1) return;

        var toRemove = LayoutProfile.Placements.Where(p => p.Column == index && p.ColumnSpan == 1).ToList();
        foreach (var p in toRemove) LayoutProfile.Placements.Remove(p);

        LayoutProfile.ColumnDefinitions.RemoveAt(index);
        foreach (var p in LayoutProfile.Placements)
            if (p.Column > index) p.Column--;
            else if (p.Column <= index && p.Column + p.ColumnSpan > index)
                p.ColumnSpan = Math.Max(1, p.ColumnSpan - 1);

        CheckSelectionValidity();
        UpdateToolbox();
        UpdateHeaders();
        RequestRender();
    }

    private void CheckSelectionValidity()
    {
        if (SelectedPlacement != null && !LayoutProfile.Placements.Contains(SelectedPlacement))
            SelectedPlacement = null;
    }

    private void NotifyLimitsChanged()
    {
        OnPropertyChanged(nameof(MaxRowIndex));
        OnPropertyChanged(nameof(MaxColIndex));
        OnPropertyChanged(nameof(MaxRowSpan));
        OnPropertyChanged(nameof(MaxColSpan));
    }

    private void RenderPreviewGrid()
    {
        if (LayoutProfile == null) return;

        ClearDragVisuals();

        PreviewGrid.Children.Clear();
        PreviewGrid.RowDefinitions.Clear();
        PreviewGrid.ColumnDefinitions.Clear();

        PreviewGrid.RowSpacing = LayoutProfile.RowSpacing;
        PreviewGrid.ColumnSpacing = LayoutProfile.ColumnSpacing;

        PreviewGrid.Padding = new Thickness(LayoutProfile.PaddingLeft, LayoutProfile.PaddingTop,
            LayoutProfile.PaddingRight, LayoutProfile.PaddingBottom);

        foreach (var rowDef in LayoutProfile.RowDefinitions)
            PreviewGrid.RowDefinitions.Add(new RowDefinition
                { Height = GridLengthExtensions.ParseGridLength(rowDef) });

        foreach (var colDef in LayoutProfile.ColumnDefinitions)
            PreviewGrid.ColumnDefinitions.Add(new ColumnDefinition
                { Width = GridLengthExtensions.ParseGridLength(colDef) });

        for (var r = 0; r < LayoutProfile.RowDefinitions.Count; r++)
        for (var c = 0; c < LayoutProfile.ColumnDefinitions.Count; c++)
        {
            var cellSlot = new Border
            {
                BorderBrush = BrushHelper.GetThemeBrush(this, "CardStrokeColorDefaultBrush"),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Colors.Transparent),
                MinHeight = 4,
                MinWidth = 4,
                CornerRadius = new CornerRadius(4)
            };
            Grid.SetRow(cellSlot, r);
            Grid.SetColumn(cellSlot, c);

            cellSlot.PointerPressed += (s, e) =>
            {
                SelectedPlacement = null;
                Focus(FocusState.Programmatic);
            };

            if (r == 0)
            {
                var colIndex = c;
                cellSlot.SizeChanged += (s, e) => SyncColumnSize(colIndex, e.NewSize.Width);
            }

            if (c == 0)
            {
                var rowIndex = r;
                cellSlot.SizeChanged += (s, e) => SyncRowSize(rowIndex, e.NewSize.Height);
            }

            PreviewGrid.Children.Add(cellSlot);
        }

        foreach (var placement in LayoutProfile.Placements.OrderBy(x => x.ComponentType))
        {
            var componentBlock = CreateComponentVisual(placement);
            Grid.SetRow(componentBlock, placement.Row);
            Grid.SetColumn(componentBlock, placement.Column);
            Grid.SetRowSpan(componentBlock, placement.RowSpan);
            Grid.SetColumnSpan(componentBlock, placement.ColumnSpan);
            PreviewGrid.Children.Add(componentBlock);
        }
    }

    private void SyncColumnSize(int colIndex, double width)
    {
        if (colIndex >= ColumnHeaderItems.Count) return;
        if (double.IsNormal(width) && Math.Abs(ColumnHeaderItems[colIndex].BaseSize - width) > 0.1)
        {
            ColumnHeaderItems[colIndex].BaseSize = width;
            RequestDeferredScaleUpdate();
        }
    }

    private void SyncRowSize(int rowIndex, double height)
    {
        if (rowIndex >= RowHeaderItems.Count) return;
        if (double.IsNormal(height) && Math.Abs(RowHeaderItems[rowIndex].BaseSize - height) > 0.1)
        {
            RowHeaderItems[rowIndex].BaseSize = height;
            RequestDeferredScaleUpdate();
        }
    }

    private void RequestDeferredScaleUpdate()
    {
        if (_isScaleUpdatePending) return;
        _isScaleUpdatePending = true;

        DispatcherQueue.TryEnqueue(() =>
        {
            _isScaleUpdatePending = false;
            UpdateScaledLayout();
        });
    }

    private FrameworkElement CreateComponentVisual(ComponentPlacement placement)
    {
        var isSelected = SelectedPlacement == placement;

        var control = new CanvasItemControl(placement, isSelected)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            MinWidth = 4,
            MinHeight = 4
        };

        Canvas.SetZIndex(control, isSelected ? 999 : 0);

        control.MainBorder.Tapped += (s, e) =>
        {
            SelectedPlacement = placement;
            Focus(FocusState.Programmatic);
            e.Handled = true;
        };
        control.MainBorder.DragStarting += CanvasItem_DragStarting;
        control.MainBorder.DropCompleted += Common_DropCompleted;

        if (isSelected)
        {
            void WireUpHandle(UIElement handle, string direction)
            {
                handle.PointerPressed += (s, e) => Handle_PointerPressed(s, e, direction, placement);
                handle.PointerMoved += Handle_PointerMoved;
                handle.PointerReleased += Handle_PointerReleased;
                handle.PointerCanceled += Handle_PointerReleased;
                handle.PointerEntered += (s, e) =>
                {
                    _hoveredHandle = direction;
                    UpdateCursor();
                };
                handle.PointerExited += (s, e) =>
                {
                    if (_hoveredHandle == direction) _hoveredHandle = "";
                    UpdateCursor();
                };
            }

            WireUpHandle(control.RightHandle, "Right");
            WireUpHandle(control.BottomHandle, "Bottom");
            WireUpHandle(control.CornerHandle, "Corner");
        }

        return control;
    }

    private void PropWidthTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedPlacement == null || _isUpdatingUI) return;

        if (PropWidthTypeBox.SelectedItem is ComboBoxItem item && item.Tag?.ToString() == "Auto")
        {
            PropWidthBox.IsEnabled = false;
            // 修改模型为 Auto
            SelectedPlacement.Width = double.NaN;
        }
        else
        {
            PropWidthBox.IsEnabled = true;
            // 如果原本是 Auto，切回 px 时给个默认值 100 方便操作
            if (double.IsNaN(SelectedPlacement.Width))
            {
                SelectedPlacement.Width = 100;
                PropWidthBox.Value = 100;
            }
        }

        RequestRender();
    }

    private void PropHeightTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedPlacement == null || _isUpdatingUI) return;

        if (PropHeightTypeBox.SelectedItem is ComboBoxItem item && item.Tag?.ToString() == "Auto")
        {
            PropHeightBox.IsEnabled = false;
            SelectedPlacement.Height = double.NaN;
        }
        else
        {
            PropHeightBox.IsEnabled = true;
            if (double.IsNaN(SelectedPlacement.Height))
            {
                SelectedPlacement.Height = 100;
                PropHeightBox.Value = 100;
            }
        }

        RequestRender();
    }

    private void Handle_PointerPressed(object sender, PointerRoutedEventArgs e, string direction,
        ComponentPlacement placement)
    {
        var handle = sender as UIElement;
        handle?.CapturePointer(e.Pointer);

        _isResizing = true;
        _resizeDirection = direction;
        _resizeTarget = placement;

        if (_dropPreviewGhost == null)
        {
            var color = Colors.White;
            var ghostContainer = new Grid { IsHitTestVisible = false };
            var cellHighlight = new Rectangle
            {
                Fill = new SolidColorBrush(color) { Opacity = 0.1 },
                Stroke = new SolidColorBrush(color) { Opacity = 0.6 },
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 4 }
            };

            var componentHighlight = new Border
            {
                Background = new SolidColorBrush(color) { Opacity = 0.4 },
                BorderBrush = new SolidColorBrush(color),
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(placement.MarginLeft, placement.MarginTop, placement.MarginRight,
                    placement.MarginBottom),
                HorizontalAlignment =
                    HorizontalAlignmentExtensions.FromAppHorizontalAlignment(placement.HorizontalAlignment),
                VerticalAlignment =
                    VerticalAlignmentExtensions.FromAppVerticalAlignment(placement.VerticalAlignment),
                Child = new TextBlock
                {
                    Text = placement.DisplayName,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Colors.White)
                }
            };

            ghostContainer.Children.Add(cellHighlight);
            ghostContainer.Children.Add(componentHighlight);
            _dropPreviewGhost = ghostContainer;
            Canvas.SetZIndex(_dropPreviewGhost, 9999);
            PreviewGrid.Children.Add(_dropPreviewGhost);
        }

        Grid.SetRow(_dropPreviewGhost, placement.Row);
        Grid.SetColumn(_dropPreviewGhost, placement.Column);
        Grid.SetRowSpan(_dropPreviewGhost, placement.RowSpan);
        Grid.SetColumnSpan(_dropPreviewGhost, placement.ColumnSpan);

        e.Handled = true;
    }

    private void Handle_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isResizing || _resizeTarget == null || _dropPreviewGhost == null) return;
        var pointerPos = e.GetCurrentPoint(PreviewGrid).Position;
        var (hoverRow, hoverCol) = GetGridCellFromPoint(pointerPos);

        var newRowSpan = _resizeTarget.RowSpan;
        var newColSpan = _resizeTarget.ColumnSpan;
        var maxRowSpan = LayoutProfile.RowDefinitions.Count - _resizeTarget.Row;
        var maxColSpan = LayoutProfile.ColumnDefinitions.Count - _resizeTarget.Column;

        if (_resizeDirection == "Right" || _resizeDirection == "Corner")
            newColSpan = Math.Clamp(hoverCol - _resizeTarget.Column + 1, 1, maxColSpan);
        if (_resizeDirection == "Bottom" || _resizeDirection == "Corner")
            newRowSpan = Math.Clamp(hoverRow - _resizeTarget.Row + 1, 1, maxRowSpan);

        Grid.SetRowSpan(_dropPreviewGhost, newRowSpan);
        Grid.SetColumnSpan(_dropPreviewGhost, newColSpan);
        e.Handled = true;
    }

    private void Handle_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isResizing) return;

        var handle = sender as UIElement;
        handle?.ReleasePointerCapture(e.Pointer);

        if (_dropPreviewGhost != null && _resizeTarget != null)
        {
            var newRowSpan = Grid.GetRowSpan(_dropPreviewGhost);
            var newColSpan = Grid.GetColumnSpan(_dropPreviewGhost);

            PreviewGrid.Children.Remove(_dropPreviewGhost);
            _dropPreviewGhost = null;

            _resizeTarget.RowSpan = newRowSpan;
            _resizeTarget.ColumnSpan = newColSpan;
        }

        _isResizing = false;
        _resizeTarget = null;

        RequestRender();
        UpdateCursor();
        e.Handled = true;
    }

    private void ToolboxItem_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is FrameworkElement element && element.DataContext is ToolboxItem item)
        {
            args.Data.SetText(item.ComponentType.ToString());
            args.Data.RequestedOperation = DataPackageOperation.Copy;

            _draggedRowSpan = 1;
            _draggedColSpan = 1;
            _draggedMargin = new Thickness(0);
            _draggedHAlign = HorizontalAlignment.Stretch;
            _draggedVAlign = VerticalAlignment.Stretch;
            _draggedText = item.DisplayName;

            _dragCellOffsetRow = 0;
            _dragCellOffsetCol = 0;

            _draggedWidth = element.ActualWidth;
            _draggedHeight = element.ActualHeight;
            var pointerPos = args.GetPosition(element);
            _dragExactOffsetX = pointerPos.X;
            _dragExactOffsetY = pointerPos.Y;
        }
    }

    private void CanvasItem_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is FrameworkElement element && element.Tag is ComponentPlacement placement)
        {
            _isStartingDrag = true;
            try
            {
                _draggedWidth = 0;
                _draggedHeight = 0;
                if (element.Parent is FrameworkElement sizingWrapper)
                {
                    _draggedWidth = sizingWrapper.ActualWidth;
                    _draggedHeight = sizingWrapper.ActualHeight;
                }

                args.Data.SetText(placement.ComponentType.ToString());
                args.Data.RequestedOperation = DataPackageOperation.Move;

                var pointerPos = args.GetPosition(PreviewGrid);
                var transform = element.TransformToVisual(PreviewGrid);
                var bounds = transform.TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
                _dragExactOffsetX = pointerPos.X - bounds.X;
                _dragExactOffsetY = pointerPos.Y - bounds.Y;

                _draggedRowSpan = placement.RowSpan;
                _draggedColSpan = placement.ColumnSpan;
                _draggedText = placement.DisplayName;
                _draggedMargin = new Thickness(placement.MarginLeft, placement.MarginTop, placement.MarginRight,
                    placement.MarginBottom);
                _draggedHAlign =
                    HorizontalAlignmentExtensions.FromAppHorizontalAlignment(placement.HorizontalAlignment);
                _draggedVAlign = VerticalAlignmentExtensions.FromAppVerticalAlignment(placement.VerticalAlignment);

                var (mouseRow, mouseCol) = GetGridCellFromPoint(pointerPos);
                _dragCellOffsetRow = mouseRow - placement.Row;
                _dragCellOffsetCol = mouseCol - placement.Column;

                SelectedPlacement = placement;

                element.Opacity = 0.3;
                if (element is Border b)
                {
                    b.BorderBrush = new SolidColorBrush(Colors.White);
                    b.BorderThickness = new Thickness(3);
                }

                UpdatePropertiesPanel();
            }
            finally
            {
                _isStartingDrag = false;
            }
        }
    }

    private void Common_DragOver(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.Text))
        {
            e.AcceptedOperation = DataPackageOperation.None;
            return;
        }

        e.DragUIOverride.IsContentVisible = false;
        e.AcceptedOperation = (e.AllowedOperations & DataPackageOperation.Move) == DataPackageOperation.Move
            ? DataPackageOperation.Move
            : DataPackageOperation.Copy;

        var pointerPos = e.GetPosition(PreviewGrid);
        var (mouseRow, mouseCol) = GetGridCellFromPoint(pointerPos);

        var targetRow = Math.Max(0, mouseRow - _dragCellOffsetRow);
        var targetCol = Math.Max(0, mouseCol - _dragCellOffsetCol);

        if (_dropPreviewGhost == null)
        {
            _dropPreviewGhost = new DropGhostControl();
            Canvas.SetZIndex(_dropPreviewGhost, 9999);
            PreviewGrid.Children.Add(_dropPreviewGhost);
        }

        Grid.SetRow(_dropPreviewGhost, targetRow);
        Grid.SetColumn(_dropPreviewGhost, targetCol);
        Grid.SetRowSpan(_dropPreviewGhost,
            Math.Min(_draggedRowSpan, LayoutProfile.RowDefinitions.Count - targetRow));
        Grid.SetColumnSpan(_dropPreviewGhost,
            Math.Min(_draggedColSpan, LayoutProfile.ColumnDefinitions.Count - targetCol));

        if (_floatingDragVisual == null)
        {
            _floatingDragVisual = new DragVisualControl(_draggedText, _draggedBrush, _draggedWidth, _draggedHeight)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                IsHitTestVisible = false
            };
            Grid.SetRow(_floatingDragVisual, 0);
            Grid.SetColumn(_floatingDragVisual, 0);
            Grid.SetRowSpan(_floatingDragVisual, Math.Max(1, PreviewGrid.RowDefinitions.Count));
            Grid.SetColumnSpan(_floatingDragVisual, Math.Max(1, PreviewGrid.ColumnDefinitions.Count));
            Canvas.SetZIndex(_floatingDragVisual, 10000);
            PreviewGrid.Children.Add(_floatingDragVisual);
        }

        _floatingDragVisual.Margin = new Thickness(
            pointerPos.X - _dragExactOffsetX - (LayoutProfile?.PaddingLeft ?? 0),
            pointerPos.Y - _dragExactOffsetY - (LayoutProfile?.PaddingTop ?? 0), 0, 0);
    }

    private async void Common_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.Text))
        {
            var draggedTypeStr = await e.DataView.GetTextAsync();
            if (Enum.TryParse(draggedTypeStr, out ComponentType droppedType))
            {
                var pointerPos = e.GetPosition(PreviewGrid);
                var (mouseRow, mouseCol) = GetGridCellFromPoint(pointerPos);

                var targetRow = Math.Max(0, mouseRow - _dragCellOffsetRow);
                var targetCol = Math.Max(0, mouseCol - _dragCellOffsetCol);

                var existingPlacement =
                    LayoutProfile.Placements.FirstOrDefault(p => p.ComponentType == droppedType);
                ComponentPlacement placementToInsert;

                if (existingPlacement != null)
                {
                    LayoutProfile.Placements.Remove(existingPlacement);
                    existingPlacement.Row = targetRow;
                    existingPlacement.Column = targetCol;
                    existingPlacement.RowSpan = Math.Min(existingPlacement.RowSpan,
                        LayoutProfile.RowDefinitions.Count - targetRow);
                    existingPlacement.ColumnSpan = Math.Min(existingPlacement.ColumnSpan,
                        LayoutProfile.ColumnDefinitions.Count - targetCol);
                    placementToInsert = existingPlacement;
                }
                else
                {
                    placementToInsert = new ComponentPlacement
                    {
                        ComponentType = droppedType,
                        Row = targetRow,
                        Column = targetCol,
                        RowSpan = 1,
                        ColumnSpan = 1,
                        HorizontalAlignment = AppHorizontalAlignment.Stretch,
                        VerticalAlignment = AppVerticalAlignment.Stretch
                    };
                }

                LayoutProfile.Placements.Add(placementToInsert);
                SelectedPlacement = placementToInsert;
                UpdateToolbox();
                RequestRender();
            }
        }
    }

    private void Common_DropCompleted(UIElement sender, DropCompletedEventArgs args)
    {
        ClearDragVisuals();
        RequestRender();
    }

    private void ClearDragVisuals()
    {
        if (_dropPreviewGhost != null)
        {
            PreviewGrid.Children.Remove(_dropPreviewGhost);
            _dropPreviewGhost = null;
        }

        if (_floatingDragVisual != null)
        {
            PreviewGrid.Children.Remove(_floatingDragVisual);
            _floatingDragVisual = null;
        }
    }

    private (int row, int col) GetGridCellFromPoint(Point position)
    {
        int row = 0, col = 0;
        double currentX = 0, currentY = 0;
        for (var c = 0; c < PreviewGrid.ColumnDefinitions.Count; c++)
        {
            currentX += PreviewGrid.ColumnDefinitions[c].ActualWidth;
            if (position.X < currentX)
            {
                col = c;
                break;
            }

            currentX += PreviewGrid.ColumnSpacing;
            col = c;
        }

        for (var r = 0; r < PreviewGrid.RowDefinitions.Count; r++)
        {
            currentY += PreviewGrid.RowDefinitions[r].ActualHeight;
            if (position.Y < currentY)
            {
                row = r;
                break;
            }

            currentY += PreviewGrid.RowSpacing;
            row = r;
        }

        return (Math.Max(0, row), Math.Max(0, col));
    }

    private void Gutter_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            FlyoutBase.ShowAttachedFlyout(fe);
            e.Handled = true;
        }
    }

    private void PreviewScrollViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        Focus(FocusState.Programmatic);
        if (_isHandToolActive && e.GetCurrentPoint(PreviewScrollViewer).Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _panStartPos = e.GetCurrentPoint(PreviewScrollViewer).Position;
            _startScrollX = PreviewScrollViewer.HorizontalOffset;
            _startScrollY = PreviewScrollViewer.VerticalOffset;
            PreviewScrollViewer.CapturePointer(e.Pointer);
            UpdateCursor();
            e.Handled = true;
        }
    }

    private void PreviewScrollViewer_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isPanning)
        {
            var currentPos = e.GetCurrentPoint(PreviewScrollViewer).Position;
            PreviewScrollViewer.ChangeView(_startScrollX - (currentPos.X - _panStartPos.X),
                _startScrollY - (currentPos.Y - _panStartPos.Y), null, true);
            e.Handled = true;
        }
    }

    private void PreviewScrollViewer_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            PreviewScrollViewer.ReleasePointerCapture(e.Pointer);
            UpdateCursor();
            e.Handled = true;
        }
    }

    private void WheelInterceptor_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        var shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
        var isCtrlDown = (ctrlState & CoreVirtualKeyStates.Down) ==
                         CoreVirtualKeyStates.Down;
        var isShiftDown = (shiftState & CoreVirtualKeyStates.Down) ==
                          CoreVirtualKeyStates.Down;
        var delta = e.GetCurrentPoint(PreviewScrollViewer).Properties.MouseWheelDelta;

        if (isCtrlDown)
        {
            var newZoom = Math.Clamp(PreviewScrollViewer.ZoomFactor + (delta > 0 ? 0.1f : -0.1f),
                PreviewScrollViewer.MinZoomFactor, PreviewScrollViewer.MaxZoomFactor);
            PreviewScrollViewer.ChangeView(null, null, newZoom);
            e.Handled = true;
        }
        else if (isShiftDown)
        {
            PreviewScrollViewer.ChangeView(PreviewScrollViewer.HorizontalOffset - delta, null, null);
            e.Handled = true;
        }
    }

    private void PreviewScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (PreviewScrollViewer == null) return;
        CurrentZoom = PreviewScrollViewer.ZoomFactor;
        if (ColHeadersItemsControl != null)
            ColHeadersItemsControl.RenderTransform = new TranslateTransform
                { X = -PreviewScrollViewer.HorizontalOffset, Y = 0 };
        if (RowHeadersItemsControl != null)
            RowHeadersItemsControl.RenderTransform = new TranslateTransform
                { X = 0, Y = -PreviewScrollViewer.VerticalOffset };

        UpdateMinimap();
    }

    private void UpdateMinimap()
    {
        if (PreviewGrid.ActualWidth == 0 || PreviewGrid.ActualHeight == 0) return;

        double zoom = PreviewScrollViewer.ZoomFactor;

        var zoomedWidth = PreviewGrid.ActualWidth * zoom;
        var zoomedHeight = PreviewGrid.ActualHeight * zoom;

        var needsMinimap =
            zoomedWidth > PreviewScrollViewer.ViewportWidth + 1.0 ||
            zoomedHeight > PreviewScrollViewer.ViewportHeight + 1.0;

        MinimapContainer.Visibility = needsMinimap ? Visibility.Visible : Visibility.Collapsed;

        if (!needsMinimap) return;

        MinimapCanvas.Width = PreviewGrid.ActualWidth;
        MinimapCanvas.Height = PreviewGrid.ActualHeight;

        var visibleWidth = PreviewScrollViewer.ViewportWidth / zoom;
        var visibleHeight = PreviewScrollViewer.ViewportHeight / zoom;

        var offsetX = PreviewScrollViewer.HorizontalOffset / zoom;
        var offsetY = PreviewScrollViewer.VerticalOffset / zoom;

        MinimapViewportBox.Width = Math.Min(visibleWidth, MinimapCanvas.Width);
        MinimapViewportBox.Height = Math.Min(visibleHeight, MinimapCanvas.Height);

        Canvas.SetLeft(MinimapViewportBox, offsetX);
        Canvas.SetTop(MinimapViewportBox, offsetY);
    }

    private void MinimapCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pt = e.GetCurrentPoint(MinimapCanvas).Position;
        double zoom = PreviewScrollViewer.ZoomFactor;

        var boxLeft = Canvas.GetLeft(MinimapViewportBox);
        var boxTop = Canvas.GetTop(MinimapViewportBox);
        var boxRight = boxLeft + MinimapViewportBox.Width;
        var boxBottom = boxTop + MinimapViewportBox.Height;

        var isInsideBox = pt.X >= boxLeft && pt.X <= boxRight && pt.Y >= boxTop && pt.Y <= boxBottom;

        if (!isInsideBox) return;

        _isDraggingMinimap = true;
        _lastMinimapDragPos = pt;
        MinimapCanvas.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void MinimapCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDraggingMinimap) return;

        var pt = e.GetCurrentPoint(MinimapCanvas).Position;

        var dx = pt.X - _lastMinimapDragPos.X;
        var dy = pt.Y - _lastMinimapDragPos.Y;

        _lastMinimapDragPos = pt;

        double zoom = PreviewScrollViewer.ZoomFactor;

        var targetOffsetX = PreviewScrollViewer.HorizontalOffset + dx * zoom;
        var targetOffsetY = PreviewScrollViewer.VerticalOffset + dy * zoom;

        PreviewScrollViewer.ChangeView(targetOffsetX, targetOffsetY, null, true);
        e.Handled = true;
    }

    private void MinimapCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isDraggingMinimap)
        {
            _isDraggingMinimap = false;
            MinimapCanvas.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
    }

    private void MinimapCanvas_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (_isDraggingMinimap)
        {
            _isDraggingMinimap = false;
            MinimapCanvas.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
    }

    private void FitToScreen()
    {
        if (LyricsWindowStatus == null || PreviewGrid.Width <= 0 || PreviewGrid.Height <= 0) return;
        var zoomX = Math.Max(10, PreviewScrollViewer.ActualWidth - 48) / PreviewGrid.Width;
        var zoomY = Math.Max(10, PreviewScrollViewer.ActualHeight - 48) / PreviewGrid.Height;
        var fitZoom = (float)Math.Min(zoomX, zoomY);
        PreviewScrollViewer.MinZoomFactor = Math.Max(0.1f, fitZoom);
        PreviewScrollViewer.ChangeView(null, null, fitZoom);
    }

    private void UpdateCursor()
    {
        if (_isPanning) ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeAll);
        else if (_isHandToolActive) ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        else if (_isResizing) ProtectedCursor = GetResizeCursor(_resizeDirection);
        else if (!string.IsNullOrEmpty(_hoveredHandle)) ProtectedCursor = GetResizeCursor(_hoveredHandle);
        else if (_isHoveringAddBtn) ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        else ProtectedCursor = null;
    }

    private InputSystemCursor GetResizeCursor(string direction)
    {
        return direction switch
        {
            "Right" => InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast),
            "Bottom" => InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth),
            _ => InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast)
        };
    }

    private void UpdatePreviewAspectRatio()
    {
        if (LyricsWindowStatus == null || LyricsWindowStatus.WindowBounds.Width <= 0 ||
            LyricsWindowStatus.WindowBounds.Height <= 0) return;
        PreviewGrid.Width = LyricsWindowStatus.WindowBounds.Width;
        PreviewGrid.Height = LyricsWindowStatus.WindowBounds.Height;
    }

    private void SetHandToolState(bool isActive)
    {
        _isHandToolActive = isActive;
        HandToolToggle.IsChecked = isActive;
        PreviewGrid.IsHitTestVisible = !_isHandToolActive;
        if (!_isHandToolActive && _isPanning)
        {
            _isPanning = false;
            PreviewScrollViewer.ReleasePointerCaptures();
        }

        UpdateCursor();
    }

    private void HandToolToggle_Click(object sender, RoutedEventArgs e)
    {
        SetHandToolState(HandToolToggle.IsChecked ?? false);
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        PreviewScrollViewer.ChangeView(null, null,
            Math.Min(PreviewScrollViewer.ZoomFactor + 0.2f, PreviewScrollViewer.MaxZoomFactor));
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        PreviewScrollViewer.ChangeView(null, null,
            Math.Max(PreviewScrollViewer.ZoomFactor - 0.2f, PreviewScrollViewer.MinZoomFactor));
    }

    private void ZoomReset_Click(object sender, RoutedEventArgs e)
    {
        FitToScreen();
    }

    private void PreviewContainer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdatePreviewAspectRatio();
    }

    private void SizeEditorGrid_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Grid grid && grid.DataContext is HeaderItemModel model)
        {
            var nb = grid.FindName("SizeInputBox") as NumberBox;
            var cb = grid.FindName("SizeTypeBox") as ComboBox;
            if (nb == null || cb == null) return;

            var def = model.Definition ?? "1*";

            if (def.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            {
                cb.SelectedIndex = 0; // Auto
                nb.Value = double.NaN; // 清空数值
                nb.IsEnabled = false;
            }
            else if (def.EndsWith("*"))
            {
                cb.SelectedIndex = 1; // *
                var numStr = def.TrimEnd('*');
                nb.Value = double.TryParse(numStr, out var val) ? val : 1;
                nb.IsEnabled = true;
            }
            else
            {
                cb.SelectedIndex = 2; // px
                var numStr = def.Replace("px", "", StringComparison.OrdinalIgnoreCase);
                nb.Value = double.TryParse(numStr, out var val) ? val : 100; // 默认 100px
                nb.IsEnabled = true;
            }
        }
    }

    private void SizeTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.Parent is Grid grid)
        {
            var nb = grid.FindName("SizeInputBox") as NumberBox;
            if (nb != null && cb.SelectedItem is ComboBoxItem item)
            {
                nb.IsEnabled = item.Tag?.ToString() != "Auto";

                if (nb.IsEnabled && double.IsNaN(nb.Value)) nb.Value = 1;
            }
        }
    }

    private void ApplySizeFlyout_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is HeaderItemModel model && btn.Parent is StackPanel panel)
        {
            var grid = panel.Children.OfType<Grid>().FirstOrDefault(g => g.Name == "SizeEditorGrid");
            var nb = grid?.FindName("SizeInputBox") as NumberBox;
            var cb = grid?.FindName("SizeTypeBox") as ComboBox;

            var type = (cb?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "*";

            var numStr = nb == null || double.IsNaN(nb.Value) ? "1" : nb.Value.ToString();

            var newDef = type switch
            {
                "Auto" => "Auto",
                "*" => $"{numStr}*",
                "px" => $"{numStr}px",
                _ => "1*"
            };

            if (RowHeaderItems.Contains(model))
                ApplyRowDefinition(model.Index, newDef);
            else if (ColumnHeaderItems.Contains(model))
                ApplyColDefinition(model.Index, newDef);

            var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(btn.XamlRoot);
            if (popups.Any()) popups.Last().IsOpen = false;
        }
    }

    private void ApplyRowDefinition(int index, string newDef)
    {
        if (LayoutProfile == null || index < 0 || index >= LayoutProfile.RowDefinitions.Count) return;

        if (LayoutProfile.RowDefinitions[index] != newDef)
        {
            LayoutProfile.RowDefinitions[index] = newDef;
            UpdateHeaders();
            RequestRender();
        }
    }

    private void ApplyColDefinition(int index, string newDef)
    {
        if (LayoutProfile == null || index < 0 || index >= LayoutProfile.ColumnDefinitions.Count) return;

        if (LayoutProfile.ColumnDefinitions[index] != newDef)
        {
            LayoutProfile.ColumnDefinitions[index] = newDef;
            UpdateHeaders();
            RequestRender();
        }
    }

    private void UserControl_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (IsInputControlFocused()) return;

        var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        var isCtrlDown = (ctrlState & CoreVirtualKeyStates.Down) ==
                         CoreVirtualKeyStates.Down;

        var shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
        var isShiftDown = (shiftState & CoreVirtualKeyStates.Down) ==
                          CoreVirtualKeyStates.Down;

        if ((int)e.OriginalKey == 191 && isCtrlDown)
        {
            HelpButton.Flyout?.ShowAt(HelpButton);
            e.Handled = true;
            return;
        }

        if (isCtrlDown && !IsInputControlFocused())
        {
            if (e.Key == VirtualKey.Z)
            {
                if (isShiftDown)
                    PerformRedo();
                else
                    PerformUndo();

                e.Handled = true;
                return;
            }

            if (e.Key == VirtualKey.Y)
            {
                PerformRedo();
                e.Handled = true;
                return;
            }
        }

        switch (e.Key)
        {
            case VirtualKey.Space:
                if (!e.KeyStatus.WasKeyDown)
                {
                    _isSpacePanning = true;
                    _wasHandToolActiveBeforeSpace = _isHandToolActive;
                    SetHandToolState(true);
                }

                e.Handled = true;
                break;
            case VirtualKey.Escape:
                SelectedPlacement = null;
                e.Handled = true;
                break;
            case VirtualKey.Delete:
            case VirtualKey.Back:
                if (HasSelection && RemoveSelectedCommand.CanExecute(null))
                {
                    RemoveSelectedCommand.Execute(null);
                    e.Handled = true;
                }

                break;
            case VirtualKey.Left:
            case VirtualKey.Right:
            case VirtualKey.Up:
            case VirtualKey.Down:
            case VirtualKey.W:
            case VirtualKey.A:
            case VirtualKey.S:
            case VirtualKey.D:
                if (SelectedPlacement != null)
                {
                    HandleDirectionalKeys(e.Key, isShiftDown);
                    e.Handled = true;
                }

                break;
            case VirtualKey.F1:
                HelpButton.Flyout?.ShowAt(HelpButton);
                e.Handled = true;
                break;
            case VirtualKey.Tab:
                if (LayoutProfile?.Placements != null && LayoutProfile.Placements.Count > 0)
                {
                    CycleSelection(isShiftDown);
                    e.Handled = true;
                }

                break;
            case VirtualKey.F:
                if (!IsInputControlFocused())
                {
                    FocusOnSelectedComponent();
                    e.Handled = true;
                }

                break;
        }
    }

    private void UserControl_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Space && _isSpacePanning)
        {
            _isSpacePanning = false;
            SetHandToolState(_wasHandToolActiveBeforeSpace);
            e.Handled = true;
        }
    }

    private void HandleDirectionalKeys(VirtualKey key, bool isShiftDown)
    {
        var p = SelectedPlacement;
        if (p == null || LayoutProfile == null) return;

        var maxRow = LayoutProfile.RowDefinitions.Count - 1;
        var maxCol = LayoutProfile.ColumnDefinitions.Count - 1;

        var isLeft = key == VirtualKey.Left || key == VirtualKey.A;
        var isRight = key == VirtualKey.Right || key == VirtualKey.D;
        var isUp = key == VirtualKey.Up || key == VirtualKey.W;
        var isDown = key == VirtualKey.Down || key == VirtualKey.S;

        if (isShiftDown)
        {
            if (isLeft) p.ColumnSpan = Math.Max(1, p.ColumnSpan - 1);
            else if (isRight) p.ColumnSpan = Math.Min(maxCol - p.Column + 1, p.ColumnSpan + 1);
            else if (isUp) p.RowSpan = Math.Max(1, p.RowSpan - 1);
            else if (isDown) p.RowSpan = Math.Min(maxRow - p.Row + 1, p.RowSpan + 1);
        }
        else
        {
            if (isLeft) p.Column = Math.Max(0, p.Column - 1);
            else if (isRight) p.Column = Math.Min(maxCol - p.ColumnSpan + 1, p.Column + 1);
            else if (isUp) p.Row = Math.Max(0, p.Row - 1);
            else if (isDown) p.Row = Math.Min(maxRow - p.RowSpan + 1, p.Row + 1);
        }

        RequestRender();
    }

    private void CycleSelection(bool isReverse)
    {
        var placements = LayoutProfile.Placements;
        if (placements.Count == 0) return;

        if (SelectedPlacement == null)
        {
            SelectedPlacement = isReverse ? placements.Last() : placements.First();
            return;
        }

        var currentIndex = placements.IndexOf(SelectedPlacement);
        int nextIndex;

        if (isReverse)
        {
            nextIndex = currentIndex - 1;
            if (nextIndex < 0) nextIndex = placements.Count - 1;
        }
        else
        {
            nextIndex = currentIndex + 1;
            if (nextIndex >= placements.Count) nextIndex = 0;
        }

        SelectedPlacement = placements[nextIndex];
    }

    private void FocusOnSelectedComponent()
    {
        if (SelectedPlacement == null) return;

        CanvasItemControl targetVisual = null;
        foreach (var child in PreviewGrid.Children)
            if (child is CanvasItemControl itemControl && itemControl.Placement == SelectedPlacement)
            {
                targetVisual = itemControl;
                break;
            }

        if (targetVisual == null) return;

        targetVisual.UpdateLayout();

        var transform = targetVisual.TransformToVisual(PreviewGrid);
        var bounds = transform.TransformBounds(new Rect(0, 0, targetVisual.ActualWidth, targetVisual.ActualHeight));

        double zoom = PreviewScrollViewer.ZoomFactor;

        var targetCenterX = bounds.Left + bounds.Width / 2;
        var targetCenterY = bounds.Top + bounds.Height / 2;

        var offsetX = targetCenterX * zoom - PreviewScrollViewer.ViewportWidth / 2;
        var offsetY = targetCenterY * zoom - PreviewScrollViewer.ViewportHeight / 2;

        PreviewScrollViewer.ChangeView(offsetX, offsetY, null, false);
    }

    private bool IsInputControlFocused()
    {
        var focusedElement = FocusManager.GetFocusedElement(XamlRoot);
        return focusedElement is TextBox || focusedElement is NumberBox;
    }

    private void RecordState()
    {
        if (LayoutProfile != null) _historyManager.SaveSnapshot(LayoutProfile);
    }

    private void PerformUndo()
    {
        if (!_historyManager.CanUndo) return;

        var previousState = _historyManager.Undo();
        RestoreState(previousState);
    }

    private void PerformRedo()
    {
        if (!_historyManager.CanRedo) return;

        var nextState = _historyManager.Redo();
        RestoreState(nextState);
    }

    private void RestoreState(LayoutProfile? restoredProfile)
    {
        if (restoredProfile == null) return;

        _isRestoringHistory = true;

        SelectedPlacement = null;

        LayoutProfile.ApplyFrom(restoredProfile);

        UpdateToolbox();
        UpdateHeaders();
        RequestRender();

        _isRestoringHistory = false;
    }
}