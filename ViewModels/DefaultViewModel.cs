using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVNFOEditor.Features;
using MVNFOEditor.Helpers;
using MVNFOEditor.Services;
using MVNFOEditor.Settings;
using SukiUI;
using SukiUI.Dialogs;
using SukiUI.Models;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels;

public partial class DefaultViewModel : ObservableObject
{
    private static ISettings _settings;
    private readonly SukiTheme _theme;
    [ObservableProperty] private PageBase? _activePage;
    [ObservableProperty] private bool _animationsEnabled;

    [ObservableProperty] private ThemeVariant _baseTheme;
    [ObservableProperty] private bool _windowLocked;

    public DefaultViewModel(IEnumerable<PageBase> pages, PageNavigationService nav)
    {
        DialogManager = App.GetDialogManager();
        ToastManager = App.GetToastManager();
        _settings = App.GetSettings();
        Pages = new AvaloniaList<PageBase>(pages.OrderBy(x => x.Index).ThenBy(x => x.DisplayName));
        _theme = SukiTheme.GetInstance();
        nav.NavigationRequested += t =>
        {
            var page = Pages.FirstOrDefault(x => x.GetType() == t);
            if (page is null || ActivePage?.GetType() == t) return;
            ActivePage = page;
        };
        Themes = _theme.ColorThemes;
        AnimationsEnabled = _settings.AnimatedBackground;
        BaseTheme = _settings.LightMode ? ThemeVariant.Light : ThemeVariant.Dark;
        //if (_settings.Theme != null){_theme.ChangeColorTheme(settingsData.Theme);}
        _theme.OnBaseThemeChanged += async variant =>
        {
            BaseTheme = variant;
            ToastHelper.ShowSuccess("Theme Changed", $"Theme has changed to {variant}.");
            //await SukiHost.ShowToast("Successfully Changed Theme", $"Changed Theme To {variant}", NotificationType.Success);
        };
        _theme.OnColorThemeChanged += async theme =>
            ToastHelper.ShowSuccess("Color Changed", $"Color has changed to {theme.DisplayName}.");
    }

    public IAvaloniaList<PageBase> Pages { get; set; }
    public IAvaloniaReadOnlyList<SukiColorTheme> Themes { get; }
    public ISukiDialogManager DialogManager { get; }
    public ISukiToastManager ToastManager { get; }

    [RelayCommand]
    private void ToggleBaseTheme()
    {
        _theme.SwitchBaseTheme();
    }

    public ArtistListParentViewModel GetParentView()
    {
        //string message = "i love you";
        //SukiHost.ShowToast("Message", message);
        //Debug.WriteLine(message);
        var parentVM = Pages.First(x => x.DisplayName == "Artist List");
        return (ArtistListParentViewModel)parentVM;
        //return Pages.First(x => x.DisplayName == displayName);
    }

    public ISukiToastManager GetToastManager()
    {
        return App.GetToastManager();
    }

    public ISukiDialogManager GetDialogManager()
    {
        return App.GetDialogManager();
    }
}