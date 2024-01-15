using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MVNFOEditor.ViewModels;
using System.Diagnostics;

namespace MVNFOEditor;

public partial class Settings : Window
{
    private SettingsViewModel _settingsViewModel;
    public Settings()
    {
        InitializeComponent();
    }
    private void SaveNewPath(object sender, RoutedEventArgs e)
    {
        _settingsViewModel = (SettingsViewModel)DataContext;
        string newPath = PathInput.Text;
        if(newPath  != null)
        {
            Debug.WriteLine(newPath);
            Debug.WriteLine(_settingsViewModel.RootFolder);
            _settingsViewModel.RootFolder = newPath;
        }
    }
}