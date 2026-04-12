using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LayoutEditorControlViewModel : BaseViewModel
    {
        [ObservableProperty] public partial LyricsWindowStatus WindowStatus { get; set; }

        [ObservableProperty] public partial ObservableCollection<ToolboxItem> AvailableToolboxItems { get; set; } = new();

        [ObservableProperty][NotifyPropertyChangedFor(nameof(HasSelection))] public partial ComponentPlacement? SelectedPlacement { get; set; }

        [ObservableProperty] public partial double CurrentZoom { get; set; } = 1.0;

        [ObservableProperty] public partial double ScaledRowSpacing { get; set; }
        [ObservableProperty] public partial double ScaledColSpacing { get; set; }
        [ObservableProperty] public partial Thickness ScaledColPadding { get; set; }
        [ObservableProperty] public partial Thickness ScaledRowPadding { get; set; }

        public ObservableCollection<HeaderItemModel> ColumnHeaderItems { get; } = new();
        public ObservableCollection<HeaderItemModel> RowHeaderItems { get; } = new();

        public int MaxRowIndex => Math.Max(0, (WindowStatus?.LayoutProfile?.RowDefinitions?.Count ?? 1) - 1);

        public int MaxColIndex => Math.Max(0, (WindowStatus?.LayoutProfile?.ColumnDefinitions?.Count ?? 1) - 1);

        public int MaxRowSpan => Math.Max(1, (WindowStatus?.LayoutProfile?.RowDefinitions?.Count ?? 1) - (SelectedPlacement?.Row ?? 0));

        public int MaxColSpan => Math.Max(1, (WindowStatus?.LayoutProfile?.ColumnDefinitions?.Count ?? 1) - (SelectedPlacement?.Column ?? 0));

        public bool HasSelection => SelectedPlacement != null;

        public event Action? LayoutRequiresRender;

        public void RequestRender() => LayoutRequiresRender?.Invoke();

        partial void OnWindowStatusChanged(LyricsWindowStatus value)
        {
            UpdateToolbox();
            RequestRender();
        }

        partial void OnSelectedPlacementChanged(ComponentPlacement? value)
        {
            RequestRender();
        }

        partial void OnCurrentZoomChanged(double value) => UpdateScaledLayout();

        public void UpdateScaledLayout()
        {
            if (WindowStatus?.LayoutProfile == null) return;

            ScaledRowSpacing = WindowStatus.LayoutProfile.RowSpacing * CurrentZoom;
            ScaledColSpacing = WindowStatus.LayoutProfile.ColumnSpacing * CurrentZoom;

            ScaledColPadding = new Thickness(WindowStatus.LayoutProfile.PaddingLeft * CurrentZoom, 0, WindowStatus.LayoutProfile.PaddingRight * CurrentZoom, 0);
            ScaledRowPadding = new Thickness(0, WindowStatus.LayoutProfile.PaddingTop * CurrentZoom, 0, WindowStatus.LayoutProfile.PaddingBottom * CurrentZoom);

            foreach (var item in ColumnHeaderItems) item.ItemSize = item.BaseSize * CurrentZoom;
            foreach (var item in RowHeaderItems) item.ItemSize = item.BaseSize * CurrentZoom;
        }

        public void UpdateToolbox()
        {
            AvailableToolboxItems.Clear();
            if (WindowStatus?.LayoutProfile == null) return;

            if (!WindowStatus.LayoutProfile.Placements.Any(p => p.ComponentType == ComponentType.AlbumArt))
                AvailableToolboxItems.Add(new ToolboxItem { ComponentType = ComponentType.AlbumArt });

            if (!WindowStatus.LayoutProfile.Placements.Any(p => p.ComponentType == ComponentType.Lyrics))
                AvailableToolboxItems.Add(new ToolboxItem { ComponentType = ComponentType.Lyrics });

            if (!WindowStatus.LayoutProfile.Placements.Any(p => p.ComponentType == ComponentType.SongInfo))
                AvailableToolboxItems.Add(new ToolboxItem { ComponentType = ComponentType.SongInfo });
        }

        public void UpdateHeaders()
        {
            if (WindowStatus?.LayoutProfile == null) return;

            var colDefs = WindowStatus.LayoutProfile.ColumnDefinitions;
            ColumnHeaderItems.Clear();
            for (int i = 0; i < colDefs.Count; i++)
            {
                ColumnHeaderItems.Add(new HeaderItemModel
                {
                    Index = i,
                    Definition = colDefs[i],
                    Parent = this,
                    CanDelete = colDefs.Count > 1,
                });
            }

            var rowDefs = WindowStatus.LayoutProfile.RowDefinitions;
            RowHeaderItems.Clear();
            for (int i = 0; i < rowDefs.Count; i++)
            {
                RowHeaderItems.Add(new HeaderItemModel
                {
                    Index = i,
                    Definition = rowDefs[i],
                    Parent = this,
                    CanDelete = rowDefs.Count > 1
                });
            }
        }

        [RelayCommand]
        private void AddRow()
        {
            WindowStatus?.LayoutProfile?.RowDefinitions.Add("1*");
            NotifyLimitsChanged();
            UpdateHeaders();
            RequestRender();
        }

        [RelayCommand]
        private void AddCol()
        {
            WindowStatus?.LayoutProfile?.ColumnDefinitions.Add("1*");
            NotifyLimitsChanged();
            UpdateHeaders();
            RequestRender();
        }

        [RelayCommand]
        private void RemoveRow()
        {
            if (WindowStatus?.LayoutProfile?.RowDefinitions.Count > 1)
            {
                int lastRow = WindowStatus.LayoutProfile.RowDefinitions.Count - 1;
                WindowStatus.LayoutProfile.RowDefinitions.RemoveAt(lastRow);
                WindowStatus.LayoutProfile.Placements.RemoveAll(p => p.Row >= lastRow);

                NotifyLimitsChanged();

                foreach (var p in WindowStatus.LayoutProfile.Placements.Where(p => p.Row + p.RowSpan > lastRow))
                    p.RowSpan = lastRow - p.Row;

                CheckSelectionValidity();
                UpdateToolbox();
                UpdateHeaders();
                RequestRender();
            }
        }

        [RelayCommand]
        private void RemoveCol()
        {
            if (WindowStatus?.LayoutProfile?.ColumnDefinitions.Count > 1)
            {
                int lastCol = WindowStatus.LayoutProfile.ColumnDefinitions.Count - 1;
                WindowStatus.LayoutProfile.ColumnDefinitions.RemoveAt(lastCol);
                WindowStatus.LayoutProfile.Placements.RemoveAll(p => p.Column >= lastCol);

                NotifyLimitsChanged();

                foreach (var p in WindowStatus.LayoutProfile.Placements.Where(p => p.Column + p.ColumnSpan > lastCol))
                    p.ColumnSpan = lastCol - p.Column;

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
                WindowStatus?.LayoutProfile?.Placements.Remove(SelectedPlacement);
                SelectedPlacement = null;
                UpdateToolbox();
                RequestRender();
            }
        }

        [RelayCommand]
        private void InsertRowAction(int index) => InsertRowAt(index);

        [RelayCommand]
        private void InsertRowAfterAction(int index)
        {
            InsertRowAt(index + 1);
        }

        [RelayCommand]
        private void DeleteRowAction(int index) => DeleteRowAt(index);

        [RelayCommand]
        private void ToggleRowStarAction(int index)
        {
            var current = WindowStatus.LayoutProfile.RowDefinitions[index];
            WindowStatus.LayoutProfile.RowDefinitions[index] = current.Equals("Auto", StringComparison.OrdinalIgnoreCase) ? "1*" : "Auto";
            UpdateHeaders();
            RequestRender();
        }

        [RelayCommand]
        private void InsertColAction(int index) => InsertColAt(index);

        [RelayCommand]
        private void InsertColAfterAction(int index)
        {
            InsertColAt(index + 1);
        }

        [RelayCommand]
        private void DeleteColAction(int index) => DeleteColAt(index);

        [RelayCommand]
        private void ToggleColStarAction(int index)
        {
            var current = WindowStatus.LayoutProfile.ColumnDefinitions[index];
            WindowStatus.LayoutProfile.ColumnDefinitions[index] = current.Equals("Auto", StringComparison.OrdinalIgnoreCase) ? "1*" : "Auto";
            UpdateHeaders();
            RequestRender();
        }

        partial void OnSelectedPlacementChanged(ComponentPlacement? oldValue, ComponentPlacement? newValue)
        {
            if (oldValue is INotifyPropertyChanged oldModel)
            {
                oldModel.PropertyChanged -= OnModelPropertyChanged;
            }

            if (newValue is INotifyPropertyChanged newModel)
            {
                newModel.PropertyChanged += OnModelPropertyChanged;
            }

            RequestRender();
        }

        partial void OnWindowStatusChanged(LyricsWindowStatus oldValue, LyricsWindowStatus newValue)
        {
            if (oldValue?.LayoutProfile is INotifyPropertyChanged oldProfile)
                oldProfile.PropertyChanged -= OnModelPropertyChanged;

            if (newValue?.LayoutProfile is INotifyPropertyChanged newProfile)
                newProfile.PropertyChanged += OnModelPropertyChanged;

            UpdateToolbox();
            UpdateHeaders();
            RequestRender();
        }

        private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            NotifyLimitsChanged();
            RequestRender();
        }

        public void InsertRowAt(int index)
        {
            WindowStatus.LayoutProfile.RowDefinitions.Insert(index, "1*");
            foreach (var p in WindowStatus.LayoutProfile.Placements)
            {
                if (p.Row >= index) p.Row++;
                else if (p.Row + p.RowSpan > index) p.RowSpan++;
            }
            NotifyLimitsChanged();
            UpdateHeaders();
            RequestRender();
        }

        public void InsertColAt(int index)
        {
            WindowStatus.LayoutProfile.ColumnDefinitions.Insert(index, "1*");
            foreach (var p in WindowStatus.LayoutProfile.Placements)
            {
                if (p.Column >= index) p.Column++;
                else if (p.Column + p.ColumnSpan > index) p.ColumnSpan++;
            }
            NotifyLimitsChanged();
            UpdateHeaders();
            RequestRender();
        }

        public void DeleteRowAt(int index)
        {
            if (WindowStatus.LayoutProfile.RowDefinitions.Count <= 1) return;
            WindowStatus.LayoutProfile.RowDefinitions.RemoveAt(index);

            var toRemove = WindowStatus.LayoutProfile.Placements.Where(p => p.Row == index && p.RowSpan == 1).ToList();
            foreach (var p in WindowStatus.LayoutProfile.Placements)
            {
                if (p.Row > index) p.Row--;
                else if (p.Row <= index && (p.Row + p.RowSpan) > index) p.RowSpan--;
            }
            foreach (var p in toRemove) WindowStatus.LayoutProfile.Placements.Remove(p);

            CheckSelectionValidity();
            UpdateToolbox();
            UpdateHeaders();
            RequestRender();
        }

        public void DeleteColAt(int index)
        {
            if (WindowStatus.LayoutProfile.ColumnDefinitions.Count <= 1) return;
            WindowStatus.LayoutProfile.ColumnDefinitions.RemoveAt(index);

            var toRemove = WindowStatus.LayoutProfile.Placements.Where(p => p.Column == index && p.ColumnSpan == 1).ToList();
            foreach (var p in WindowStatus.LayoutProfile.Placements)
            {
                if (p.Column > index) p.Column--;
                else if (p.Column <= index && (p.Column + p.ColumnSpan) > index) p.ColumnSpan--;
            }
            foreach (var p in toRemove) WindowStatus.LayoutProfile.Placements.Remove(p);

            CheckSelectionValidity();
            UpdateToolbox();
            UpdateHeaders();
            RequestRender();
        }

        private void CheckSelectionValidity()
        {
            if (SelectedPlacement != null && !WindowStatus.LayoutProfile.Placements.Contains(SelectedPlacement))
            {
                SelectedPlacement = null;
            }
        }

        private void NotifyLimitsChanged()
        {
            OnPropertyChanged(nameof(MaxRowIndex));
            OnPropertyChanged(nameof(MaxColIndex));
            OnPropertyChanged(nameof(MaxRowSpan));
            OnPropertyChanged(nameof(MaxColSpan));
        }
    }
}
