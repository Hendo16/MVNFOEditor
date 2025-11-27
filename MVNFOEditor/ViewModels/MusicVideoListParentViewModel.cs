using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Helpers;

namespace MVNFOEditor.ViewModels;

public partial class MusicVideoListParentViewModel : ObservableObject
{
    [ObservableProperty] private object _currentContent;
    private MusicDBHelper _dbHelper;
    private MusicVideoDetailsViewModel _musicVideoDetails;
    private MusicVideoListViewModel _musicVideoList;

    //public MusicVideoListParentViewModel() : base("Music Videos", MaterialIconKind.AccountMusic, 1)
    public MusicVideoListParentViewModel()
    {
        var currView = new MusicVideoListViewModel();
        CurrentContent = currView;
        _musicVideoList = currView;
        _dbHelper = App.GetDBHelper();
    }
}