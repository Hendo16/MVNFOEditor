using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    private List<VideoCardViewModel> _songs;

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
            var path = metadata.SourceId.GetSourceIconPath();
            SourceIcons.Add(new Bitmap(path));
            if (metadata.SourceId == SearchSource.YouTubeMusic) HasYTMusic = true;
            if (metadata.SourceId == SearchSource.AppleMusic) HasAppleMusic = true;
        }
    }

    public Artist Artist => _album.Artist;

    public string Title => _album.Title;

    public string Year => _album.Year;

    public List<VideoCardViewModel> Songs
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

    public async void AddSource()
    {
        AddAlbumSourceViewModel newVM = await AddAlbumSourceViewModel.CreateNewViewModel(_album);
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(_ => newVM)
            .TryShow();
    }

    public void EditAlbum()
    {
        EditAlbumDialogViewModel editVM = new EditAlbumDialogViewModel(_album, Cover);
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(dialog => editVM)
            .TryShow();
    }

    public async void GenerateSongs()
    {
        var vidCards = new List<VideoCardViewModel>();
        var vidList = App.GetDBContext().MusicVideos.Where(mv => mv.album.Id == _album.Id).ToList();
        foreach (var single in vidList)
        {
            var newVm = new VideoCardViewModel(single);
            vidCards.Add(newVm);
            await newVm.LoadThumbnail();
        }
        Songs = new List<VideoCardViewModel>(vidCards);
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

    public void AddVideo()
    {
        NewResultDialogViewModel parentVm = NewResultDialogViewModel.CreateResultSearch(typeof(VideoResult), Artist, _album);
        //Open Dialog
        App.GetVM().GetDialogManager().CreateDialog()
            .WithViewModel(dialog => parentVm)
            .TryShow();
    }
}