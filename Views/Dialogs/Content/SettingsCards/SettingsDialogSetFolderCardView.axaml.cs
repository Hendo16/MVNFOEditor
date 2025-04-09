using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using System.Runtime;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class SettingsDialogSetFolderCardView : UserControl
    {
        public SettingsDialogSetFolderCardView()
        {
            InitializeComponent();
            MVInput.Text = App.GetSettings().RootFolder;
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
                App.GetSettings().RootFolder = folder[0].TryGetLocalPath();
                MVInput.Text = folder[0].TryGetLocalPath();
            }
        }
    }
}