using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MVNFOEditor.ViewModels;
using System.Diagnostics;
using MVNFOEditor.Models;
using System;
using System.Linq;
using MVNFOEditor.Helpers;

namespace MVNFOEditor;

public partial class Settings : Window
{
    private SettingsViewModel _settingsViewModel;
    public Settings()
    {
        InitializeComponent();

        DataContextChanged += CheckSettingsData;
    }

    private void CheckSettingsData(object? sender, EventArgs e)
    {
        _settingsViewModel = (SettingsViewModel)DataContext;
        _settingsViewModel.MVDBContext.Database.EnsureCreated();
        if (_settingsViewModel.MVDBContext.SettingsData.Count() != 0)
        {
            LoadData(_settingsViewModel.MVDBContext.SettingsData.SingleOrDefault());
        }
    }

    private void LoadData(SettingsData setData)
    {
        PathInput.Text = setData.RootFolder;
        FFMPEGPath.Text = setData.FFMPEGPath;
        YTDLPath.Text = setData.YTDLPath;
        OutputFolder.Text = setData.OutputFolder;
    }

    private void SaveSettings(object sender, RoutedEventArgs e)
    {
        SettingsData setData = new SettingsData();

        setData.RootFolder = (PathInput.Text != null) ? PathInput.Text : "";
        setData.FFMPEGPath = (FFMPEGPath.Text != null) ? FFMPEGPath.Text : "";
        setData.YTDLPath = (YTDLPath.Text != null) ? YTDLPath.Text : "";
        setData.OutputFolder = (OutputFolder.Text != null) ? OutputFolder.Text : "";
        
        //Update YTDL Settings
        YTDLHelper ytdlHelper = App.GetYTDLHelper();
        ytdlHelper.UpdateSettings(setData);

        _settingsViewModel.SettingsData = setData;
    }
}