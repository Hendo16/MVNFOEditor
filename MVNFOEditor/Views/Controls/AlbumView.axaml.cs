using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using M3U8Parser.Attributes.Name;
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
        if (DataContext is AlbumViewModel viewModel && !VideoList.IsVisible) viewModel.GenerateSongs();
        VideoList.IsVisible = !VideoList.IsVisible;
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