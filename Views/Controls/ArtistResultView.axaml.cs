using Avalonia.Controls;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class ArtistResultView : UserControl
    {
        public ArtistResultView()
        {
            InitializeComponent();
        }

        public void GrabArtist(object sender, RoutedEventArgs e)
        {
            if (DataContext is ArtistResultViewModel viewModel)
            {
                viewModel.GrabArtist();
            }
        }
    }
}
