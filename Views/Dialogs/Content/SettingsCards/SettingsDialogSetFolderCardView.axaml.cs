using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Runtime;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class SettingsDialogSetFolderCardView : UserControl
    {
        public SettingsDialogSetFolderCardView()
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
            if (folder.Count > 0 && DataContext is SettingsDialogViewModel viewModel)
            {
                viewModel.SetMVFolder(folder[0].TryGetLocalPath());
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
            if (path.Count > 0 && DataContext is SettingsDialogViewModel viewModel)
            {
                viewModel.SetFFMPEGPath(path[0].TryGetLocalPath());
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

            if (path.Count > 0 && DataContext is SettingsDialogViewModel viewModel)
            {
                viewModel.SetYTDLPath(path[0].TryGetLocalPath());
                YTDLInput.Text = path[0].TryGetLocalPath();
            }
        }
    }
}