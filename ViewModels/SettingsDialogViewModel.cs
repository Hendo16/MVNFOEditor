using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVNFOEditor.Models;
using MVNFOEditor.Settings;
using SukiUI;
using SukiUI.Controls;
using SukiUI.Enums;
using SukiUI.Models;

namespace MVNFOEditor.ViewModels
{
    public partial class SettingsDialogViewModel : ObservableObject
    {
        private static ISettings _settings;
        private readonly SukiTheme _theme = SukiTheme.GetInstance();
        public IAvaloniaReadOnlyList<SukiColorTheme> AvailableColors { get; }
        public Action<bool>? BackgroundTransitionsChanged { get; set; }
        [ObservableProperty] private string _screenshotSecond;
        [ObservableProperty] private string _ytdlFormat;
        [ObservableProperty] private bool _backgroundTransitions;
        [ObservableProperty] private string _selectedResolution;
        [ObservableProperty] private bool _isLightTheme;
        [ObservableProperty] private bool _backVisible;
        [ObservableProperty] private bool _nextVisible;
        [ObservableProperty] private int _stepIndex;
        //[ObservableProperty] private SukiBackgroundStyle _backgroundStyle ;
        [ObservableProperty] private string[] _resolutions =
        {
            "4K" ,
            "1440p",
            "1080p",
            "720p"
        };

        public SettingsDialogViewModel()
        {
            _settings = App.GetSettings();
            AvailableColors = _theme.ColorThemes;
            BackVisible = false;
            NextVisible = true;
            IsLightTheme = _settings.LightMode;
            SelectedResolution = _settings.YTDLResolution;
            ScreenshotSecond = _settings.ScreenshotSecond.ToString();
        }

        public void HandleFinish()
        {
            _settings.ScreenshotSecond = ScreenshotSecond == null ? 20 : int.Parse(ScreenshotSecond);
            _settings.YTDLResolution = SelectedResolution != null ? SelectedResolution : "1080p";
            _settings.YTDLFormat = YtdlFormat == null ? "" : YtdlFormat;
            
            App.GetVM().GetDialogManager().DismissDialog();
            //Task.Run(() => { App.GetVM().GetParentView().InitList(); });
        }

        public void HandleBackwards(Carousel settingsPages)
        {
            StepIndex--;
            if (!NextVisible)
            {
                NextVisible = true;

            }
            if (StepIndex == 0)
            {
                BackVisible = false;
            }
            settingsPages.Previous();
        }

        public void HandleForward(Carousel settingsPages)
        {
            StepIndex++;
            if (!BackVisible)
            {
                BackVisible = true;
            }
            if (StepIndex == 4)
            {
                NextVisible = false;
            }
            settingsPages.Next();
        }

        public void FFMPEGDown()
        {
            Process.Start(new ProcessStartInfo("https://www.ffmpeg.org/download.html") { UseShellExecute = true });
        }

        public void YTDLDown()
        {
            Process.Start(new ProcessStartInfo("https://github.com/yt-dlp/yt-dlp/releases") { UseShellExecute = true });
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
}