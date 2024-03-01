using Avalonia.Collections;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using SukiUI;
using SukiUI.Models;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using MVNFOEditor.Features;
using MVNFOEditor.Services;
using SukiUI.Controls;
namespace MVNFOEditor.ViewModels
{
    public partial class DefaultViewModel : ObservableObject
    {
        public IAvaloniaList<PageBase> Pages { get; set; }
        public IAvaloniaReadOnlyList<SukiColorTheme> Themes { get; }

        [ObservableProperty] private ThemeVariant _baseTheme;
        [ObservableProperty] private bool _animationsEnabled;
        [ObservableProperty] private bool _windowLocked = false;
        [ObservableProperty] private PageBase? _activePage;
        private readonly SukiTheme _theme;

        public DefaultViewModel(IEnumerable<PageBase> pages, PageNavigationService nav)
        {
            Pages = new AvaloniaList<PageBase>(pages.OrderBy(x => x.Index).ThenBy(x => x.DisplayName));
            _theme = SukiTheme.GetInstance();
            nav.NavigationRequested += t =>
            {
                var page = Pages.FirstOrDefault(x => x.GetType() == t);
                if (page is null || ActivePage?.GetType() == t) return;
                ActivePage = page;
            };
            Themes = _theme.ColorThemes;
            BaseTheme = _theme.ActiveBaseTheme;
            _theme.OnBaseThemeChanged += async variant =>
            {
                BaseTheme = variant;
                await SukiHost.ShowToast("Successfully Changed Theme", $"Changed Theme To {variant}");
            };
            _theme.OnColorThemeChanged += async theme =>
                await SukiHost.ShowToast("Successfully Changed Color", $"Changed Color To {theme.DisplayName}.");
            _theme.OnBackgroundAnimationChanged +=
                value => AnimationsEnabled = value;
        }

        [RelayCommand]
        private void ToggleBaseTheme() =>
            _theme.SwitchBaseTheme();

        public void ChangeTheme(SukiColorTheme theme) =>
            _theme.ChangeColorTheme(theme);

        public ArtistListParentViewModel GetParentView()
        {
            //string message = "i love you";
            //SukiHost.ShowToast("Message", message);
            //Debug.WriteLine(message);
            var parentVM = Pages.First(x => x.DisplayName == "Artist List");
            return (ArtistListParentViewModel)parentVM;
            //return Pages.First(x => x.DisplayName == displayName);
        }

        public void InitilizeSettings()
        {
            var SettingsVM = Pages.First(x => x.DisplayName == "Settings");
            SukiHost.ShowToast("Error!", "Database doesn't exist - go to Settings");
            GetParentView().CurrentContent = SettingsVM;
        }
    }
}