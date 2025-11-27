using Material.Icons;
using MVNFOEditor.Features;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;

namespace MVNFOEditor.ViewModels;

public class ArtistListParentViewModel : PageBase
{
    private readonly ArtistListViewModel _listVM;
    private object _currentContent;
    private MusicDBHelper _dbHelper;
    private ArtistDetailsViewModel _detailsVM;

    public ArtistListParentViewModel() : base("Artist List", MaterialIconKind.AccountMusic, 1)
    {
        var currView = new ArtistListViewModel();
        CurrentContent = currView;
        _listVM = currView;
        _dbHelper = App.GetDBHelper();
    }

    public object CurrentContent
    {
        get => _currentContent;
        set
        {
            _currentContent = value;
            OnPropertyChanged();
        }
    }

    public void SetDetailsVM(ArtistDetailsViewModel vm)
    {
        _detailsVM = vm;
    }

    public async void RefreshArtistList()
    {
        _listVM.RefreshArtists();
    }

    public async void RefreshDetails()
    {
        await _detailsVM.LoadAlbums();
    }

    public void AddArtistToList(Artist newArtist)
    {
        _listVM.AddArtistToList(newArtist);
    }

    public async void BackToDetails(bool reload = false)
    {
        if (reload) await _detailsVM.LoadAlbums();
        CurrentContent = _detailsVM;
    }

    public void BackToList(bool reload = false)
    {
        if (reload) _listVM.LoadArtists();

        if (_detailsVM != null) _detailsVM.ClearImages();
        CurrentContent = _listVM;
    }
}