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
        var mainView = this.FindControl<MainView>("MainView");
        var settingsViewModel = new SettingsViewModel((MainViewModel)mainView.DataContext);
        Settings setPanel = new Settings
        {
            DataContext = settingsViewModel
        };

        setPanel.Show();
    }
}
