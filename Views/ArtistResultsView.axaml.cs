using Avalonia.Controls;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class ArtistResultsView : UserControl
    {
        public ArtistResultsView()
        {
            InitializeComponent();
        }

        public void SearchArtist(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ArtistResultsViewModel viewModel)
            {
                viewModel.SearchArtist();
            }
        }
    }
}
