using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using MVNFOEditor.Features;
using SukiUI.Models;
using SukiUI;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Input;
using MVNFOEditor.DB;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using MVNFOEditor.Settings;
using MVNFOEditor.Views;
using SukiUI.Controls;
using SukiUI.Dialogs;
using SukiUI.Enums;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels
{
    public partial class SettingsViewModel : PageBase
    {
        public IAvaloniaReadOnlyList<SukiColorTheme> AvailableColors { get; }

        private readonly SukiTheme _theme = SukiTheme.GetInstance();

        private static ISettings _settings;

        [ObservableProperty] private bool _backgroundTransitions;
        [ObservableProperty] private bool _isLightTheme;

        [ObservableProperty] private string _mvText;
        [ObservableProperty] private string _ffmpegText;
        [ObservableProperty] private string _ytdlText;
        [ObservableProperty] private string _screenshotSecond;
        [ObservableProperty] private string _selectedResolution;
        [ObservableProperty] private string _ytdlFormat;

        [ObservableProperty] private SettingsItemViewModel _activeVm;
        [ObservableProperty] private int _limit = 5;

        [ObservableProperty] private string[] _resolutions =
        {
            "4K" ,
            "1440p",
            "1080p",
            "720p"
        };
        public SettingsViewModel() : base("Settings", MaterialIconKind.Layers, 2)
        {
            _activeVm = new SettingsItemViewModel(1, OnRecurseClicked);
            _settings = App.GetSettings();
            AvailableColors = _theme.ColorThemes;
            _theme.OnBaseThemeChanged += variant =>
                IsLightTheme = variant == ThemeVariant.Light;

            MvText = _settings.RootFolder;
            ScreenshotSecond = _settings.ScreenshotSecond.ToString();
            YtdlFormat = _settings.YTDLFormat;
            BackgroundTransitions = _settings.BackgroundTransitions;
            IsLightTheme = _settings.LightMode;
            SelectedResolution = Resolutions.FirstOrDefault(r => r == _settings.YTDLResolution);
        }

        public void SaveChanges()
        {
            _settings.ScreenshotSecond = int.Parse(ScreenshotSecond);
            _settings.YTDLResolution = SelectedResolution;
            _settings.YTDLFormat = YtdlFormat;
            App.GetVM().GetToastManager().CreateToast()
                .WithTitle("Success!")
                .WithContent("Settings successfully saved")
                .OfType(NotificationType.Success)
                .Queue();
        }

        public async void AppleMusicDownloadTesting()
        {
            AppleMusicDLHelper _amHelper = new AppleMusicDLHelper();
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            waveVM.HeaderText = "Downloading Closure - ";
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => waveVM)
                .TryShow();
            await DownloadViewTesting("https://filebrowser.hendoserver.com/api/public/dl/zZPhzoAs/b3872530-9952-47e9-924c-81170aa84382/media_server/MusicVideos/Chevelle/Closure.mkv", "test.mkv", waveVM);
            App.GetVM().GetDialogManager().DismissDialog();
        }

        public void ArtistMergeTest()
        {
            Artist testArt1 = App.GetDBContext().Artist.First(a => a.Name == "Architects");
            Artist testArt2 = App.GetDBContext().Artist.First(a => a.Name == "Slipknot");
            App.GetVM().GetDialogManager().CreateDialog()
                .OfType(NotificationType.Warning)
                .WithTitle("Merge Artists")
                .WithViewModel(dialog => new MergeArtistDialogViewModel(dialog, testArt1, testArt2))
                .WithActionButton("Extra Close Button", _ => { }, true, "Flat")
                .TryShow();
        }

        public async Task DownloadViewTesting(string url, string filename, WaveProgressViewModel wavevm)
        {
            using HttpClient client = new HttpClient();
            long totalSize = 0;
        
            var size_response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
            if (size_response.Content.Headers.ContentLength.HasValue) totalSize = size_response.Content.Headers.ContentLength.Value;
        
            using FileStream fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
            long downloaded = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
        
            using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            using Stream stream = await response.Content.ReadAsStreamAsync();
            
            byte[] buffer = new byte[32768];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                fileStream.Write(buffer, 0, bytesRead);
                downloaded += bytesRead;

                // Progress update
                double percent = (double)downloaded / totalSize * 100;
                double speed = downloaded / (stopwatch.Elapsed.TotalSeconds + 1); // Bytes per second
                string speedStr = speed >= 1_048_576  // 1MB = 1,048,576 bytes
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
        partial void OnIsLightThemeChanged(bool value) =>
            _theme.ChangeBaseTheme(value ? ThemeVariant.Light : ThemeVariant.Dark);

        [RelayCommand]
        public void SwitchToColorTheme(SukiColorTheme colorTheme) =>
            _theme.ChangeColorTheme(colorTheme);
    }
}