using Avalonia.Controls;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views
{
    public partial class AddMusicVideoParentView : UserControl
    {
        public AddMusicVideoParentView()
        {
            InitializeComponent();
        }
        private void ModeToggleChange(object? sender, RoutedEventArgs e)
        {
            if (DataContext is AddMusicVideoParentViewModel viewModel &&
                sender is ToggleSwitch _switch)
            {
                viewModel.HandleChangedMode(_switch.IsChecked);
            }
        }
    }
}