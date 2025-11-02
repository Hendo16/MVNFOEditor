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

    public async void AppleMusicDownloadTesting()
    {
        var _amHelper = await AppleMusicDLHelper.CreateHelper();
        var waveVM = new WaveProgressViewModel();
        waveVM.HeaderText = "Downloading Closure - ";
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(dialog => waveVM)
            .TryShow();
        await DownloadViewTesting(
            "https://filebrowser.hendoserver.com/api/public/dl/zZPhzoAs/b3872530-9952-47e9-924c-81170aa84382/media_server/MusicVideos/Chevelle/Closure.mkv",
            "test.mkv", waveVM);
        App.GetVM().GetDialogManager().DismissDialog();
    }

    public void AMNFOTest()
    {
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

    public async Task DownloadViewTesting(string url, string filename, WaveProgressViewModel wavevm)
    {
        using var client = new HttpClient();
        long totalSize = 0;

        var size_response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
        if (size_response.Content.Headers.ContentLength.HasValue)
            totalSize = size_response.Content.Headers.ContentLength.Value;

        using var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
        long downloaded = 0;
        var stopwatch = Stopwatch.StartNew();

        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        using var stream = await response.Content.ReadAsStreamAsync();

        var buffer = new byte[32768];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            fileStream.Write(buffer, 0, bytesRead);
            downloaded += bytesRead;

            // Progress update
            var percent = (double)downloaded / totalSize * 100;
            var speed = downloaded / (stopwatch.Elapsed.TotalSeconds + 1); // Bytes per second
            var speedStr = speed >= 1_048_576 // 1MB = 1,048,576 bytes
                ? $"{speed / 1_048_576:0.0} MB/s"
                : $"{speed / 1024:0.0} KB/s";
            Console.Write($"\rProgress: {percent:0.0}% | Speed: {speedStr} ");
            wavevm.UpdateProgress(percent);
            wavevm.UpdateDownloadSpeed(speedStr);
        }
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