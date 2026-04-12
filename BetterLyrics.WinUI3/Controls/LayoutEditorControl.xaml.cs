using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class LayoutEditorControl : UserControl, IRecipient<PropertyChangedMessage<Rect>>
    {
        private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        public LayoutEditorControlViewModel ViewModel => (LayoutEditorControlViewModel)DataContext;

        private bool _isUpdatingUI = false;
        private bool _isHandToolActive = false;
        private bool _isPanning = false;
        private Point _panStartPos;
        private double _startScrollX;
        private double _startScrollY;

        private int _draggedRowSpan = 1;
        private int _draggedColSpan = 1;
        private Brush? _draggedBrush = null;
        private FrameworkElement? _dropPreviewGhost = null;
        private Thickness _draggedMargin;
        private HorizontalAlignment _draggedHAlign;
        private VerticalAlignment _draggedVAlign;
        private string _draggedText = "";
        private int _dragCellOffsetRow = 0;
        private int _dragCellOffsetCol = 0;

        private double _draggedWidth = 100;
        private double _draggedHeight = 40;
        private double _dragExactOffsetX = 0;
        private double _dragExactOffsetY = 0;
        private FrameworkElement? _floatingDragVisual = null;

        private bool _isStartingDrag = false;

        private bool _isResizing = false;
        private string _resizeDirection = "";
        private ComponentPlacement? _resizeTarget = null;
        private string _hoveredHandle = "";
        private bool _isHoveringAddBtn = false;

        private bool _isSpacePanning = false;
        private bool _wasHandToolActiveBeforeSpace = false;

        public static readonly DependencyProperty LyricsWindowStatusProperty =
            DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus), typeof(LayoutEditorControl), new PropertyMetadata(null, OnDependencyPropertyChanged));

        public LyricsWindowStatus LyricsWindowStatus
        {
            get => (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty);
            set => SetValue(LyricsWindowStatusProperty, value);
        }

        public LayoutEditorControl()
        {
            this.InitializeComponent();            
            DataContext = Ioc.Default.GetRequiredService<LayoutEditorControlViewModel>();
            ViewModel.LayoutRequiresRender += () =>
            {
                if (_isStartingDrag) return;

                UpdateLayerList();
                UpdatePropertiesPanel();
                RenderPreviewGrid();
            };

            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayoutEditorControl control && e.NewValue is LyricsWindowStatus status)
            {
                control.ViewModel.WindowStatus = status;
                control.InitializeData();
            }
        }

        private void InitializeData()
        {
            if (LyricsWindowStatus?.LayoutProfile == null) return;
            ViewModel.RequestRender();
        }

        private void UpdateLayerList()
        {
            if (LyricsWindowStatus?.LayoutProfile == null) return;
            LayerListView.ItemsSource = null;
            LayerListView.ItemsSource = LyricsWindowStatus.LayoutProfile.Placements;
            LayerListView.SelectedItem = ViewModel.SelectedPlacement;
        }

        private void LayerListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LayerListView.SelectedItem is ComponentPlacement placement)
            {
                ViewModel.SelectedPlacement = placement;
            }
        }

        private void UpdatePropertiesPanel()
        {
            _isUpdatingUI = true;

            if (ViewModel.SelectedPlacement != null)
            {
                SelectedComponentLabel.Text = ViewModel.SelectedPlacement.DisplayName;
            }
            else
            {
                SelectedComponentLabel.Text = _localizationService.GetLocalizedString("LayoutEditorControlNoSelection");
            }

            _isUpdatingUI = false;
        }

        private void ToolboxItem_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            if (sender is FrameworkElement element && element.DataContext is ToolboxItem item)
            {
                args.Data.SetText(item.ComponentType.ToString());
                args.Data.RequestedOperation = DataPackageOperation.Copy;

                _draggedRowSpan = 1; _draggedColSpan = 1;
                _draggedBrush = (element as Border)?.Background;
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
                    _draggedBrush = (element as Border)?.Background;
                    _draggedText = placement.DisplayName;
                    _draggedMargin = new Thickness(placement.MarginLeft, placement.MarginTop, placement.MarginRight, placement.MarginBottom);
                    _draggedHAlign = placement.HorizontalAlignment;
                    _draggedVAlign = placement.VerticalAlignment;

                    var (mouseRow, mouseCol) = GetGridCellFromPoint(pointerPos);
                    _dragCellOffsetRow = mouseRow - placement.Row;
                    _dragCellOffsetCol = mouseCol - placement.Column;

                    ViewModel.SelectedPlacement = placement;

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

        private void RenderPreviewGrid()
        {
            if (LyricsWindowStatus?.LayoutProfile == null) return;

            ClearDragVisuals();

            PreviewGrid.Children.Clear();
            PreviewGrid.RowDefinitions.Clear();
            PreviewGrid.ColumnDefinitions.Clear();

            PreviewGrid.RowSpacing = LyricsWindowStatus.LayoutProfile.RowSpacing;
            PreviewGrid.ColumnSpacing = LyricsWindowStatus.LayoutProfile.ColumnSpacing;

            double pLeft = LyricsWindowStatus.LayoutProfile.PaddingLeft;
            double pTop = LyricsWindowStatus.LayoutProfile.PaddingTop;
            double pRight = LyricsWindowStatus.LayoutProfile.PaddingRight;
            double pBottom = LyricsWindowStatus.LayoutProfile.PaddingBottom;

            PreviewGrid.Padding = new Thickness(pLeft, pTop, pRight, pBottom);

            foreach (var rowDef in LyricsWindowStatus.LayoutProfile.RowDefinitions)
            {
                var gl = rowDef.Equals("Auto", StringComparison.OrdinalIgnoreCase) ? new GridLength(1, GridUnitType.Auto) : new GridLength(1, GridUnitType.Star);
                PreviewGrid.RowDefinitions.Add(new RowDefinition { Height = gl });
            }

            foreach (var colDef in LyricsWindowStatus.LayoutProfile.ColumnDefinitions)
            {
                var gl = colDef.Equals("Auto", StringComparison.OrdinalIgnoreCase) ? new GridLength(1, GridUnitType.Auto) : new GridLength(1, GridUnitType.Star);
                PreviewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = gl });
            }

            for (int r = 0; r < LyricsWindowStatus.LayoutProfile.RowDefinitions.Count; r++)
            {
                for (int c = 0; c < LyricsWindowStatus.LayoutProfile.ColumnDefinitions.Count; c++)
                {
                    var cellSlot = new Border
                    {
                        BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush", Colors.Gray),
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
                        ViewModel.SelectedPlacement = null;
                        this.Focus(FocusState.Programmatic);
                    };

                    PreviewGrid.Children.Add(cellSlot);
                }
            }

            foreach (var placement in LyricsWindowStatus.LayoutProfile.Placements)
            {
                var componentBlock = CreateComponentVisual(placement);
                Grid.SetRow(componentBlock, placement.Row);
                Grid.SetColumn(componentBlock, placement.Column);
                Grid.SetRowSpan(componentBlock, placement.RowSpan);
                Grid.SetColumnSpan(componentBlock, placement.ColumnSpan);
                PreviewGrid.Children.Add(componentBlock);
            }
        }

        private FrameworkElement CreateComponentVisual(ComponentPlacement placement)
        {
            bool isSelected = ViewModel.SelectedPlacement == placement;

            var control = new CanvasItemControl(placement, isSelected)
            {
                Margin = new Thickness(placement.MarginLeft, placement.MarginTop, placement.MarginRight, placement.MarginBottom),
                HorizontalAlignment = placement.HorizontalAlignment,
                VerticalAlignment = placement.VerticalAlignment,
                MinWidth = 4,
                MinHeight = 4
            };

            Canvas.SetZIndex(control, isSelected ? 999 : 0);

            control.MainBorder.Tapped += (s, e) =>
            {
                ViewModel.SelectedPlacement = placement;
                this.Focus(FocusState.Programmatic);
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
                    handle.PointerEntered += (s, e) => { _hoveredHandle = direction; UpdateCursor(); };
                    handle.PointerExited += (s, e) => { if (_hoveredHandle == direction) _hoveredHandle = ""; UpdateCursor(); };
                }

                WireUpHandle(control.RightHandle, "Right");
                WireUpHandle(control.BottomHandle, "Bottom");
                WireUpHandle(control.CornerHandle, "Corner");
            }

            return control;
        }

        private void Handle_PointerPressed(object sender, PointerRoutedEventArgs e, string direction, ComponentPlacement placement)
        {
            var handle = sender as UIElement;
            handle?.CapturePointer(e.Pointer);

            _isResizing = true;
            _resizeDirection = direction;
            _resizeTarget = placement;

            if (_dropPreviewGhost == null)
            {
                var ghostContainer = new Grid { IsHitTestVisible = false };
                var cellHighlight = new Microsoft.UI.Xaml.Shapes.Rectangle
                {
                    Fill = new SolidColorBrush(Colors.DodgerBlue) { Opacity = 0.1 },
                    Stroke = new SolidColorBrush(Colors.DodgerBlue) { Opacity = 0.6 },
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                };

                var componentHighlight = new Border
                {
                    Background = new SolidColorBrush(Colors.DodgerBlue) { Opacity = 0.4 },
                    BorderBrush = new SolidColorBrush(Colors.DodgerBlue),
                    BorderThickness = new Thickness(3),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(placement.MarginLeft, placement.MarginTop, placement.MarginRight, placement.MarginBottom),
                    HorizontalAlignment = placement.HorizontalAlignment,
                    VerticalAlignment = placement.VerticalAlignment,
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

            int newRowSpan = _resizeTarget.RowSpan;
            int newColSpan = _resizeTarget.ColumnSpan;
            int maxRowSpan = LyricsWindowStatus.LayoutProfile.RowDefinitions.Count - _resizeTarget.Row;
            int maxColSpan = LyricsWindowStatus.LayoutProfile.ColumnDefinitions.Count - _resizeTarget.Column;

            if (_resizeDirection == "Right" || _resizeDirection == "Corner") newColSpan = Math.Clamp(hoverCol - _resizeTarget.Column + 1, 1, maxColSpan);
            if (_resizeDirection == "Bottom" || _resizeDirection == "Corner") newRowSpan = Math.Clamp(hoverRow - _resizeTarget.Row + 1, 1, maxRowSpan);

            Grid.SetRowSpan(_dropPreviewGhost, newRowSpan);
            Grid.SetColumnSpan(_dropPreviewGhost, newColSpan);
            e.Handled = true;
        }

        private void Handle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isResizing) return;
            var handle = sender as UIElement;
            handle?.ReleasePointerCapture(e.Pointer);

            _resizeTarget?.RowSpan = Grid.GetRowSpan(_dropPreviewGhost);
            _resizeTarget?.ColumnSpan = Grid.GetColumnSpan(_dropPreviewGhost);

            _isResizing = false; _resizeTarget = null;
            if (_dropPreviewGhost != null) { PreviewGrid.Children.Remove(_dropPreviewGhost); _dropPreviewGhost = null; }

            ViewModel.RequestRender();
            UpdateCursor();
            e.Handled = true;
        }

        private void Common_DragOver(object sender, DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.Text)) { e.AcceptedOperation = DataPackageOperation.None; return; }

            e.DragUIOverride.IsContentVisible = false;
            e.AcceptedOperation = (e.AllowedOperations & DataPackageOperation.Move) == DataPackageOperation.Move
                                  ? DataPackageOperation.Move : DataPackageOperation.Copy;

            var pointerPos = e.GetPosition(PreviewGrid);
            var (mouseRow, mouseCol) = GetGridCellFromPoint(pointerPos);

            int targetRow = Math.Max(0, mouseRow - _dragCellOffsetRow);
            int targetCol = Math.Max(0, mouseCol - _dragCellOffsetCol);

            if (_dropPreviewGhost == null)
            {
                _dropPreviewGhost = new DropGhostControl();
                Canvas.SetZIndex(_dropPreviewGhost, 9999);
                PreviewGrid.Children.Add(_dropPreviewGhost);
            }
            Grid.SetRow(_dropPreviewGhost, targetRow);
            Grid.SetColumn(_dropPreviewGhost, targetCol);
            Grid.SetRowSpan(_dropPreviewGhost, Math.Min(_draggedRowSpan, LyricsWindowStatus.LayoutProfile.RowDefinitions.Count - targetRow));
            Grid.SetColumnSpan(_dropPreviewGhost, Math.Min(_draggedColSpan, LyricsWindowStatus.LayoutProfile.ColumnDefinitions.Count - targetCol));

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

            double pLeft = LyricsWindowStatus?.LayoutProfile?.PaddingLeft ?? 0;
            double pTop = LyricsWindowStatus?.LayoutProfile?.PaddingTop ?? 0;

            _floatingDragVisual.Margin = new Thickness(
                pointerPos.X - _dragExactOffsetX - pLeft,
                pointerPos.Y - _dragExactOffsetY - pTop,
                0, 0);
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

                    int targetRow = Math.Max(0, mouseRow - _dragCellOffsetRow);
                    int targetCol = Math.Max(0, mouseCol - _dragCellOffsetCol);

                    var existingPlacement = LyricsWindowStatus.LayoutProfile.Placements.FirstOrDefault(p => p.ComponentType == droppedType);
                    ComponentPlacement placementToInsert;

                    if (existingPlacement != null)
                    {
                        LyricsWindowStatus.LayoutProfile.Placements.Remove(existingPlacement);
                        existingPlacement.Row = targetRow; existingPlacement.Column = targetCol;
                        int maxRowSpan = LyricsWindowStatus.LayoutProfile.RowDefinitions.Count - targetRow;
                        int maxColSpan = LyricsWindowStatus.LayoutProfile.ColumnDefinitions.Count - targetCol;
                        existingPlacement.RowSpan = Math.Min(existingPlacement.RowSpan, maxRowSpan);
                        existingPlacement.ColumnSpan = Math.Min(existingPlacement.ColumnSpan, maxColSpan);
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
                            MarginLeft = 0,
                            MarginTop = 0,
                            MarginRight = 0,
                            MarginBottom = 0,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch
                        };
                    }

                    LyricsWindowStatus.LayoutProfile.Placements.Add(placementToInsert);
                    ViewModel.SelectedPlacement = placementToInsert;
                    ViewModel.UpdateToolbox();
                    ViewModel.RequestRender();
                }
            }
        }

        private void Common_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
            ClearDragVisuals();
            ViewModel.RequestRender();
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
            int row = 0; int col = 0;
            double currentX = 0; double currentY = 0;
            for (int c = 0; c < PreviewGrid.ColumnDefinitions.Count; c++)
            {
                currentX += PreviewGrid.ColumnDefinitions[c].ActualWidth;
                if (position.X < currentX)
                {
                    col = c;
                    break;
                }
                currentX += PreviewGrid.ColumnSpacing; col = c;
            }
            for (int r = 0; r < PreviewGrid.RowDefinitions.Count; r++)
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

        private void Gutter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid)
            {
                ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            }
        }

        private void Gutter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid)
            {
                ProtectedCursor = null;
            }
        }

        private void Gutter_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.ShowAttachedFlyout(fe);
                e.Handled = true;
            }
        }

        private void PreviewScrollViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.Focus(FocusState.Programmatic);

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
                PreviewScrollViewer.ChangeView(_startScrollX - (currentPos.X - _panStartPos.X), _startScrollY - (currentPos.Y - _panStartPos.Y), null, true);
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

            bool isCtrlDown = (ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
            bool isShiftDown = (shiftState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

            var delta = e.GetCurrentPoint(PreviewScrollViewer).Properties.MouseWheelDelta;

            if (isCtrlDown)
            {
                float newZoom = Math.Clamp(
                    PreviewScrollViewer.ZoomFactor + (delta > 0 ? 0.1f : -0.1f),
                    PreviewScrollViewer.MinZoomFactor,
                    PreviewScrollViewer.MaxZoomFactor);
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
            if (PreviewScrollViewer == null || ViewModel == null) return;

            ViewModel.CurrentZoom = PreviewScrollViewer.ZoomFactor;

            if (ColHeadersItemsControl != null)
            {
                ColHeadersItemsControl.RenderTransform = new TranslateTransform { X = -PreviewScrollViewer.HorizontalOffset, Y = 0 };
            }

            if (RowHeadersItemsControl != null)
            {
                RowHeadersItemsControl.RenderTransform = new TranslateTransform { X = 0, Y = -PreviewScrollViewer.VerticalOffset };
            }
        }

        private void FitToScreen()
        {
            if (LyricsWindowStatus == null || PreviewGrid.Width <= 0 || PreviewGrid.Height <= 0) return;

            double containerWidth = Math.Max(10, PreviewScrollViewer.ActualWidth - 48);
            double containerHeight = Math.Max(10, PreviewScrollViewer.ActualHeight - 48);

            double zoomX = containerWidth / PreviewGrid.Width;
            double zoomY = containerHeight / PreviewGrid.Height;
            float fitZoom = (float)Math.Min(zoomX, zoomY);

            PreviewScrollViewer.MinZoomFactor = Math.Min(0.1f, fitZoom);

            PreviewScrollViewer.ChangeView(null, null, fitZoom);
        }

        private void UpdateCursor()
        {
            if (_isPanning) this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeAll);
            else if (_isHandToolActive) this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            else if (_isResizing) this.ProtectedCursor = GetResizeCursor(_resizeDirection);
            else if (!string.IsNullOrEmpty(_hoveredHandle)) this.ProtectedCursor = GetResizeCursor(_hoveredHandle);
            else if (_isHoveringAddBtn) this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            else this.ProtectedCursor = null;
        }

        private InputSystemCursor GetResizeCursor(string direction) => direction switch
        {
            "Right" => InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast),
            "Bottom" => InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth),
            _ => InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast)
        };

        private void UpdatePreviewAspectRatio()
        {
            if (LyricsWindowStatus == null || LyricsWindowStatus.WindowBounds.Width <= 0 || LyricsWindowStatus.WindowBounds.Height <= 0) return;

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

        private void ZoomIn_Click(object sender, RoutedEventArgs e) => PreviewScrollViewer.ChangeView(null, null, Math.Min(PreviewScrollViewer.ZoomFactor + 0.2f, PreviewScrollViewer.MaxZoomFactor));

        private void ZoomOut_Click(object sender, RoutedEventArgs e) => PreviewScrollViewer.ChangeView(null, null, Math.Max(PreviewScrollViewer.ZoomFactor - 0.2f, PreviewScrollViewer.MinZoomFactor));

        private void ZoomReset_Click(object sender, RoutedEventArgs e)
        {
            FitToScreen();
        }

        private void PreviewContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePreviewAspectRatio();
            FitToScreen();
        }

        // TODO: 目前这个 LayoutUpdated 的实现可能会有性能问题，后续可以考虑改成更精确的事件触发机制
        private void PreviewGrid_LayoutUpdated(object sender, object e)
        {
            if (PreviewGrid == null || ViewModel == null) return;
            bool needsUpdate = false;

            for (int c = 0; c < Math.Min(PreviewGrid.ColumnDefinitions.Count, ViewModel.ColumnHeaderItems.Count); c++)
            {
                double actualWidth = PreviewGrid.ColumnDefinitions[c].ActualWidth;
                if (double.IsNormal(actualWidth) && Math.Abs(ViewModel.ColumnHeaderItems[c].BaseSize - actualWidth) > 0.1)
                {
                    ViewModel.ColumnHeaderItems[c].BaseSize = actualWidth;
                    needsUpdate = true;
                }
            }

            for (int r = 0; r < Math.Min(PreviewGrid.RowDefinitions.Count, ViewModel.RowHeaderItems.Count); r++)
            {
                double actualHeight = PreviewGrid.RowDefinitions[r].ActualHeight;
                if (double.IsNormal(actualHeight) && Math.Abs(ViewModel.RowHeaderItems[r].BaseSize - actualHeight) > 0.1)
                {
                    ViewModel.RowHeaderItems[r].BaseSize = actualHeight;
                    needsUpdate = true;
                }
            }

            if (needsUpdate) ViewModel.UpdateScaledLayout();
        }

        private void UserControl_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (IsInputControlFocused()) return;

            var shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
            bool isShiftDown = (shiftState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

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
                    ViewModel.SelectedPlacement = null;
                    UpdatePropertiesPanel();
                    e.Handled = true;
                    break;

                case VirtualKey.Delete:
                case VirtualKey.Back:
                    if (ViewModel.HasSelection && ViewModel.RemoveSelectedCommand.CanExecute(null))
                    {
                        ViewModel.RemoveSelectedCommand.Execute(null);
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
                    if (ViewModel.SelectedPlacement != null)
                    {
                        HandleDirectionalKeys(e.Key, isShiftDown);
                        e.Handled = true;
                    }
                    break;

                case VirtualKey.F1:
                    HelpButton.Flyout?.ShowAt(HelpButton);
                    e.Handled = true;
                    break;

                // 捕获 / 或 ? 键
                case (VirtualKey)191:
                    var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
                    if ((ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                    {
                        HelpButton.Flyout?.ShowAt(HelpButton);
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
            var p = ViewModel.SelectedPlacement;
            if (p == null || LyricsWindowStatus?.LayoutProfile == null) return;

            int maxRow = LyricsWindowStatus.LayoutProfile.RowDefinitions.Count - 1;
            int maxCol = LyricsWindowStatus.LayoutProfile.ColumnDefinitions.Count - 1;

            bool isLeft = key == VirtualKey.Left || key == VirtualKey.A;
            bool isRight = key == VirtualKey.Right || key == VirtualKey.D;
            bool isUp = key == VirtualKey.Up || key == VirtualKey.W;
            bool isDown = key == VirtualKey.Down || key == VirtualKey.S;

            if (isShiftDown)
            {
                if (isLeft)
                    p.ColumnSpan = Math.Max(1, p.ColumnSpan - 1);
                else if (isRight)
                    p.ColumnSpan = Math.Min(maxCol - p.Column + 1, p.ColumnSpan + 1);
                else if (isUp)
                    p.RowSpan = Math.Max(1, p.RowSpan - 1);
                else if (isDown)
                    p.RowSpan = Math.Min(maxRow - p.Row + 1, p.RowSpan + 1);
            }
            else
            {
                if (isLeft)
                    p.Column = Math.Max(0, p.Column - 1);
                else if (isRight)
                    p.Column = Math.Min(maxCol - p.ColumnSpan + 1, p.Column + 1);
                else if (isUp)
                    p.Row = Math.Max(0, p.Row - 1);
                else if (isDown)
                    p.Row = Math.Min(maxRow - p.RowSpan + 1, p.Row + 1);
            }

            ViewModel.RequestRender();
        }

        private bool IsInputControlFocused()
        {
            var focusedElement = FocusManager.GetFocusedElement(this.XamlRoot);
            return focusedElement is TextBox || focusedElement is NumberBox;
        }

        private Brush GetThemeBrush(string resourceName, Color fallbackColor)
        {
            if (Resources.TryGetValue(resourceName, out object value) && value is Brush brush)
            {
                return brush;
            }
            return new SolidColorBrush(fallbackColor);
        }

        public void Receive(PropertyChangedMessage<Rect> message)
        {
            if (message.Sender == LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.WindowBounds))
                {
                    UpdatePreviewAspectRatio();
                }
            }
        }
    }
}