using Avalonia.Controls;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class AlbumResultView : UserControl
    {
        public AlbumResultView()
        {
            InitializeComponent();
        }

        public void GrabAlbum(object sender, RoutedEventArgs e)
        {
            if (DataContext is AlbumResultViewModel viewModel)
            {
                viewModel.GrabAlbum();
            }
        }
    }
}