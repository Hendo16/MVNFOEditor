using Avalonia.Controls;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views;

public partial class AddAlbumSourceView : UserControl
{
    public AddAlbumSourceView()
    {
        InitializeComponent();
    }

    private void YTMusicClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AddAlbumSourceViewModel viewModel &&
            (bool)(sender as RadioButton)?.IsChecked)
            viewModel.YouTubeChecked();
    }

    private void AppleMusicClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AddAlbumSourceViewModel viewModel &&
            (bool)(sender as RadioButton)?.IsChecked)
            viewModel.AppleMusicChecked();
    }
}