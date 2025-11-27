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
    private readonly iTunesAPIHelper _iTunesApiHelper;
    private readonly ArtistListParentViewModel _parentVM;
    private readonly MusicDBHelper DBHelper;
    private readonly ISukiToastManager ToastManager;
    private readonly YTMusicHelper ytMusicHelper;
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
        ytMusicHelper = App.GetYTMusicHelper();
        _iTunesApiHelper = App.GetiTunesHelper();
        _parentVM = App.GetVM().GetParentView();
        _parentVM.SetDetailsVM(this);
        ToastManager = App.GetVM().GetToastManager();
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
            .WithViewModel(dialog => parentVm)
            .TryShow();
    }

    private void AddAlbum(SearchSource source)
    {
        /*
        var albumResults = await Artist.GetAlbums(source);
        if (albumResults == null || albumResults.Count == 0)
        {
            var manualVM = new ManualAlbumViewModel(Artist);
            var newAlbumVM = new NewAlbumDialogViewModel(manualVM, Artist);
            ToastManager.CreateToast()
                .WithTitle($"No {source.ToString()} Albums Available")
                .WithContent("Please provide videos manually")
                .OfType(NotificationType.Warning)
                .Queue();
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => newAlbumVM)
                .TryShow();
            return;
        }
        */
        //ObservableCollection<AlbumResultViewModel> results = new ObservableCollection<AlbumResultViewModel>(albumResults.ConvertAll(AlbumResultToVM));
        //AlbumResultsViewModel resultsVM = await AlbumResultsViewModel.CreateViewModel(Artist, source);
        //resultsVM.LoadCovers();
        //NewAlbumDialogViewModel parentVM = new NewAlbumDialogViewModel(resultsVM, Artist, source);
        NewResultDialogViewModel parentVm = NewResultDialogViewModel.CreateResultSearch(typeof(AlbumResult), Artist, source:source);
        
        parentVm.ClosePageEvent += ReturnToPreviousView;
        //Open Dialog
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(dialog => parentVm)
            .TryShow();
    }

    public async void AddYTMVideo()
    {
        NewResultDialogViewModel parentVm = NewResultDialogViewModel.CreateResultSearch(typeof(VideoResult), Artist, source:SearchSource.YouTubeMusic);
        parentVm.ClosePageEvent += ReturnToPreviousView;
        //Open Dialog
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(dialog => parentVm)
            .TryShow();
        /*
        BusyText = "Searching Youtube Music...";
        IsBusy = true;
        var artistMetadata = _artist.GetArtistMetadata(SearchSource.YouTubeMusic);
        var videos = await _artist.GetVideos(SearchSource.YouTubeMusic);
        //Sometimes artists won't have any videos listed, so we need to handle this
        if (videos == null)
        {
            IsBusy = false;
            App.GetVM().GetDialogManager().CreateDialog()
                .WithContent("No Videos Available")
                .Dismiss().ByClickingBackground()
                .TryShow();
            return;
        }

        var results = await ytMusicHelper.GenerateVideoResultList(videos, _artist);
        if (results.Count == 0)
        {
            IsBusy = false;
            App.GetVM().GetDialogManager().CreateDialog()
                .WithContent("No Videos Available")
                .Dismiss().ByClickingBackground()
                .TryShow();
        }
        else
        {
            var resultsVM = new VideoResultsViewModel(results);
            var parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);
            parentVM.RefreshAlbumEvent += RefreshDetailsView;
            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.ProgressStarted += parentVM.SaveYTMVideo;
            }

            IsBusy = false;
            //Open Dialog
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => parentVM)
                .Dismiss().ByClickingBackground()
                .TryShow();
        }
        */
    }

    public async void AddAppleMusicVideo()
    {
        NewResultDialogViewModel parentVm = NewResultDialogViewModel.CreateResultSearch(typeof(VideoResult), Artist, source:SearchSource.AppleMusic);
        parentVm.ClosePageEvent += ReturnToPreviousView;
        //Open Dialog
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(dialog => parentVm)
            .TryShow();
        /*
        BusyText = "Searching Apple Music...";
        IsBusy = true;
        var artistMetadata = _artist.GetArtistMetadata(SearchSource.AppleMusic);
        var artistID = artistMetadata.BrowseId;
        var videoSearch = await _iTunesApiHelper.GetVideosByArtistId(artistID);
        //Sometimes artists won't have any videos listed, so we need to handle this
        if (videoSearch == null)
        {
            IsBusy = false;
            App.GetVM().GetDialogManager().CreateDialog()
                .WithContent("No Videos Available")
                .Dismiss().ByClickingBackground()
                .TryShow();
            return;
        }

        var results = await _iTunesApiHelper.GenerateVideoResultList(videoSearch, _artist);
        if (results.Count == 0)
        {
            IsBusy = false;
            App.GetVM().GetDialogManager().CreateDialog()
                .WithContent("No Videos Available")
                .Dismiss().ByClickingBackground()
                .TryShow();
        }
        else
        {
            var resultsVM = new VideoResultsViewModel(results);
            var parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);
            parentVM.RefreshAlbumEvent += RefreshDetailsView;
            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.ProgressStarted += parentVM.SaveYTMVideo;
            }

            IsBusy = false;
            //Open Dialog
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => parentVM)
                .Dismiss().ByClickingBackground()
                .TryShow();
        }
        */
    }

    private async void RefreshDetailsView(object? sender, bool e)
    {
        await LoadAlbums();
    }

    public async void AddManualVideo()
    {/*
        var newVM = new ManualMusicVideoViewModel(_artist);
        var parentVM = new AddMusicVideoParentViewModel(newVM);
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(dialog => parentVM)
            .Dismiss().ByClickingBackground()
            .TryShow();
            */
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


    public void ReturnToPreviousView(object? sender, bool t)
    {
        //_parentVM.BackToList(true);
        LoadAlbums();
    }
}