using Avalonia.Controls;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class MusicVideoDetailsView : UserControl
    {
        public MusicVideoDetailsView()
        {
            InitializeComponent();
        }

        public void UpdateVideoInfo(object sender, RoutedEventArgs e)
        {
            if (DataContext is MusicVideoDetailsViewModel viewModel)
            {
                viewModel.UpdateMusicVideo();
            }
        }

        public void DeleteVideo(object sender, RoutedEventArgs e)
        {
            if (DataContext is MusicVideoDetailsViewModel viewModel)
            {
                viewModel.DeleteVideo();
            }
        }
    }
}