using Avalonia.Controls;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class EditAlbumDialogView : UserControl
    {
        public EditAlbumDialogView()
        {
            InitializeComponent();
        }
        public void UpdateAlbum(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditAlbumDialogViewModel viewModel)
            {
                viewModel.UpdateAlbum();
            }
        }

        public void DeleteAlbum(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditAlbumDialogViewModel viewModel)
            {
                viewModel.DeleteAlbum();
            }
        }
    }
}
