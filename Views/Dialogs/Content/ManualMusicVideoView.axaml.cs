using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MVNFOEditor.ViewModels;
using System;

namespace MVNFOEditor.Views
{
    public partial class ManualMusicVideoView : UserControl
    {
        public ManualMusicVideoView()
        {
            InitializeComponent();
        }

        public async void GrabLocalVideo(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var path = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Select Music Video",
                AllowMultiple = false
            });
            if (path.Count > 0 && DataContext is ManualMusicVideoViewModel viewModel)
            {
                viewModel.SetLocalVideo(path[0].TryGetLocalPath());
            }
        }
    }
}