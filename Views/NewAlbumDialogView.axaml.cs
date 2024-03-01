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

        private void ModeToggleChange(object? sender, RoutedEventArgs e)
        {
            if (DataContext is NewAlbumDialogViewModel viewModel &&
                sender is ToggleSwitch _switch)
            {
                viewModel.HandleChangedMode(_switch.IsChecked);
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