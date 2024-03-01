using Avalonia.Controls;
using Avalonia.Input;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class SyncDialogView : UserControl
    {
        public SyncDialogView()
        {
            InitializeComponent();
        }
        private void SearchText_KeyPressUp(object sender, KeyEventArgs e)
        {
            if (DataContext is SyncDialogViewModel viewModel && sender is TextBox textBox)
            {
                viewModel.SearchText = textBox.Text;
            }
        }
    }
}