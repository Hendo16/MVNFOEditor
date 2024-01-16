using Avalonia.Controls;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    private void OpenSettings(object sender, RoutedEventArgs e)
    {
        var mainViewContext = (MainViewModel)this.DataContext;
        var settingsViewModel = new SettingsViewModel(mainViewContext);
        Settings setPanel = new Settings
        {
            DataContext = settingsViewModel
        };

        setPanel.Show();
    }
}
