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

namespace MVNFOEditor.ViewModels
{
    public class ManualAlbumViewModel : ReactiveObject
    {
        private Artist _artist;
        private string _albumNameText;
        private string CachePath => $"./Cache/{_artist.Name}";
        public string AlbumNameText
        {
            get => _albumNameText;
            set => this.RaiseAndSetIfChanged(ref _albumNameText, value);
        }
        private string _albumYear;
        public string AlbumYear
        {
            get => _albumYear;
            set => this.RaiseAndSetIfChanged(ref _albumYear, value);
        }

        private Bitmap? _cover;
        private string _coverPath;
        public Bitmap? AlbumCover
        {
            get => _cover;
            set => this.RaiseAndSetIfChanged(ref _cover, value);
        }
        public string CoverPath
        {
            get => _coverPath;
            set => this.RaiseAndSetIfChanged(ref _coverPath, value);
        }

        public async void LoadCover(string path)
        {
            CoverPath = path;
            AlbumCover = await Task.Run(() => Bitmap.DecodeToWidth(File.OpenRead(path), 200));
        }
        public async Task SaveCoverAsync(string folderPath)
        {
            var bitmap = Bitmap.DecodeToWidth(File.OpenRead(CoverPath), 400);
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
        }

        public async Task<Album> SaveAlbum()
        {
            Album newAlbum = new Album();
            newAlbum.Artist = _artist;
            newAlbum.Year = AlbumYear;
            newAlbum.Title = AlbumNameText;
            App.GetDBContext().Album.Add(newAlbum);
            await App.GetDBContext().SaveChangesAsync();
            await SaveCoverAsync(CachePath);
            return newAlbum;
        }
    }
}