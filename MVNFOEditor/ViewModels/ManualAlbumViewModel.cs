using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels;

public partial class ManualAlbumViewModel : ObservableObject
{
    [ObservableProperty] private string _albumNameText;
    [ObservableProperty] private string _albumYear;
    [ObservableProperty] private Artist _linkedArtist;
    [ObservableProperty] private Bitmap? _cover;

    private string _coverPath;

    [ObservableProperty] private string _coverURL;
    [ObservableProperty] private bool _localRadio;
    [ObservableProperty] private bool _uRLRadio;

    public ManualAlbumViewModel(Artist art)
    {
        LinkedArtist = art;
        URLRadio = true;
    }

    private string CachePath => $"./Cache/{LinkedArtist.Name}";

    public string CoverPath
    {
        get => _coverPath;
        set
        {
            _coverPath = value;
            OnPropertyChanged();
        }
    }

    public async void GrabURL()
    {
        var data = await NetworkHandler.GetFileData(CoverURL);
        if (data == null)
        {
            ToastHelper.ShowError("Cover Error", "Couldn't fetch album artwork, please check logs");
            return;
        }
        var ms = new MemoryStream(data);
        Cover = await Task.Run(() => Bitmap.DecodeToWidth(ms, 200));
    }

    public async void LoadCover(string path)
    {
        CoverPath = path;
        Cover = await Task.Run(() => Bitmap.DecodeToWidth(File.OpenRead(path), 200));
    }

    public async Task SaveCoverAsync(string folderPath)
    {
        Stream ms;
        if (!string.IsNullOrEmpty(CoverURL))
        {
            var data = await NetworkHandler.GetFileData(CoverURL);
            if (data == null)
            {
                ToastHelper.ShowError("Cover Error", "Couldn't fetch album artwork, please check logs");
                return;
            }
            ms = new MemoryStream(data);
        }
        else if (!string.IsNullOrEmpty(CoverPath))
        {
            ms = File.OpenRead(CoverPath);
        }
        else
        {
            return;
        }

        var bitmap = new Bitmap(ms);
        await Task.Run(() =>
        {
            using var fs = SaveCoverBitmapStream(folderPath);
            bitmap.Save(fs);
        });
    }

    private Stream SaveCoverBitmapStream(string folderPath)
    {
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        return File.OpenWrite(folderPath + $"/{AlbumNameText}.jpg");
    }
    
    public AlbumResult GetResult()
    {
        return new AlbumResult(AlbumNameText, AlbumYear, _coverPath, LinkedArtist);
    }

    public async Task<Album> SaveAlbum()
    {
        var newAlbum = new Album();
        var _dbContext = App.GetDBContext();
        newAlbum.Artist = LinkedArtist;
        newAlbum.Year = AlbumYear;
        newAlbum.Title = AlbumNameText;
        //ensure no duplicates
        if (!_dbContext.Album.Any(e =>
                e.Title == newAlbum.Title && e.Artist == newAlbum.Artist && e.Year == newAlbum.Year))
        {
            _dbContext.Album.Add(newAlbum);
            await _dbContext.SaveChangesAsync();
            await SaveCoverAsync(CachePath);
        }

        return newAlbum;
    }
}