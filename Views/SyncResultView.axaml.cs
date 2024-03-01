using Avalonia.Controls;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;


namespace MVNFOEditor.Views
{
    public partial class SyncResultView : UserControl
    {
        public SyncResultView()
        {
            InitializeComponent();
        }

        public void OpenResult(object sender, RoutedEventArgs e)
        {
            if (DataContext is SyncResultViewModel viewModel)
            {
                viewModel.OpenResult();
            }
        }
    }
}