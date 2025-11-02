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
    private readonly MusicDbContext _dbContext;
    private readonly YTDLHelper _ytDLHelper;
    private List<Album> _albums;
    private Album _currAlbum;
    [ObservableProperty] private string _grabText;
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private bool _localRadio;
    [ObservableProperty] private bool _localVisible;
    [ObservableProperty] private ObservableCollection<ManualMVEntryViewModel> _manualItems;
    [ObservableProperty] private string _source;

    private Bitmap? _thumb;
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _videoPath;
    [ObservableProperty] private string _videoURL;
    [ObservableProperty] private string _year;
    private bool _ytRadio;
    private bool _ytVisible;

    public ManualMusicVideoViewModel(Artist art)
    {
        _dbContext = App.GetDBContext();
        _ytDLHelper = App.GetYTDLHelper();
        _settings = App.GetSettings();
        Artist = art;
        YTRadio = true;
        GrabText = "Grab";
        if (App.GetDBContext().Album.Any(e => e.Artist == Artist))
            Albums = App.GetDBContext().Album.Where(e => e.Artist == Artist).ToList();
        ;
        ManualItems = new ObservableCollection<ManualMVEntryViewModel>();
    }

    public ManualMusicVideoViewModel(Album album)
    {
        _dbContext = App.GetDBContext();
        _ytDLHelper = App.GetYTDLHelper();
        _settings = App.GetSettings();
        Albums = new List<Album> { album };
        CurrAlbum = album;
        Artist = album.Artist;
        YTRadio = true;
        GrabText = "Grab";
        ManualItems = new ObservableCollection<ManualMVEntryViewModel>();
    }

    public VideoData? _vidData { get; set; }
    public Artist Artist { get; set; }
    public MusicVideo PreviousVideo { get; set; }

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
        ManualItems.Remove((ManualMVEntryViewModel)sender);
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

    public void AddSingleToList()
    {
        var newMVEntry = new ManualMVEntryViewModel(_title, _year, Thumbnail, _vidData.ID, CurrAlbum, _vidData);
        newMVEntry.RemoveCallback += RemoveCard;
        ManualItems.Add(newMVEntry);
    }

    public async Task LoadThumbnail(string URL, bool edit = false)
    {
        var data = edit ? await File.ReadAllBytesAsync(URL) : await App.GetHttpClient().GetByteArrayAsync(URL);
        await using (var imageStream = new MemoryStream(data))
        {
            Thumbnail = Bitmap.DecodeToWidth(imageStream, 200);
        }
    }

    public async Task SaveThumbnailAsync(string folderPath, string fileName)
    {
        var bitmap = Thumbnail;
        await Task.Run(() =>
        {
            using (var fs = SaveThumbnailBitmapStream(folderPath, fileName))
            {
                bitmap.Save(fs);
            }
        });
    }

    private Stream SaveThumbnailBitmapStream(string folderPath, string fileName)
    {
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        return File.OpenWrite(folderPath + $"/{fileName}.jpg");
    }

    public void SetLocalVideo(string path)
    {
        VideoPath = path;
        Title = Path.GetFileNameWithoutExtension(path);
        Debug.WriteLine(
            $"Move {Path.GetFileName(VideoPath)} to {_settings.RootFolder}/{Artist.Name}/{Path.GetFileName(VideoPath)}");
    }

    public void ClearData()
    {
        Title = "";
        Year = "";
        Source = "";
        VideoURL = "";
        Thumbnail = null;
    }

    public async Task<int> GenerateManualNFO(string vidPath, int currInd)
    {
        var _dbContext = App.GetDBContext();
        var newMV = new MusicVideo();
        newMV.title = ManualItems[currInd].Title;
        newMV.year = ManualItems[currInd].Year;
        newMV.artist = Artist;
        newMV.vidPath = vidPath;
        if (ManualItems[currInd].Album != null)
            newMV.album = ManualItems[currInd].Album;
        else
            newMV.album = null;

        if (ManualItems[currInd].VidData != null)
        {
            newMV.videoID = ManualItems[currInd].VidData.ID;
            newMV.source = "youtube";
            await SaveThumbnailAsync($"{_settings.RootFolder}/{newMV.artist.Name}", newMV.title);
            newMV.thumb = $"{newMV.title}.jpg";
        }
        else
        {
            newMV.source = "local";
        }

        newMV.nfoPath = $"{_settings.RootFolder}/{newMV.artist.Name}/{newMV.title}.nfo";

        newMV.SaveToNFO();
        _dbContext.MusicVideos.Add(newMV);
        return await _dbContext.SaveChangesAsync();
    }

    public async Task<int> GenerateManualNFO(string vidPath, bool edit)
    {
        MusicVideo newMV;
        if (edit)
        {
            //Clear out old data
            File.Delete(PreviousVideo.vidPath);
            File.Delete($"{_settings.RootFolder}\\{Artist.Name}\\{PreviousVideo.thumb}");
            File.Delete($"{_settings.RootFolder}\\{Artist.Name}\\{PreviousVideo.nfoPath}");
            //Retain old video entry
            newMV = PreviousVideo;
        }
        else
        {
            newMV = new MusicVideo();
        }

        newMV.title = Title;
        newMV.year = Year;
        newMV.artist = Artist;
        newMV.vidPath = vidPath;
        if (CurrAlbum != null)
            newMV.album = CurrAlbum;
        else
            newMV.album = null;

        if (_vidData != null)
        {
            newMV.videoID = _vidData.ID;
            newMV.source = "youtube";
            await SaveThumbnailAsync($"{_settings.RootFolder}/{newMV.artist.Name}", newMV.title);
            newMV.thumb = $"{newMV.title}.jpg";
        }
        else
        {
            newMV.source = "local";
            var newImagePath = $"{_settings.RootFolder}/{newMV.artist.Name}/{newMV.title}.png";
            FFMpeg.Snapshot(vidPath, newImagePath, new Size(400, 225),
                TimeSpan.FromSeconds(_settings.ScreenshotSecond));
            newMV.thumb = $"{newMV.title}.png";
        }

        newMV.nfoPath = $"{_settings.RootFolder}/{newMV.artist.Name}/{newMV.title}.nfo";
        newMV.SaveToNFO();
        _dbContext.MusicVideos.Add(newMV);
        return await _dbContext.SaveChangesAsync();
    }
}