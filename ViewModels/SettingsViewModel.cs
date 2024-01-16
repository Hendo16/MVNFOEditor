
using MVNFOEditor.DB;
using MVNFOEditor.Models;

namespace MVNFOEditor.ViewModels;
public partial class SettingsViewModel : ViewModelBase
{
    private MainViewModel _mainViewModel;
    private SettingsData _settingsData;
    public MusicDbContext MVDBContext { get; set; }
    public SettingsData SettingsData
    {
        get => _settingsData;
        set
        {
            if (_settingsData != value)
            {
                _settingsData = value;

                MVDBContext.SettingsData.Add(value);
                MVDBContext.SaveChanges();

                // Pass the updated RootFolder value to MainViewModel
                _mainViewModel.RootFolder = value.RootFolder;
            }
        }
    }
    
    public SettingsViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        MVDBContext = _mainViewModel.MVDBContext;
    }
}