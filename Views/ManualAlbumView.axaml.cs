using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class ManualAlbumView : UserControl
    {
        public ManualAlbumView()
        {
            InitializeComponent();
        }

        public async void CoverSearch(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var path = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Select Album Cover",
                AllowMultiple = false
            });
            if (path.Count > 0 && DataContext is ManualAlbumViewModel viewModel)
            {
                viewModel.LoadCover(path[0].TryGetLocalPath());
            }
        }
    }
}