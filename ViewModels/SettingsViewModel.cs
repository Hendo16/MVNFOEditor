using System.Collections.Generic;
using MVNFOEditor.Models;

namespace MVNFOEditor.ViewModels;
public partial class SettingsViewModel : ViewModelBase
{
    private MainViewModel _mainViewModel;

    public SettingsViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    private string _rootFolder;

    public string RootFolder
    {
        get => _rootFolder;
        set
        {
            if (_rootFolder != value)
            {
                _rootFolder = value;
                OnPropertyChanged(nameof(RootFolder));
                // Pass the updated RootFolder value to MainViewModel
                _mainViewModel.RootFolder = value;
            }
        }
    }
}
