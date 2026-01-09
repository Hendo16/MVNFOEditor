using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Factories;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using Org.BouncyCastle.Asn1.X509.Qualified;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using YtMusicNet.Models;
using Artist = MVNFOEditor.Models.Artist;

namespace MVNFOEditor.ViewModels;

public partial class ArtistDetailsViewModel : ObservableValidator
{
    private readonly ArtistListParentViewModel _parentVM;
    private readonly MusicDBHelper DBHelper;
    [ObservableProperty] private ObservableCollection<AlbumViewModel> _albumCards;
    [ObservableProperty] private Artist _artist;
    private string _artistName;
    private ArtistDetailsBannerViewModel _bannerVM;
    [ObservableProperty] private string _busyText;
    [ObservableProperty] private bool _hasAlbums;
    [ObservableProperty] private bool _hasSingles;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private ObservableCollection<VideoCardViewModel> _singleCards;
    [ObservableProperty] private SearchSource _source;

    public ArtistDetailsViewModel()
    {
        DBHelper = App.GetDBHelper();
        _parentVM = App.GetVM().GetParentView();
        _parentVM.SetDetailsVM(this);
    }

    public string ArtistName
    {
        set
        {
            _artistName = value;
            OnPropertyChanged();
        }
    }


    public ArtistDetailsBannerViewModel BannerVM
    {
        get => _bannerVM;
        set
        {
            _bannerVM = value;
            OnPropertyChanged();
        }
    }

    public async Task LoadBanner()
    {
        if (_artist.LargeBannerURL != null)
            await using (var imageStream = await _artist.LoadLargeBannerBitmapAsync())
            {
                BannerVM = new ArtistDetailsBannerViewModel(Bitmap.DecodeToHeight(imageStream, 800), this);
            }
        else
            await using (var imageStream = await _artist.LoadLocalLargeBannerBitmapAsync())
            {
                BannerVM = new ArtistDetailsBannerViewModel(Bitmap.DecodeToHeight(imageStream, 800), this);
            }
    }

    public void AddAlbum()
    {
        AddResult(typeof(AlbumResult));
    }

    public void AddVideo()
    {
        AddResult(typeof(VideoResult));
    }

    public void AddResult(Type resultType)
    {
        SearchSource currSource = (Artist.GetArtistMetadata()).SourceId;
        switch (resultType.Name)
        {
            case nameof(AlbumResult):
                AddAlbum(currSource);
                break;
            case nameof(VideoResult):
                AddVideo(currSource);
                break;
        }
    }

    private void AddVideo(SearchSource source)
    {
        NewResultDialogViewModel parentVm = NewResultDialogViewModel.CreateResultSearch(typeof(VideoResult), Artist, source:source);
        
        parentVm.ClosePageEvent += ReturnToPreviousView;
        //Open Dialog
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(_ => parentVm)
            .TryShow();
    }

    private void AddAlbum(SearchSource source)
    {
        NewResultDialogViewModel parentVm = NewResultDialogViewModel.CreateResultSearch(typeof(AlbumResult), Artist, source:source);
        
        parentVm.ClosePageEvent += ReturnToPreviousView;
        //Open Dialog
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(_ => parentVm)
            .TryShow();
    }

    private async void RefreshDetailsView(object? sender, bool e)
    {
        await LoadAlbums();
    }

    public async void SetArtist(Artist artist)
    {
        _artist = artist;
        //TODO: When adding album, how do we select a primary source??? Just assume first for now
        var artistMetadata = _artist.GetArtistMetadata();
        Source = artistMetadata.SourceId;
        ArtistName = _artist.Name;
        await LoadAlbums();
        await LoadBanner();
    }

    public async Task<bool> LoadAlbums()
    {
        BusyText = "Loading Albums...";
        IsBusy = true;
        AlbumCards = await DBHelper.GenerateAlbums(Artist);
        SingleCards = await DBHelper.GenerateSingles(Artist);
        for (var i = 0; i < AlbumCards.Count; i++) AlbumCards[i].SyncStarted += LoadingSync;
        HasAlbums = AlbumCards.Count > 0;
        HasSingles = SingleCards.Count > 0;
        IsBusy = false;
        return true;
    }

    private void LoadingSync(object sender, bool isSyncTriggered)
    {
        BusyText = "Getting videos...";
        IsBusy = isSyncTriggered;
    }

    public void ClearImages()
    {
        for (var i = 0; i < AlbumCards.Count; i++)
            if (AlbumCards[i].Cover != null)
                AlbumCards[i].Cover.Dispose();

        for (var i = 0; i < SingleCards.Count; i++)
            if (SingleCards[i].Thumbnail != null)
                SingleCards[i].Thumbnail.Dispose();

        AlbumCards.Clear();
        SingleCards.Clear();
        BannerVM.ArtistBanner.Dispose();
    }


    private async void ReturnToPreviousView(object? sender, bool t)
    {
        await LoadAlbums();
    }
}