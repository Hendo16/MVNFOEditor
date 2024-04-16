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
using Microsoft.EntityFrameworkCore;
using MVNFOEditor.DB;
using Avalonia.Controls.Shapes;
using Newtonsoft.Json.Linq;
using Path = Avalonia.Controls.Shapes.Path;

namespace MVNFOEditor.ViewModels
{
    public partial class ManualMusicVideoViewModel : ObservableObject
    {
        [ObservableProperty] private string _title;
        [ObservableProperty] private string _year;
        [ObservableProperty] private string _source;
        [ObservableProperty] private string _videoURL;
        [ObservableProperty] private string _grabText;
        [ObservableProperty] private string _videoPath;

        [ObservableProperty] private bool _localRadio;
        [ObservableProperty] private bool _localVisible;
        [ObservableProperty] private bool _isBusy;
        private bool _ytRadio;
        private bool _ytVisible;

        private Bitmap? _thumb;
        private List<Album> _albums;
        private Album _currAlbum;
        private YTDLHelper _ytDLHelper;
        private YTMusicHelper _ytMusicHelper;
        private MusicDbContext _dbContext;
        private static HttpClient s_httpClient = new();
        public VideoData? _vidData { get; set; }
        public Artist Artist { get; set; }
        public MusicVideo PreviousVideo { get; set; }

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

        public ManualMusicVideoViewModel(Artist art)
        {
            _dbContext = App.GetDBContext();
            _ytMusicHelper = App.GetYTMusicHelper();
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
            _dbContext = App.GetDBContext();
            _ytMusicHelper = App.GetYTMusicHelper();
            _ytDLHelper = App.GetYTDLHelper();
            Albums = new List<Album>(){ album };
            CurrAlbum = album;
            Artist = album.Artist;
            YTRadio = true;
            GrabText = "Grab";
        }

        public ManualMusicVideoViewModel(MusicVideo video)
        {
            _dbContext = App.GetDBContext();
            _ytMusicHelper = App.GetYTMusicHelper();
            _ytDLHelper = App.GetYTDLHelper();
            Artist = video.artist;
            if (video.source == "youtube" || video.source == "tidal" || video.source == "null")
            {
                YTRadio = true;
                VideoURL = video.videoID;
                GrabText = "Grab";
            }
            else{LocalRadio = true;}
            if (App.GetDBContext().Album.Any(e => e.Artist == Artist))
            {
                Albums = App.GetDBContext().Album.Where(e => e.Artist == Artist).ToList();
            }
            Title = video.title;
            Year = video.year;
            CurrAlbum = video.album;
            PreviousVideo = video;
            var root_folder = System.IO.Path.GetDirectoryName(video.nfoPath);
            LoadThumbnail($"{root_folder}\\{video.thumb}", true);
        }

        public ManualMusicVideoViewModel(List<Album> albumList)
        {
            _dbContext = App.GetDBContext();
            _ytMusicHelper = App.GetYTMusicHelper();
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
                if (VidData.ReleaseYear != null)
                {
                    Year = VidData.ReleaseYear;
                }
                else
                {
                    Year = ((DateTime)VidData.UploadDate).Year.ToString();
                }
                LoadThumbnail(VidData.Thumbnail);
                _vidData = VidData;
            }
            IsBusy = false;
            GrabText = "Grab";
        }

        public void DetectAlbum()
        {
            var SongName = Title.ToLower();
            foreach (JToken AlbumResult in Artist.YTMusicAlbumResults)
            {
                JObject newAlbumCheck = _ytMusicHelper.get_album(AlbumResult["browseId"].ToString());
                //Check if the title directly exists
                if (((JArray)newAlbumCheck["tracks"]).Any(t => t["title"].ToString().ToLower() == SongName))
                {
                    if (_dbContext.Album.Any(a => a.ytMusicBrowseID == AlbumResult["browseId"].ToString()))
                    {
                        CurrAlbum = _dbContext.Album.First(a =>
                            a.ytMusicBrowseID == AlbumResult["browseId"].ToString());
                    }
                    else
                    {
                        AlbumResult currResult = new AlbumResult();

                        currResult.Title = newAlbumCheck["title"].ToString();
                        try { currResult.Year = newAlbumCheck["year"].ToString(); } catch (NullReferenceException e) { }
                        currResult.browseId = AlbumResult["browseId"].ToString();
                        currResult.thumbURL = _ytMusicHelper.GetHighQualityArt(newAlbumCheck);
                        currResult.isExplicit = Convert.ToBoolean(newAlbumCheck["isExplicit"]);
                        currResult.Artist = Artist;

                        Album newAlbum = new Album(currResult);
                        _dbContext.Album.Add(newAlbum);
                        _dbContext.SaveChanges();

                        CurrAlbum = newAlbum;
                    }
                    continue;
                }
            }
        }

        public async Task LoadThumbnail(string URL, bool edit = false)
        {
            var data = edit ? await File.ReadAllBytesAsync(URL) : await s_httpClient.GetByteArrayAsync(URL);
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
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return File.OpenWrite(folderPath + $"/{fileName}-video.jpg");
        }

        public void SetLocalVideo(string path)
        {
            VideoPath = path;
            Title = System.IO.Path.GetFileNameWithoutExtension(path);
            SettingsData localData = App.GetDBContext().SettingsData.First();
            Debug.WriteLine($"Move {System.IO.Path.GetFileName(VideoPath)} to {localData.RootFolder}/{Artist.Name}/{System.IO.Path.GetFileName(VideoPath)}");
        }

        public void ClearData()
        {
            Title = "";
            Year = "";
            Source = "";
            VideoURL = "";
            Thumbnail = null;
        }
        public async void GenerateManualNFO(string vidPath)
        {
            MusicDbContext _dbContext = App.GetDBContext();
            SettingsData localData = _dbContext.SettingsData.First();
            MusicVideo newMV = new MusicVideo();
            newMV.title = Title;
            newMV.year = Year;
            newMV.artist = Artist;
            newMV.vidPath = vidPath;
            if (CurrAlbum != null)
            {
                newMV.album = CurrAlbum;
            }
            else
            {
                newMV.album = null;
            }

            if (_vidData != null)
            {
                newMV.videoID = _vidData.ID;
                newMV.source = "youtube";
                await SaveThumbnailAsync($"{localData.RootFolder}/{newMV.artist.Name}",newMV.title);
                newMV.thumb = $"{newMV.title}-video.jpg";
            }
            else
            {
                newMV.source = "local";
            }
            newMV.nfoPath = $"{localData.RootFolder}/{newMV.artist.Name}/{newMV.title}-video.nfo";
            
            newMV.SaveToNFO();
            _dbContext.MusicVideos.Add(newMV);
            _dbContext.SaveChanges();
        }
    }
}