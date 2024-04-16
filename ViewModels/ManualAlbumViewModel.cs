using Avalonia.Media.Imaging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVNFOEditor.Models;
using Avalonia.Controls.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Net.Http;
using MVNFOEditor.DB;

namespace MVNFOEditor.ViewModels
{
    public partial class ManualAlbumViewModel : ObservableObject
    {
        private static HttpClient s_httpClient = new();
        private string CachePath => $"./Cache/{_artist.Name}";

        [ObservableProperty] private string _coverURL;
        [ObservableProperty] private Artist _artist;
        [ObservableProperty] private Bitmap? _cover;
        [ObservableProperty] private string _albumNameText;
        [ObservableProperty] private bool _uRLRadio;
        [ObservableProperty] private bool _localRadio;
        [ObservableProperty] private string _albumYear;
        
        private string _coverPath;
        public string CoverPath
        {
            get { return _coverPath; }
            set
            {
                _coverPath = value;
                OnPropertyChanged(nameof(CoverPath));
            }
        }

        public async void GrabURL()
        {
            var data = await s_httpClient.GetByteArrayAsync(CoverURL);
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
                var data = await s_httpClient.GetByteArrayAsync(CoverURL);
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
            Bitmap? bitmap = new Bitmap(ms);
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
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return File.OpenWrite(folderPath + $"/{AlbumNameText}.jpg");
        }

        public ManualAlbumViewModel(Artist art)
        {
            _artist = art;
            URLRadio = true;
        }

        public async Task<Album> SaveAlbum()
        {
            Album newAlbum = new Album();
            MusicDbContext _dbContext = App.GetDBContext();
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
}