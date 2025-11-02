using Avalonia.Controls;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views;

public partial class NewArtistDialogView : UserControl
{
    public NewArtistDialogView()
    {
        InitializeComponent();
    }

    private void ManualVideoClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is NewArtistDialogViewModel viewModel &&
            (bool)(sender as RadioButton)?.IsChecked)
            viewModel.ManualChecked();
    }

    private void YTMusicClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is NewArtistDialogViewModel viewModel &&
            (bool)(sender as RadioButton)?.IsChecked)
            viewModel.YouTubeChecked();
    }

    private void AppleMusicClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is NewArtistDialogViewModel viewModel &&
            (bool)(sender as RadioButton)?.IsChecked)
            viewModel.AppleMusicChecked();
    }
}