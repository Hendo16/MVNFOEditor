using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FFMpegCore;
using MVNFOEditor.DB;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using MVNFOEditor.Settings;
using YoutubeDLSharp.Metadata;

namespace MVNFOEditor.ViewModels;

public partial class ManualMusicVideoViewModel : ObservableObject
{
    private static ISettings _settings;
    private readonly YTDLHelper _ytDLHelper;
    private List<Album> _albums;
    private Album _currAlbum;
    [ObservableProperty] private string _grabText;
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private bool _localRadio;
    [ObservableProperty] private bool _localVisible;
    [ObservableProperty] private ObservableCollection<VideoResultViewModel> _manualItems;
    [ObservableProperty] private string _source;

    private Bitmap? _thumb;
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _coverUrl = "";
    [ObservableProperty] private string _videoPath;
    [ObservableProperty] private string _videoURL;
    [ObservableProperty] private string _year;
    private bool _ytRadio;
    private bool _ytVisible;

    public ManualMusicVideoViewModel(Artist art)
    {
        _ytDLHelper = App.GetYTDLHelper();
        _settings = App.GetSettings();
        Artist = art;
        YTRadio = true;
        GrabText = "Grab";
        if (App.GetDBContext().Album.Any(e => e.Artist == Artist))
            Albums = App.GetDBContext().Album.Where(e => e.Artist == Artist).ToList();
        ;
        ManualItems = new ObservableCollection<VideoResultViewModel>();
    }

    public ManualMusicVideoViewModel(Album album)
    {
        _ytDLHelper = App.GetYTDLHelper();
        _settings = App.GetSettings();
        Albums = new List<Album> { album };
        CurrAlbum = album;
        Artist = album.Artist;
        YTRadio = true;
        GrabText = "Grab";
        ManualItems = new ObservableCollection<VideoResultViewModel>();
    }

    public VideoData? _vidData { get; set; }
    public Artist Artist { get; set; }
    public List<Album> Albums
    {
        get => _albums;
        set
        {
            _albums = value;
            OnPropertyChanged();
        }
    }

    public Album CurrAlbum
    {
        get => _currAlbum;
        set
        {
            _currAlbum = value;
            OnPropertyChanged();
        }
    }

    public Bitmap? Thumbnail
    {
        get => _thumb;
        set
        {
            _thumb = value;
            OnPropertyChanged();
        }
    }

    public bool YTRadio
    {
        get => _ytRadio;
        set
        {
            _ytRadio = value;
            OnPropertyChanged();
            if (_ytRadio)
            {
                LocalVisible = false;
                YTVisible = true;
            }
            else
            {
                LocalVisible = true;
                YTVisible = false;
                CoverUrl = "";
            }
        }
    }

    public bool YTVisible
    {
        get => _ytVisible;
        set
        {
            _ytVisible = value;
            OnPropertyChanged();
        }
    }

    private void RemoveCard(object sender, EventArgs e)
    {
        ManualItems.Remove((VideoResultViewModel)sender);
    }

    public async void GrabYTVideo()
    {
        IsBusy = true;
        GrabText = "Searching";
        var VidData = await _ytDLHelper.GetVideoInfo(VideoURL);
        if (VidData != null)
        {
            Title = App.GetYTMusicHelper().CleanYTName(VidData.Title, Artist);
            if (VidData.ReleaseYear != null)
                Year = VidData.ReleaseYear;
            else
                Year = ((DateTime)VidData.UploadDate).Year.ToString();
            LoadThumbnail(VidData.Thumbnail);
            _vidData = VidData;
        }

        IsBusy = false;
        GrabText = "Grab";
    }

    public async void AddSingleToList()
    {
        SearchSource currSource = SearchSource.Manual;
        if (YTVisible)
        {
            currSource = SearchSource.YouTubeMusic;
        }
        string id = _vidData != null ? _vidData.ID : Guid.NewGuid().ToString("N");
        var newMvEntry = new VideoResult(Title, Year, CoverUrl, id, VideoPath, Artist, CurrAlbum, _vidData, currSource);
        var mvVM = await VideoResultViewModel.CreateViewModel(newMvEntry, CurrAlbum);
        newMvEntry.RemoveCallback += RemoveCard;
        ManualItems.Add(mvVM);
    }

    public async Task LoadThumbnail(string URL, bool edit = false)
    {
        CoverUrl = URL;
        var data = edit ? await File.ReadAllBytesAsync(URL) : await NetworkHandler.GetFileData(URL);
        if (data == null)
        {
            ToastHelper.ShowError("Cover Error", "Couldn't fetch album artwork, please check logs");
            return;
        }
        await using var imageStream = new MemoryStream(data);
        Thumbnail = Bitmap.DecodeToWidth(imageStream, 200);
    }

    public void SetLocalVideo(string path)
    {
        VideoPath = path;
        Title = Path.GetFileNameWithoutExtension(path);
        Debug.WriteLine(
            $"Move {Path.GetFileName(VideoPath)} to {_settings.RootFolder}/{Artist.Name}/{Path.GetFileName(VideoPath)}");
    }
}