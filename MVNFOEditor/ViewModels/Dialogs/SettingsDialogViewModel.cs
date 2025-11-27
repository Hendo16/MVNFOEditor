using System;
using System.Diagnostics;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVNFOEditor.Settings;
using SukiUI;
using SukiUI.Models;

namespace MVNFOEditor.ViewModels;

public partial class SettingsDialogViewModel : ObservableObject
{
    private static ISettings _settings;
    private readonly SukiTheme _theme = SukiTheme.GetInstance();
    [ObservableProperty] private AdvSettingsViewModel _advSettings = new();
    [ObservableProperty] private bool _backgroundTransitions;
    [ObservableProperty] private bool _backVisible;
    [ObservableProperty] private int _carouselHeight;
    [ObservableProperty] private bool _isLightTheme;
    [ObservableProperty] private bool _nextVisible;
    [ObservableProperty] private int _stepIndex;

    public SettingsDialogViewModel()
    {
        _settings = App.GetSettings();
        AvailableColors = _theme.ColorThemes;
        BackVisible = false;
        NextVisible = true;
        IsLightTheme = _settings.LightMode;

        CarouselHeight = 350;
    }

    public IAvaloniaReadOnlyList<SukiColorTheme> AvailableColors { get; }
    public Action<bool>? BackgroundTransitionsChanged { get; set; }

    public async void HandleFinish()
    {
        _settings.Resolution = AdvSettings.Resolution;
        _settings.YTDLFormat = AdvSettings.YtdlFormat ?? "";
        _settings.ScreenshotSecond = AdvSettings.ScreenshotSecond;
        if (AdvSettings.DeviceId != null) _settings.AM_DeviceId = AdvSettings.DeviceId;
        if (AdvSettings.DeviceKey != null) _settings.AM_DeviceKey = AdvSettings.DeviceKey;
        if (AdvSettings.YtDLPath != null) _settings.YTDLPath = AdvSettings.YtDLPath;
        if (AdvSettings.FfmpegPath != null) _settings.FFMPEGPath = AdvSettings.FfmpegPath;
        if (AdvSettings.YtMusicAuthFile != null) _settings.YTM_AuthHeaders = AdvSettings.YtMusicAuthFile;
        if (AdvSettings.AppleMusicToken != null)
        {
            _settings.AM_UserToken = AdvSettings.AppleMusicToken;
            await App.GetAppleMusicDLHelper().UpdateUserToken(AdvSettings.AppleMusicToken);
        }

        await App.GetDBContext().Database.EnsureCreatedAsync();
        App.GetVM().GetDialogManager().DismissDialog();
        //Task.Run(() => { App.GetVM().GetParentView().InitList(); });
    }

    public void HandleBackwards(Carousel settingsPages)
    {
        StepIndex--;
        if (!NextVisible) NextVisible = true;
        if (StepIndex == 0) BackVisible = false;
        settingsPages.Previous();
    }

    public void HandleForward(Carousel settingsPages)
    {
        StepIndex++;
        if (!BackVisible) BackVisible = true;
        if (StepIndex == 4) NextVisible = false;
        settingsPages.Next();
    }

    partial void OnIsLightThemeChanged(bool value)
    {
        _theme.ChangeBaseTheme(value ? ThemeVariant.Light : ThemeVariant.Dark);
        _settings.LightMode = value;
    }

    partial void OnBackgroundTransitionsChanged(bool value)
    {
        BackgroundTransitionsChanged?.Invoke(value);
        //App.GetSettings().BackgroundTransitions = value;
    }

    [RelayCommand]
    public void SwitchToColorTheme(SukiColorTheme colorTheme)
    {
        _theme.ChangeColorTheme(colorTheme);
        //App.GetSettings().Theme = colorTheme;
    }
}