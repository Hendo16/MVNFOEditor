using Avalonia.Controls;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class NewArtistDialogView : UserControl
    {
        public NewArtistDialogView()
        {
            InitializeComponent();
        }
        private void ModeToggleChange(object? sender, RoutedEventArgs e)
        {
            if (DataContext is NewArtistDialogViewModel viewModel &&
                sender is ToggleSwitch _switch)
            {
                viewModel.HandleChangedMode(_switch.IsChecked);
            }
        }

        private void NavigateBack(object? sender, RoutedEventArgs e)
        {
            if (DataContext is NewArtistDialogViewModel viewModel)
            {
                viewModel.BackTrigger();
            }
        }

        private void NavigateForward(object? sender, RoutedEventArgs e)
        {
            if (DataContext is NewArtistDialogViewModel viewModel)
            {
                viewModel.NextTrigger();
            }
        }

        private void SkipAlbum(object? sender, RoutedEventArgs e)
        {
            if (DataContext is NewArtistDialogViewModel viewModel)
            {
                viewModel.HandleSkip();
            }
        }

    }
}
