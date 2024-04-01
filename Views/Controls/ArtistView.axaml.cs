using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class ArtistView : UserControl
    {
        public ArtistView()
        {
            InitializeComponent();
        }

        public void ArtistClick(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed && DataContext is ArtistViewModel viewModel)
            {
                viewModel.HandleArtistClick();
            }
        }
    }
}
