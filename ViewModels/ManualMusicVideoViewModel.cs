using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using YoutubeDLSharp.Metadata;
using MVNFOEditor.DB;
using FFMpegCore;
using MVNFOEditor.Settings;
using Newtonsoft.Json.Linq;

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
        [ObservableProperty] private ObservableCollection<ManualMVEntryViewModel> _manualItems;

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
        private static ISettings _settings;
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
            _settings = App.GetSettings();
            Artist = art;
            YTRadio = true;
            GrabText = "Grab";
            if (App.GetDBContext().Album.Any(e => e.Artist == Artist))
            {
                Albums = App.GetDBContext().Album.Where(e => e.Artist == Artist).ToList();
            };
            ManualItems = new ObservableCollection<ManualMVEntryViewModel>();
        }

        public ManualMusicVideoViewModel(Album album)
        {
            _dbContext = App.GetDBContext();
            _ytMusicHelper = App.GetYTMusicHelper();
            _ytDLHelper = App.GetYTDLHelper();
            _settings = App.GetSettings();
            Albums = new List<Album>(){ album };
            CurrAlbum = album;
            Artist = album.Artist;
            YTRadio = true;
            GrabText = "Grab";
            ManualItems = new ObservableCollection<ManualMVEntryViewModel>();
        }

        public ManualMusicVideoViewModel(MusicVideo video)
        {
            _dbContext = App.GetDBContext();
            _ytMusicHelper = App.GetYTMusicHelper();
            _ytDLHelper = App.GetYTDLHelper();
            _settings = App.GetSettings();
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
            ManualItems = new ObservableCollection<ManualMVEntryViewModel>();
        }

        public ManualMusicVideoViewModel(List<Album> albumList)
        {
            _dbContext = App.GetDBContext();
            _ytMusicHelper = App.GetYTMusicHelper();
            _ytDLHelper = App.GetYTDLHelper();
            _settings = App.GetSettings();
            Albums = albumList;
            Artist = Albums[0].Artist;
            YTRadio = true;
            GrabText = "Grab";
            ManualItems = new ObservableCollection<ManualMVEntryViewModel>();
        }

        private void RemoveCard(object sender, EventArgs e)
        {
            ManualItems.Remove((ManualMVEntryViewModel)sender);
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

        public void AddSingleToList()
        {
            var newMVEntry = new ManualMVEntryViewModel(_title, _year, Thumbnail, _vidData.ID, CurrAlbum, _vidData);
            newMVEntry.RemoveCallback += RemoveCard;
            ManualItems.Add(newMVEntry);
        }

        public void DetectAlbum()
        {
            var SongName = Title.ToLower();
            ArtistMetadata artistMetadata = Artist.GetArtistMetadata(SearchSource.YouTubeMusic);
            foreach (JToken AlbumResult in artistMetadata.AlbumResults)
            {
                JObject newAlbumCheck = _ytMusicHelper.get_album(AlbumResult["browseId"].ToString());
                //Check if the title directly exists
                if (((JArray)newAlbumCheck["tracks"]).Any(t => t["title"].ToString().ToLower() == SongName))
                {
                    if (_dbContext.Album.Any(a => a.AlbumBrowseID == AlbumResult["browseId"].ToString()))
                    {
                        CurrAlbum = _dbContext.Album.First(a =>
                            a.AlbumBrowseID == AlbumResult["browseId"].ToString());
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
            return File.OpenWrite(folderPath + $"/{fileName}.jpg");
        }

        public void SetLocalVideo(string path)
        {
            VideoPath = path;
            Title = Path.GetFileNameWithoutExtension(path);
            Debug.WriteLine($"Move {Path.GetFileName(VideoPath)} to {_settings.RootFolder}/{Artist.Name}/{System.IO.Path.GetFileName(VideoPath)}");
        }

        public void ClearData()
        {
            Title = "";
            Year = "";
            Source = "";
            VideoURL = "";
            Thumbnail = null;
        }
        public async Task<int> GenerateManualNFO(string vidPath, int currInd)
        {
            MusicDbContext _dbContext = App.GetDBContext();
            MusicVideo newMV = new MusicVideo();
            newMV.title = ManualItems[currInd].Title;
            newMV.year = ManualItems[currInd].Year;
            newMV.artist = Artist;
            newMV.vidPath = vidPath;
            if (ManualItems[currInd].Album != null)
            {
                newMV.album = ManualItems[currInd].Album;
            }
            else
            {
                newMV.album = null;
            }

            if (ManualItems[currInd].VidData != null)
            {
                newMV.videoID = ManualItems[currInd].VidData.ID;
                newMV.source = "youtube";
                await SaveThumbnailAsync($"{_settings.RootFolder}/{newMV.artist.Name}",newMV.title);
                newMV.thumb = $"{newMV.title}.jpg";
            }
            else
            {
                newMV.source = "local";
            }
            newMV.nfoPath = $"{_settings.RootFolder}/{newMV.artist.Name}/{newMV.title}.nfo";
            
            newMV.SaveToNFO();
            _dbContext.MusicVideos.Add(newMV);
            return await _dbContext.SaveChangesAsync();
        }
        
        public async Task<int> GenerateManualNFO(string vidPath, bool edit)
        {
            MusicVideo newMV;
            if (edit)
            {
                //Clear out old data
                File.Delete(PreviousVideo.vidPath);
                File.Delete($"{_settings.RootFolder}\\{Artist.Name}\\{PreviousVideo.thumb}");
                File.Delete($"{_settings.RootFolder}\\{Artist.Name}\\{PreviousVideo.nfoPath}");
                //Retain old video entry
                newMV = PreviousVideo;
            }
            else
            {
                newMV = new MusicVideo();
            }
             
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
                await SaveThumbnailAsync($"{_settings.RootFolder}/{newMV.artist.Name}", newMV.title);
                newMV.thumb = $"{newMV.title}.jpg";
            }
            else
            {
                newMV.source = "local";
                var newImagePath = $"{_settings.RootFolder}/{newMV.artist.Name}/{newMV.title}.png";
                FFMpeg.Snapshot(vidPath, newImagePath, new System.Drawing.Size(400, 225), TimeSpan.FromSeconds(_settings.ScreenshotSecond));
                newMV.thumb = $"{newMV.title}.png";
            }

            newMV.nfoPath = $"{_settings.RootFolder}/{newMV.artist.Name}/{newMV.title}.nfo";
            newMV.SaveToNFO();
            _dbContext.MusicVideos.Add(newMV);
            return await _dbContext.SaveChangesAsync();
        }
    }
}