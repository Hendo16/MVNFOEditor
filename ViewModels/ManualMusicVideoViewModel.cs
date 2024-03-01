using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using YoutubeDLSharp.Metadata;

namespace MVNFOEditor.ViewModels
{
    public partial class ManualMusicVideoViewModel : ObservableObject
    {
        private string _title;
        private string _year;
        private string _source;
        private string _videoPath;
        private string _videoURL;

        private bool _ytRadio;
        private bool _ytVisible;
        private bool _localRadio;
        private bool _localVisible;

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _grabText;

        private Bitmap? _thumb;
        private List<Album> _albums;
        private Album _currAlbum;
        private YTDLHelper _ytDLHelper;

        private static HttpClient s_httpClient = new();

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public string Year
        {
            get { return _year; }
            set
            {
                _year = value;
                OnPropertyChanged(nameof(Year));
            }
        }

        public string Source
        {
            get { return _source; }
            set
            {
                _source = value;
                OnPropertyChanged(nameof(Source));
            }
        }

        public string VideoPath
        {
            get { return _videoPath; }
            set
            {
                _videoPath = value;
                OnPropertyChanged(nameof(VideoPath));
            }
        }

        public string VideoURL
        {
            get { return _videoURL; }
            set
            {
                _videoURL = value;
                OnPropertyChanged(nameof(VideoURL));
            }
        }

        public List<Album> Albums
        {
            get { return _albums; }
            set
            {
                _albums = value;
                OnPropertyChanged(nameof(Albums));
            }
        }

        public Album CurrAlbum
        {
            get { return _currAlbum; }
            set
            {
                _currAlbum = value;
                OnPropertyChanged(nameof(CurrAlbum));
            }
        }
        
        public Bitmap? Thumbnail
        {
            get { return _thumb; }
            set
            {
                _thumb = value;
                OnPropertyChanged(nameof(Thumbnail));
            }
        }

        public bool YTRadio
        {
            get { return _ytRadio; }
            set
            {
                _ytRadio = value;
                OnPropertyChanged(nameof(YTRadio));
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
            get { return _ytVisible; }
            set
            {
                _ytVisible = value;
                OnPropertyChanged(nameof(YTVisible));
            }
        }

        public bool LocalRadio
        {
            get { return _localRadio; }
            set
            {
                _localRadio = value;
                OnPropertyChanged(nameof(LocalRadio));
            }
        }

        public bool LocalVisible
        {
            get { return _localVisible; }
            set
            {
                _localVisible = value;
                OnPropertyChanged(nameof(LocalVisible));
            }
        }

        public VideoData? _vidData { get; set; }
        public Artist Artist { get; set; }

        public ManualMusicVideoViewModel(Artist art)
        {
            _ytDLHelper = App.GetYTDLHelper();
            Artist = art;
            YTRadio = true;
            GrabText = "Grab";
            if (App.GetDBContext().Album.Any(e => e.Artist == Artist))
            {
                Albums = App.GetDBContext().Album.Where(e => e.Artist == Artist).ToList();
            };
        }

        public ManualMusicVideoViewModel(Album album)
        {
            _ytDLHelper = App.GetYTDLHelper();
            Albums = new List<Album>(){ album };
            CurrAlbum = album;
            Artist = album.Artist;
            YTRadio = true;
            GrabText = "Grab";
        }

        public ManualMusicVideoViewModel(List<Album> albumList)
        {
            _ytDLHelper = App.GetYTDLHelper();
            Albums = albumList;
            Artist = Albums[0].Artist;
            YTRadio = true;
            GrabText = "Grab";
        }

        public async void GrabYTVideo()
        {
            IsBusy = true;
            GrabText = "Searching";
            VideoData VidData = await _ytDLHelper.GetVideoInfo(VideoURL);
            if (VidData != null)
            {
                Title = App.GetYTMusicHelper().CleanYTName(VidData.Title, Artist);
                Year = VidData.ReleaseYear;
                LoadThumbnail(VidData.Thumbnail);
                _vidData = VidData;
            }
            IsBusy = false;
            GrabText = "Grab";
        }

        public async Task LoadThumbnail(string URL)
        {
            var data = await s_httpClient.GetByteArrayAsync(URL);
            await using (var imageStream = new MemoryStream(data))
            {
                Thumbnail = Bitmap.DecodeToWidth(imageStream, 200);
            }
        }

        public async Task SaveThumbnailAsync(string folderPath)
        {
            var bitmap = Thumbnail;
            await Task.Run(() =>
            {
                using (var fs = SaveThumbnailBitmapStream(folderPath))
                {
                    bitmap.Save(fs);
                }
            });
        }

        private Stream SaveThumbnailBitmapStream(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return File.OpenWrite(folderPath + $"/{Title}-video.jpg");
        }

        public void SetLocalVideo(string path)
        {
            VideoPath = path;
            Debug.WriteLine(VideoPath);
        }

        public void ClearData()
        {
            Title = "";
            Year = "";
            Source = "";
            VideoURL = "";
            Thumbnail = null;
        }
    }
}