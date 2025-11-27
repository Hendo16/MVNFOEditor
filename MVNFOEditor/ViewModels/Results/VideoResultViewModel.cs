using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using MVNFOEditor.DB;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using MVNFOEditor.Settings;

namespace MVNFOEditor.ViewModels;

public partial class VideoResultViewModel : ObservableObject
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(VideoResultViewModel));
    private static ISettings _settings;
    private readonly Album? _album;
    private readonly MusicDbContext _dbContext;
    private readonly VideoResult _result;
    private readonly YTDLHelper _ytDLHelper;

    [ObservableProperty] private string _borderColor;
    [ObservableProperty] private string _downloadBtnText;
    [ObservableProperty] private bool _downloadEnabled;
    private iTunesAPIHelper _iTunesApiHelper;
    private YTMusicHelper _musicHelper;
    [ObservableProperty] private Bitmap? _thumbnail;

    public VideoResultViewModel(VideoResult result)
    {
        _result = result;
        _musicHelper = App.GetYTMusicHelper();
        _ytDLHelper = App.GetYTDLHelper();
        _iTunesApiHelper = App.GetiTunesHelper();
        _dbContext = App.GetDBContext();
        _settings = App.GetSettings();
        _album = null;
    }

    public VideoResultViewModel(VideoResult result, Album album)
    {
        _result = result;
        _musicHelper = App.GetYTMusicHelper();
        _ytDLHelper = App.GetYTDLHelper();
        _iTunesApiHelper = App.GetiTunesHelper();
        _dbContext = App.GetDBContext();
        _settings = App.GetSettings();
        _album = album;
    }

    public string Title => _result.Name;
    public Artist Artist => _result.Artist;
    public string Duration => _result.Duration;
    public string TopRes => _result.TopRes;
    public string VideoURL => _result.VideoURL;

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
            using (var fs = _result.SaveThumbnailBitmapStream(folderPath))
            {
                bitmap.Save(fs);
            }
        });
    }

    public async Task<int> GenerateNFO(string filePath, SearchSource source)
    {
        switch (source)
        {
            case SearchSource.YouTubeMusic:
                return await GenerateNFO_YTM(filePath);
            case SearchSource.AppleMusic:
                return await GenerateNFO_AM(filePath);
        }

        return 0;
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

        newMV.source = "Apple Music";
        newMV.nfoPath = $"{_settings.RootFolder}/{newMV.artist.Name}/{newMV.title}.nfo";

        await SaveThumbnailAsync($"{_settings.RootFolder}/{newMV.artist.Name}");
        newMV.thumb = $"{newMV.title}.jpg";

        newMV.vidPath = filePath;

        //Handle Genres
        var baseGenre = _dbContext.AppleMusicVideoMetadata.First(am => am.id == int.Parse(_result.SourceId)).genre;
        if (!_dbContext.Genres.Any(g => g.Name == baseGenre))
        {
            var newGenre = new Genre(baseGenre, newMV);
            _dbContext.Genres.Add(newGenre);
        }

        newMV.SaveToNFO();
        _dbContext.MusicVideos.Add(newMV);
        return await _dbContext.SaveChangesAsync();
    }

    private async Task<int> GenerateNFO_YTM(string filePath)
    {
        var newMV = new MusicVideo();
        var vidData =
            await _ytDLHelper.GetVideoInfo($"https://www.youtube.com/watch?v={_result.SourceId}");
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

        newMV.source = "youtube";
        newMV.nfoPath = $"{_settings.RootFolder}/{newMV.artist.Name}/{newMV.title}.nfo";

        await SaveThumbnailAsync($"{_settings.RootFolder}/{newMV.artist.Name}");
        newMV.thumb = $"{newMV.title}.jpg";

        newMV.vidPath = filePath;

        newMV.SaveToNFO();
        _dbContext.MusicVideos.Add(newMV);
        return await _dbContext.SaveChangesAsync();
    }

    protected virtual void OnProgressTrigger()
    {
        ProgressStarted?.Invoke(this, this);
    }
}