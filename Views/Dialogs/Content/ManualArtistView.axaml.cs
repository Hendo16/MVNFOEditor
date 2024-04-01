using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Runtime;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class ManualArtistView : UserControl
    {
        public ManualArtistView()
        {
            InitializeComponent();
        }

        public async void BannerSearch(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var path = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Select Artist Banner",
                AllowMultiple = false
            });
            if (path.Count > 0 && DataContext is ManualArtistViewModel viewModel)
            {
                viewModel.LoadBanner(path[0].TryGetLocalPath());
            }
        }
    }
}
