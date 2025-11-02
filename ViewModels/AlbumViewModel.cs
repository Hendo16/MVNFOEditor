using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels;

public partial class AlbumViewModel : ObservableObject
{
    private readonly Album _album;
    private readonly iTunesAPIHelper _iTunesApiHelper;
    private readonly ArtistListParentViewModel _parentVM;

    private readonly ISukiToastManager ToastManager;
    private readonly YTMusicHelper ytMusicHelper;

    private Bitmap? _cover;
    [ObservableProperty] private bool _hasAppleMusic;
    [ObservableProperty] private bool _hasYTMusic;

    private List<MusicVideo> _songs;

    [ObservableProperty] private bool _sourceAvailable;

    //Show/Hide Source Options
    [ObservableProperty] private List<Bitmap> _sourceIcons = new();

    public AlbumViewModel(Album album)
    {
        _parentVM = App.GetVM().GetParentView();
        ToastManager = App.GetVM().GetToastManager();
        _album = album;
        ytMusicHelper = App.GetYTMusicHelper();
        _iTunesApiHelper = App.GetiTunesHelper();
        //Bit hacky, would love something more dynamic but we're likely only ever going to have 2 sources anyway...
        SourceAvailable = album.Artist.Metadata.Count > 1 &&
                          album.Metadata.Count(am =>
                              am.SourceId is SearchSource.YouTubeMusic or SearchSource.AppleMusic) == 1;
        foreach (var metadata in album.Metadata)
        {
            var path = metadata.SourceIconPath;
            SourceIcons.Add(new Bitmap(path));
            if (metadata.SourceId == SearchSource.YouTubeMusic) HasYTMusic = true;
            if (metadata.SourceId == SearchSource.AppleMusic) HasAppleMusic = true;
        }
    }

    public Artist Artist => _album.Artist;

    public string Title => _album.Title;

    public string Year => _album.Year;

    public List<MusicVideo> Songs
    {
        get => _songs;
        set
        {
            _songs = value;
            OnPropertyChanged();
        }
    }

    public Bitmap? Cover
    {
        get => _cover;
        set
        {
            _cover = value;
            OnPropertyChanged();
        }
    }

    public event EventHandler<bool> SyncStarted;

    public async Task LoadCover()
    {
        await using (var imageStream = await _album.LoadCoverBitmapAsync())
        {
            Cover = Bitmap.DecodeToWidth(imageStream, 400);
        }
    }

    public async Task SaveCoverAsync()
    {
        var bitmap = Cover;
        await Task.Run(() =>
        {
            using (var fs = _album.SaveCoverBitmapStream())
            {
                bitmap.Save(fs);
            }
        });
    }

    public void AddSource()
    {
        var newVM = new AddAlbumSourceViewModel(_album);
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(_ => newVM)
            .TryShow();
    }

    public void EditAlbum()
    {
        var editVM = new EditAlbumDialogViewModel(_album, Cover);
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(dialog => editVM)
            .TryShow();
    }

    public void GenerateSongs()
    {
        var dbContext = App.GetDBContext();
        Songs = dbContext.MusicVideos.Where(mv => mv.album.Id == _album.Id).ToList();
    }

    public async void HandleSongClick(int songID)
    {
        var dbContext = App.GetDBContext();
        var songObj = dbContext.MusicVideos.First(mv => mv.Id == songID);
        if (File.Exists(songObj.vidPath))
        {
            var mvVM = new MusicVideoDetailsViewModel();
            mvVM.SetVideo(songObj);
            mvVM.AnalyzeVideo();
            await mvVM.LoadThumbnail();
            _parentVM.CurrentContent = mvVM;
        }
        else
        {
            //Handle deleted video
            ToastHelper.ShowError("Music Video Missing", $"{songObj.title} has been deleted from folder - removing from db...");
            App.GetDBContext().MusicVideos.Remove(songObj);
            await App.GetDBContext().SaveChangesAsync();
            _parentVM.RefreshDetails();
        }
    }

    public async void AddAppleMusicVideo()
    {
        NewResultDialogViewModel parentVm = NewResultDialogViewModel.CreateResultSearch(typeof(VideoResult), Artist, _album, source:SearchSource.AppleMusic);
        //Open Dialog
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(dialog => parentVm)
            .TryShow();
        /*
        OnSyncTrigger(true);
        var results = await _iTunesApiHelper.GenerateVideoResultList(_album);
        //Sometimes artists won't have any videos listed, so we need to handle this
        if (results.Count == 0)
        {
            OnSyncTrigger(false);
            App.GetVM().GetDialogManager().CreateDialog()
                .WithContent("No Videos Available")
                .Dismiss().ByClickingBackground()
                .TryShow();
            return;
        }

        var resultsVM = new VideoResultsViewModel(results);
        var artistMetadata = Artist.GetArtistMetadata(SearchSource.AppleMusic);
        var parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);
        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            result.ProgressStarted += parentVM.SaveAMVideo;
        }

        resultsVM.BusyText = "Searching Apple Music...";
        OnSyncTrigger(false);
        //Open Dialog
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(dialog => parentVM)
            //.OnDismissed(e => parentVM.HandleDismiss())
            .TryShow();
            */
    }

    public void AddVideo()
    {        
        NewResultDialogViewModel parentVm = NewResultDialogViewModel.CreateResultSearch(typeof(VideoResult), Artist, _album);
        //Open Dialog
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(dialog => parentVm)
            .TryShow();

    }

    public async void AddYTMVideo()
    {
        NewResultDialogViewModel parentVm = NewResultDialogViewModel.CreateResultSearch(typeof(VideoResult), Artist, _album, source:SearchSource.YouTubeMusic);
        //Open Dialog
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(dialog => parentVm)
            .TryShow();
        /*
        OnSyncTrigger(true);
        var artistMetadata = Artist.GetArtistMetadata(SearchSource.YouTubeMusic);
        var artistID = artistMetadata.BrowseId;
        var videos = await Artist.GetVideos(SearchSource.YouTubeMusic);
        //Sometimes artists won't have any videos listed, so we need to handle this
        if (videos == null)
        {
            OnSyncTrigger(false);
            App.GetVM().GetDialogManager().CreateDialog()
                .WithContent("No Videos Available")
                .Dismiss().ByClickingBackground()
                .TryShow();
            return;
        }

        var fullAlbum = await ytMusicHelper.GetAlbum(_album.AlbumBrowseID);
        var results = await ytMusicHelper.GenerateVideoResultList(videos, fullAlbum, _album);

        if (results.Count == 0)
        {
            OnSyncTrigger(false);
            App.GetVM().GetDialogManager().CreateDialog()
                .WithContent("No Videos Available")
                .Dismiss().ByClickingBackground()
                .TryShow();
        }
        else
        {
            var resultsVM = new VideoResultsViewModel(results);
            var parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);
            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.ProgressStarted += parentVM.SaveYTMVideo;
            }

            OnSyncTrigger(false);
            //Open Dialog
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => parentVM)
                .TryShow();
        }
        */
    }

    public void AddManualSingle()
    {
        var newVM = new ManualMusicVideoViewModel(_album);
        var parentVM = new AddMusicVideoParentViewModel(newVM);
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(dialog => parentVM)
            .Dismiss().ByClickingBackground()
            .TryShow();
    }

    protected virtual void OnSyncTrigger(bool isTaskStarted)
    {
        SyncStarted?.Invoke(this, isTaskStarted);
    }
}