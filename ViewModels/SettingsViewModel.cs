using System;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using MVNFOEditor.Features;
using SukiUI.Models;
using SukiUI;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Input;

namespace MVNFOEditor.ViewModels
{
    public partial class SettingsViewModel : PageBase
    {
        public IAvaloniaReadOnlyList<SukiColorTheme> AvailableColors { get; }

        private readonly SukiTheme _theme = SukiTheme.GetInstance();

        [ObservableProperty] private bool _isBackgroundAnimated;
        [ObservableProperty] private bool _isLightTheme;

        [ObservableProperty] private string _mvText;
        [ObservableProperty] private string _ffmpegText;
        [ObservableProperty] private string _ytdlText;

        [ObservableProperty] private SettingsItemViewModel _activeVm;
        [ObservableProperty] private int _limit = 5;
        public SettingsViewModel() : base("Settings", MaterialIconKind.Layers, 2)
        {
            _activeVm = new SettingsItemViewModel(1, OnRecurseClicked);
            AvailableColors = _theme.ColorThemes;
            _theme.OnBaseThemeChanged += variant =>
                IsLightTheme = variant == ThemeVariant.Light;
            _theme.OnBackgroundAnimationChanged += value =>
                IsBackgroundAnimated = value;

            MvText = App.GetSettings().RootFolder;
            FfmpegText = App.GetSettings().FFMPEGPath;
            YtdlText = App.GetSettings().YTDLPath;
            IsBackgroundAnimated = App.GetSettings().AnimatedBackground;
            IsLightTheme = App.GetSettings().LightOrDark == ThemeVariant.Light;
        }

        private void OnRecurseClicked(SettingsItemViewModel newRecursiveVm)
        {
            ActiveVm = newRecursiveVm;
        }
        partial void OnIsLightThemeChanged(bool value) =>
            _theme.ChangeBaseTheme(value ? ThemeVariant.Light : ThemeVariant.Dark);

        partial void OnIsBackgroundAnimatedChanged(bool value) =>
            _theme.SetBackgroundAnimationsEnabled(value);

        [RelayCommand]
        public void SwitchToColorTheme(SukiColorTheme colorTheme) =>
            _theme.ChangeColorTheme(colorTheme);
    }
}