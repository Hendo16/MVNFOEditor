using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views;

public partial class AlbumView : UserControl
{
    public AlbumView()
    {
        InitializeComponent();
    }

    public void TriggerSongList(object sender, PointerPressedEventArgs e)
    {
        if (DataContext is AlbumViewModel viewModel && !songList.IsVisible) viewModel.GenerateSongs();
        songList.IsVisible = !songList.IsVisible;
    }

    public void SongClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && DataContext is AlbumViewModel viewModel)
        {
            var songID = (int)button.CommandParameter;
            viewModel.HandleSongClick(songID);
        }
    }
}