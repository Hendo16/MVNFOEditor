using Avalonia.Controls;
using MVNFOEditor.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using MVNFOEditor.DB;
using MVNFOEditor.Models;
using SukiUI.Controls;
using Microsoft.EntityFrameworkCore;
using MVNFOEditor.Settings;
using Newtonsoft.Json.Linq;
using SukiUI.Enums;

namespace MVNFOEditor.Views
{
    public partial class SettingsView : UserControl
    {
        private static ISettings _settings;
        public SettingsView()
        {
            InitializeComponent();
        }

        public async void BrowseMVFolder(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                Title = "Select Music Video Folder",
                AllowMultiple = false
            });
            if (folder.Count > 0)
            {
                _settings.RootFolder = folder[0].TryGetLocalPath();
                MVInput.Text = folder[0].TryGetLocalPath();
            }
        }

        public async void BrowseDeviceId(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Select device_client_id_blob",
                AllowMultiple = false
            });
            if (file.Count > 0)
            {
                _settings.AM_DeviceId = file[0].TryGetLocalPath();
                MVInput.Text = file[0].TryGetLocalPath();
            }
        }
        
        public async void BrowseDeviceKey(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Select device_private_key",
                AllowMultiple = false
            });
            if (file.Count > 0)
            {
                _settings.AM_DeviceKey = file[0].TryGetLocalPath();
                MVInput.Text = file[0].TryGetLocalPath();
            }
        }
    }
}