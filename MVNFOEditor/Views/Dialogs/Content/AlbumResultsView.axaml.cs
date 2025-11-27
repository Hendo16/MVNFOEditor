using Avalonia.Controls;
using Avalonia.Input;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views;

public partial class AlbumResultsView : UserControl
{
    public AlbumResultsView()
    {
        InitializeComponent();
    }

    private void SearchText_KeyPressUp(object sender, KeyEventArgs e)
    {
        if (DataContext is AlbumResultsViewModel viewModel && sender is TextBox textBox)
            viewModel.SearchText = textBox.Text;
    }
}