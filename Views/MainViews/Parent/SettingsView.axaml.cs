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
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Views
{
    public partial class SettingsView : UserControl
    {
        private SettingsData _settings;
        public SettingsView()
        {
            InitializeComponent();
            _settings = new SettingsData();
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

        public async void BrowseFFMPEGPath(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var path = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Select FFMPEG Path",
                AllowMultiple = false
            });
            if (path.Count > 0)
            {
                _settings.FFMPEGPath = path[0].TryGetLocalPath();
                FFMPEGInput.Text = path[0].TryGetLocalPath();
            }
        }

        public async void BrowseYTDLPath(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var path = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Select YTDL Path",
                AllowMultiple = false
            });
            
            if (path.Count > 0)
            {
                _settings.YTDLPath = path[0].TryGetLocalPath();
                YTDLInput.Text = path[0].TryGetLocalPath();
            }
        }

        public async void SaveSettings(object sender, RoutedEventArgs e)
        {
            MusicDbContext db = App.GetDBContext();
            SettingsData preData = db.SettingsData.SingleOrDefault();
            _settings.ScreenshotSecond = int.Parse(ScreenshotSecond.Text);
            _settings.YTDLFormat = YTDLFormat.Text;
            if (preData != null)
            {
                db.SettingsData.Remove(preData);
            }
            db.SettingsData.Add(_settings);
            await db.SaveChangesAsync();
            SukiHost.ShowToast("Success!", "Settings successfully saved");
        }

        public void ProgressTest(object sender, RoutedEventArgs e)
        {
            var vmTest = App.GetVM();
        }
    }
}