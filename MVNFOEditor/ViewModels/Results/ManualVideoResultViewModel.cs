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
    private readonly VideoResult _result;

    [ObservableProperty] private string _borderColor;
    [ObservableProperty] private string _downloadBtnText;
    [ObservableProperty] private bool _downloadEnabled;
    [ObservableProperty] private Bitmap? _thumbnail;
    
    public ManualVideoResultViewModel(VideoResult result)
    {
        _result = result;
        _dbContext = App.GetDBContext();
        _settings = App.GetSettings();
        _album = result.Album;
    }
    public static async Task<ManualVideoResultViewModel> CreateViewModel(VideoResult result)
    {
        var newVm = new ManualVideoResultViewModel(result);
        await newVm.LoadThumbnail();
        return newVm;
    }

    public string Title => _result.Name;
    public string Year => _result.Year;
    public Artist Artist => _result.Artist;
    public Album? Album => _result.Album;
    public string? Duration => _result.Duration;

    public event EventHandler<ManualVideoResult> ProgressStarted;
    public event EventHandler RemoveCallback;

    public void RemoveListing()
    {
        RemoveCallback?.Invoke(this, EventArgs.Empty);
    }

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
}