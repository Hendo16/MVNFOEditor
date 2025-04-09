using Avalonia.Media.Imaging;
using MVNFOEditor.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using MVNFOEditor.Helpers;
using MVNFOEditor.DB;
using YoutubeDLSharp.Metadata;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Settings;

namespace MVNFOEditor.ViewModels
{
    public partial class VideoResultViewModel : ObservableObject
    {
        private readonly VideoResult _result;
        private MusicDbContext _dbContext;
        private static ISettings _settings;
        private YTMusicHelper _musicHelper;
        private YTDLHelper _ytDLHelper;
        private iTunesAPIHelper _iTunesApiHelper;
        private Album? _album;

        [ObservableProperty] private string _borderColor;
        [ObservableProperty] private string _downloadBtnText;
        [ObservableProperty] private bool _downloadEnabled;
        [ObservableProperty] private Bitmap? _thumbnail;

        public event EventHandler<VideoResultViewModel> ProgressStarted;

        public string Title => _result.Title;
        public Artist Artist => _result.Artist;
        public string Duration => _result.Duration;
        public string Year => _result.Year;
        public string TopRes => _result.TopRes;
        public string VideoURL => _result.VideoURL;

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

        public VideoResult HandleDownload()
        {
            BorderColor = "Green";
            DownloadEnabled = false;
            DownloadBtnText = "Downloaded";
            return _result;
        }

        public void OpenResult()
        {
            BorderColor = "Green";
            DownloadEnabled = false;
            DownloadBtnText = "Downloaded";
            OnProgressTrigger();
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

        public async Task<int> GenerateNFO(string filePath)
        {
            //TODO: Handle multiple sources, how do we determine metadata
            ArtistMetadata artistMetadata = _result.Artist.GetArtistMetadata(SearchSource.AppleMusic);
            switch (artistMetadata.SourceId)
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
            MusicVideo newMV = new MusicVideo();
            newMV.title = _result.Title;
            newMV.videoID = _result.VideoID;
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

            newMV.SaveToNFO();
            _dbContext.MusicVideos.Add(newMV);
            return await _dbContext.SaveChangesAsync();
        }

        private async Task<int> GenerateNFO_YTM(string filePath)
        {
            MusicVideo newMV = new MusicVideo();
            VideoData vidData =
                await _ytDLHelper.GetVideoInfo($"https://www.youtube.com/watch?v={_result.VideoID}");
            newMV.title = _result.Title;
            newMV.videoID = _result.VideoID;
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
                {
                    newMV.year = vidData.ReleaseYear;
                }
                else
                {
                    newMV.year = ((DateTime)vidData.UploadDate).Year.ToString();
                }
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
}