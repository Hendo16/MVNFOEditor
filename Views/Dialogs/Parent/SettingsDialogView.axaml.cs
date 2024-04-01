using Avalonia.Controls;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class SettingsDialogView : UserControl
    {
        public SettingsDialogView()
        {
            InitializeComponent();
        }
        public void Previous(object source, RoutedEventArgs args)
        {
            settingsPages.Previous();
            if (DataContext is SettingsDialogViewModel viewModel)
            {
                viewModel.HandleBackwards();
            }
        }

        public void Next(object source, RoutedEventArgs args)
        {
            settingsPages.Next();
            if (DataContext is SettingsDialogViewModel viewModel)
            {
                viewModel.HandleForward();
            }
        }
    }
}