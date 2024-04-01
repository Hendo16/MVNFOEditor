using Avalonia.Controls;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class ArtistListView : UserControl
    {
        public ArtistListView()
        {
            InitializeComponent();
        }

        public void AddArtist(object sender, RoutedEventArgs e)
        {
            if (DataContext is ArtistListViewModel viewModel)
            {
                viewModel.AddArtist();
            }
        }
    }
}
