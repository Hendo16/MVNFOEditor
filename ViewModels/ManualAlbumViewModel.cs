using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;

namespace MVNFOEditor.ViewModels;

public partial class ManualAlbumViewModel : ObservableObject
{
    [ObservableProperty] private string _albumNameText;
    [ObservableProperty] private string _albumYear;
    [ObservableProperty] private Artist _artist;
    [ObservableProperty] private Bitmap? _cover;

    private string _coverPath;

    [ObservableProperty] private string _coverURL;
    [ObservableProperty] private bool _localRadio;
    [ObservableProperty] private bool _uRLRadio;

    public ManualAlbumViewModel(Artist art)
    {
        _artist = art;
        URLRadio = true;
    }

    private string CachePath => $"./Cache/{_artist.Name}";

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
        var data = await App.GetHttpClient().GetByteArrayAsync(CoverURL);
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
        if (CoverURL != null)
        {
            var data = await App.GetHttpClient().GetByteArrayAsync(CoverURL);
            ms = new MemoryStream(data);
        }
        else if (CoverPath != null)
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
            using (var fs = SaveCoverBitmapStream(folderPath))
            {
                bitmap.Save(fs);
            }
        });
    }

    private Stream SaveCoverBitmapStream(string folderPath)
    {
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        return File.OpenWrite(folderPath + $"/{AlbumNameText}.jpg");
    }

    public async Task<Album> SaveAlbum()
    {
        var newAlbum = new Album();
        var _dbContext = App.GetDBContext();
        newAlbum.Artist = _artist;
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