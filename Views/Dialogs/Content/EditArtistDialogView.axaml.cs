using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MVNFOEditor.Views;

public partial class EditArtistDialogView : UserControl
{
    public EditArtistDialogView()
    {
        InitializeComponent();
    }

    public void Next(object? source, RoutedEventArgs routedEventArgs)
    {
        Banners.Next();
    }

    public void Previous(object source, RoutedEventArgs args)
    {
        Banners.Previous();
    }
}