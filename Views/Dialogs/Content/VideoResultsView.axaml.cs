using Avalonia.Controls;
using Avalonia.Input;
using MVNFOEditor.ViewModels;
using SukiUI.Controls;

namespace MVNFOEditor.Views
{
    public partial class VideoResultsView : UserControl
    {
        public VideoResultsView()
        {
            InitializeComponent();
        }
        private void SearchText_KeyPressUp(object sender, KeyEventArgs e)
        {
            if (DataContext is VideoResultsViewModel viewModel && sender is TextBox textBox)
            {
                viewModel.SearchText = textBox.Text;
                viewModel.FilterResults();
            }
        }
    }
}