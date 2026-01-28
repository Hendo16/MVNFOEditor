using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using MVNFOEditor.Features;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using MVNFOEditor.Settings;
using SukiUI;
using SukiUI.Dialogs;
using SukiUI.Models;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels;

public partial class SettingsViewModel : PageBase
{
    private static ISettings _settings;

    private readonly SukiTheme _theme = SukiTheme.GetInstance();

    [ObservableProperty] private SettingsItemViewModel _activeVm;
    [ObservableProperty] private string _appleMusicToken;
    [ObservableProperty] private string _appleMusicTokenProvided;

    [ObservableProperty] private bool _backgroundTransitions;
    [ObservableProperty] private string _ffmpegText;
    [ObservableProperty] private bool _isLightTheme;
    [ObservableProperty] private int _limit = 5;

    [ObservableProperty] private string _mvText;

    [ObservableProperty] [property: Category("Download")] [property: DisplayName("Desired Resolution")]
    private Resolution _resolutions;

    [ObservableProperty] private string _screenshotSecond;
    [ObservableProperty] private string _userId;
    [ObservableProperty] private string _userKey;
    [ObservableProperty] private string _ytdlFormat;
    [ObservableProperty] private string _ytdlText;

    public SettingsViewModel() : base("Settings", MaterialIconKind.Layers, 2)
    {
        _activeVm = new SettingsItemViewModel(1, OnRecurseClicked);
        _settings = App.GetSettings();
        AvailableColors = _theme.ColorThemes;
        _theme.OnBaseThemeChanged += variant =>
            IsLightTheme = variant == ThemeVariant.Light;

        MvText = _settings.RootFolder;
        BackgroundTransitions = _settings.BackgroundTransitions;
        IsLightTheme = _settings.LightMode;

        ScreenshotSecond = _settings.ScreenshotSecond.ToString();
        YtdlFormat = _settings.YTDLFormat;

        AppleMusicToken = _settings.AM_AccessToken;
        AppleMusicTokenProvided = AppleMusicToken == "n/a" ? "Not Found" : "Found!";
        UserId = _settings.AM_DeviceId;
        UserKey = _settings.AM_DeviceKey;

        //Resolutions = AdvSettings.Resolution;
        //SelectedResolution = Resolutions.FirstOrDefault(r => r == _settings.YTDLResolution);
    }

    public IAvaloniaReadOnlyList<SukiColorTheme> AvailableColors { get; }

    public void SaveChanges()
    {
        _settings.ScreenshotSecond = int.Parse(ScreenshotSecond);
        _settings.Resolution = Resolutions;
        _settings.YTDLFormat = YtdlFormat;
        ToastHelper.ShowSuccess("Settings", "Successfully saved");
    }

    public void UpdateAMToken()
    {
        App.GetDialogManager().CreateDialog()
            .WithViewModel(dialog => new AMUserSubmissionViewModel(dialog))
            .TryShow();
    }

    public void GenerateYTMHeaders()
    {
        App.GetDialogManager().CreateDialog()
            .WithViewModel(dialog => new YTMHeaderSubmissionViewModel(dialog))
            .TryShow();
    }

    private void OnRecurseClicked(SettingsItemViewModel newRecursiveVm)
    {
        ActiveVm = newRecursiveVm;
    }

    partial void OnIsLightThemeChanged(bool value)
    {
        _theme.ChangeBaseTheme(value ? ThemeVariant.Light : ThemeVariant.Dark);
    }

    [RelayCommand]
    public void SwitchToColorTheme(SukiColorTheme colorTheme)
    {
        _theme.ChangeColorTheme(colorTheme);
    }
}