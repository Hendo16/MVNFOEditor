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

public partial class ManualVideoResultViewModel : ObservableObject
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ManualVideoResultViewModel));
    private static ISettings _settings;
    private readonly Album? _album;
    private readonly MusicDbContext _dbContext;
    private readonly ManualVideoResult _result;

    [ObservableProperty] private string _borderColor;
    [ObservableProperty] private string _downloadBtnText;
    [ObservableProperty] private bool _downloadEnabled;
    [ObservableProperty] private Bitmap? _thumbnail;
    
    public ManualVideoResultViewModel(ManualVideoResult result)
    {
        _result = result;
        _dbContext = App.GetDBContext();
        _settings = App.GetSettings();
        _album = result.Album;
    }

    public string Title => _result.Name;
    public Artist Artist => _result.Artist;
    public Album? Album => _result.Album;
    public string? Duration => _result.Duration;

    public event EventHandler<ManualVideoResult> ProgressStarted;

    public ManualVideoResult HandleDownload()
    {
        BorderColor = "Green";
        DownloadEnabled = false;
        DownloadBtnText = "Downloaded";
        return _result;
    }

    public ManualVideoResult GetResult()
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
        }

        return 0;
    }
    private async Task<int> GenerateNFO_YTM(string filePath)
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
        else if (_result.VidData != null)
        {
            if (_result.VidData.ReleaseYear != null)
                newMV.year = _result.VidData.ReleaseYear;
            else
                newMV.year = ((DateTime)_result.VidData.UploadDate!).Year.ToString();
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
}