using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVNFOEditor.Models;
using SukiUI;
using SukiUI.Controls;
using SukiUI.Models;

namespace MVNFOEditor.ViewModels
{
    public partial class SettingsDialogViewModel : ObservableObject
    {
        private readonly SukiTheme _theme = SukiTheme.GetInstance();
        public IAvaloniaReadOnlyList<SukiColorTheme> AvailableColors { get; }
        [ObservableProperty] private bool _isBackgroundAnimated;
        [ObservableProperty] private bool _isLightTheme;
        [ObservableProperty] private int _stepIndex;

        public SettingsDialogViewModel()
        {
            AvailableColors = _theme.ColorThemes;
            IsLightTheme = _theme.ActiveBaseTheme == ThemeVariant.Light;
            IsBackgroundAnimated = _theme.IsBackgroundAnimated;
        }

        public async void HandleFinish()
        {
            var db = App.GetDBContext();
            var localSettings = App.GetSettings();
            SettingsData preData = db.SettingsData.SingleOrDefault();
            if (preData != null)
            {
                db.SettingsData.Remove(preData);
            }
            db.SettingsData.Add(localSettings);
            db.SaveChanges();
            SukiHost.CloseDialog();
            Task.Run(() => { App.GetVM().GetParentView().InitList(); });
        }

        public void HandleBackwards()
        {
            StepIndex--;
        }

        public void HandleForward()
        {
            StepIndex++;
        }
        public void SetMVFolder(string path)
        {
            App.GetSettings().RootFolder = path;
        }

        public void SetFFMPEGPath(string path)
        {
            App.GetSettings().FFMPEGPath = path;
        }

        public void SetYTDLPath(string path)
        {
            App.GetSettings().YTDLPath = path;
        }

        partial void OnIsLightThemeChanged(bool value)
        {
            _theme.ChangeBaseTheme(value ? ThemeVariant.Light : ThemeVariant.Dark);
            App.GetSettings().LightOrDark = value ? ThemeVariant.Light : ThemeVariant.Dark;
        }

        partial void OnIsBackgroundAnimatedChanged(bool value)
        {
            _theme.SetBackgroundAnimationsEnabled(value);
            App.GetSettings().AnimatedBackground = value;
        }

        [RelayCommand]
        public void SwitchToColorTheme(SukiColorTheme colorTheme)
        {
            _theme.ChangeColorTheme(colorTheme);
            App.GetSettings().Theme = colorTheme;
        }

    }
}