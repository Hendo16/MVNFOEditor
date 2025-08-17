using Avalonia.Controls;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class NewAlbumDialogView : UserControl
    {
        public NewAlbumDialogView()
        {
            InitializeComponent();
        }

        private void ManualVideoClicked(object? sender, RoutedEventArgs e)
        {
            if (DataContext is NewAlbumDialogViewModel viewModel &&
                (bool)(sender as RadioButton)?.IsChecked)
            {
                viewModel.ManualChecked();
            }
        }

        private void YTMusicClicked(object? sender, RoutedEventArgs e)
        {
            if (DataContext is NewAlbumDialogViewModel viewModel &&
                (bool)(sender as RadioButton)?.IsChecked)
            {
                viewModel.YouTubeChecked();
            }
        }

        private void AppleMusicClicked(object? sender, RoutedEventArgs e)
        {
            if (DataContext is NewAlbumDialogViewModel viewModel &&
                (bool)(sender as RadioButton)?.IsChecked)
            {
                viewModel.AppleMusicChecked();
            }
        }

        private void NavigateBack(object? sender, RoutedEventArgs e)
        {
            if (DataContext is NewAlbumDialogViewModel viewModel)
            {
                viewModel.BackTrigger();
            }
        }

        private void NavigateForward(object? sender, RoutedEventArgs e)
        {
            if (DataContext is NewAlbumDialogViewModel viewModel)
            {
                viewModel.NextTrigger();
            }
        }
    }
}