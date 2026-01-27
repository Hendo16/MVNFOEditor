using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FFMpegCore;
using log4net;
using MVNFOEditor.DB;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using MVNFOEditor.Settings;

namespace MVNFOEditor.ViewModels;

public partial class VideoResultViewModel : ObservableObject
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(VideoResultViewModel));
    private readonly Album? _album;
    private readonly VideoResult _result;

    [ObservableProperty] private string _borderColor;
    [ObservableProperty] private string _downloadBtnText;
    [ObservableProperty] private bool _downloadEnabled;
    [ObservableProperty] private Bitmap? _thumbnail;

    private VideoResultViewModel(VideoResult result, Album? album = null)
    {
        _result = result;
        if (album != null)
        {
            _album = album;
        }
    }
    
    public static async Task<VideoResultViewModel> CreateViewModel(VideoResult result, Album? album = null)
    {
        var newVm = new VideoResultViewModel(result, album);
        await newVm.LoadThumbnail();
        return newVm;
    }

    public string Title => _result.Name;
    public Artist Artist => _result.Artist;
    public string Duration => _result.Duration;
    public string TopRes => _result.TopRes;

    public event EventHandler<VideoResultViewModel> ProgressStarted;

    public VideoResult HandleDownload()
    {
        BorderColor = "Green";
        DownloadEnabled = false;
        DownloadBtnText = "Downloaded";
        return _result;
    }

    public VideoResult GetResult()
    {
        return _result;
    }

    public async Task LoadThumbnail()
    {
        await using (var imageStream = await _result.LoadCoverBitmapAsync())
        {
            Thumbnail = new Bitmap(imageStream);
        }
    }

    public async Task SaveThumbnailAsync(string folderPath)
    {
        var bitmap = Thumbnail;
        await Task.Run(() =>
        {
            using (var fs = _result.SaveThumbnailBitmapStream(folderPath, _result.Source.ToString()))
            {
                bitmap.Save(fs);
            }
        });
    }

    public async Task<int> GenerateNFO(string filePath)
    {
        switch (_result.Source)
        {
            case SearchSource.YouTubeMusic:
                return await GenerateNFO_YTM(filePath);
            case SearchSource.AppleMusic:
                return await GenerateNFO_AM(filePath);
            case SearchSource.Manual:
                return await GenerateNFO_Manual(filePath);
        }

        return 0;
    }

    private async Task<int> GenerateNFO_Manual(string filePath)
    {
        var newMV = new MusicVideo();
        newMV.title = _result.Name;
        newMV.artist = _result.Artist;
        if (_album != null)
        {
            newMV.album = _album;
            newMV.year = _album.Year;
        }
        else if (_result.Year != null)
        {
            newMV.year = _result.Year;
        }

        newMV.source = SearchSource.Manual;
        newMV.nfoPath = $"{App.GetSettings().RootFolder}/{newMV.artist.Name}/{newMV.title}.nfo";

        var newImagePath = $"{App.GetSettings().RootFolder}/{newMV.artist.Name}/{newMV.title}.png";
        FFMpeg.Snapshot(filePath, newImagePath, new Size(400, 225),
            TimeSpan.FromSeconds(App.GetSettings().ScreenshotSecond));
        newMV.thumb = $"{newMV.title}.png";

        newMV.vidPath = filePath;
        
        newMV.SaveToNFO();
        App.GetDBContext().MusicVideos.Add(newMV);
        return await App.GetDBContext().SaveChangesAsync();
    }

    private async Task<int> GenerateNFO_AM(string filePath)
    {
        var newMV = new MusicVideo();
        newMV.title = _result.Name;
        newMV.videoID = _result.SourceId;
        newMV.artist = _result.Artist;
        if (_album != null)
        {
            newMV.album = _album;
            newMV.year = _album.Year;
        }
        else
        {
            newMV.year = _result.Year;
        }

        newMV.source = SearchSource.AppleMusic;
        newMV.nfoPath = $"{App.GetSettings().RootFolder}/{newMV.artist.Name}/{newMV.title}-{newMV.source.ToString()}.nfo";

        await SaveThumbnailAsync($"{App.GetSettings().RootFolder}/{newMV.artist.Name}");
        newMV.thumb = $"{newMV.title}-{newMV.source.ToString()}.jpg";

        newMV.vidPath = filePath;

        //Handle Genres
        var baseGenre = App.GetDBContext().AppleMusicVideoMetadata.First(am => am.id == int.Parse(_result.SourceId)).genre;
        if (!App.GetDBContext().Genres.Any(g => g.Name == baseGenre))
        {
            var newGenre = new Genre(baseGenre, newMV);
            App.GetDBContext().Genres.Add(newGenre);
        }

        newMV.SaveToNFO();
        App.GetDBContext().MusicVideos.Add(newMV);
        return await App.GetDBContext().SaveChangesAsync();
    }

    private async Task<int> GenerateNFO_YTM(string filePath)
    {
        var newMV = new MusicVideo();
        var vidData =
            await App.GetYTDLHelper().GetVideoInfo($"https://www.youtube.com/watch?v={_result.SourceId}");
        newMV.title = _result.Name;
        newMV.videoID = _result.SourceId;
        newMV.artist = _result.Artist;
        if (_album != null)
        {
            newMV.album = _album;
            newMV.year = _album.Year;
        }
        else
        {
            newMV.album = null;
            if (vidData.ReleaseYear != null)
                newMV.year = vidData.ReleaseYear;
            else
                newMV.year = ((DateTime)vidData.UploadDate).Year.ToString();
        }

        newMV.source = SearchSource.YouTubeMusic;
        newMV.nfoPath = $"{App.GetSettings().RootFolder}/{newMV.artist.Name}/{newMV.title}-{newMV.source.ToString()}.nfo";

        await SaveThumbnailAsync($"{App.GetSettings().RootFolder}/{newMV.artist.Name}");
        newMV.thumb = $"{newMV.title}-{newMV.source.ToString()}.jpg";

        newMV.vidPath = filePath.Replace(_result.Name, $"{_result.Name}-{SearchSource.YouTubeMusic.ToString()}");

        newMV.SaveToNFO();
        App.GetDBContext().MusicVideos.Add(newMV);
        return await App.GetDBContext().SaveChangesAsync();
    }

    protected virtual void OnProgressTrigger()
    {
        ProgressStarted?.Invoke(this, this);
    }
}