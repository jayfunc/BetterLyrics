using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels {
    public partial class MainViewModel : ObservableObject {
        [ObservableProperty]
        private string? _title;

        [ObservableProperty]
        private string? _artist;

        [ObservableProperty]
        private ObservableCollection<Color> _coverImageDominantColors =
            [Colors.Transparent, Colors.Transparent, Colors.Transparent];

        [ObservableProperty]
        private Color _startGraidentColor;

        [ObservableProperty]
        private Color _endGraidentColor;

        [ObservableProperty]
        private bool _aboutToUpdateUI;

        [ObservableProperty]
        private bool _isSmallScreenMode;
    }
}
