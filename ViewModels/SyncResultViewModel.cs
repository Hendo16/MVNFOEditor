using Avalonia.Media.Imaging;
using MVNFOEditor.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using MVNFOEditor.Helpers;
using ReactiveUI;
using MVNFOEditor.DB;
using YoutubeDLSharp.Metadata;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.IO;

namespace MVNFOEditor.ViewModels
{
    public partial class SyncResultViewModel : ObservableObject
    {
        private readonly SyncResult _result;
        private MusicDbContext _dbContext;
        private YTMusicHelper _musicHelper;
        private YTDLHelper _ytDLHelper;
        private Album? _album;

        [ObservableProperty] private string _borderColor;
        [ObservableProperty] private string _downloadBtnText;
        [ObservableProperty] private string _downloadEnabled;
        [ObservableProperty] private Bitmap? _thumbnail;

        public event EventHandler<SyncResultViewModel> ProgressStarted;

        public string Title => _result.Title;
        public Artist Artist => _result.Artist;
        public string Duration => _result.Duration;

        public SyncResultViewModel(SyncResult result)
        {
            _result = result;
            _musicHelper = App.GetYTMusicHelper();
            _ytDLHelper = App.GetYTDLHelper();
            _dbContext = App.GetDBContext();
            _album = null;
        }

        public SyncResultViewModel(SyncResult result, Album album)
        {
            _result = result;
            _musicHelper = App.GetYTMusicHelper();
            _ytDLHelper = App.GetYTDLHelper();
            _dbContext = App.GetDBContext();
            _album = album;
        }

        public SyncResult HandleDownload()
        {
            BorderColor = "Green";
            DownloadEnabled = "False";
            DownloadBtnText = "Downloaded";
            return _result;
        }

        public void OpenResult()
        {
            BorderColor = "Green";
            DownloadEnabled = "False";
            DownloadBtnText = "Downloaded";
            OnProgressTrigger();
        }

        public SyncResult GetResult()
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
            MusicVideo newMV = new MusicVideo();
            SettingsData localData = _dbContext.SettingsData.First();
            VideoData vidData = await _ytDLHelper.GetVideoInfo($"https://www.youtube.com/watch?v={_result.vidID}");
            newMV.title = _result.Title;
            newMV.videoID = _result.vidID;
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
            newMV.nfoPath = $"{localData.RootFolder}/{newMV.artist.Name}/{newMV.title}-video.nfo";

            await SaveThumbnailAsync($"{localData.RootFolder}/{newMV.artist.Name}");
            newMV.thumb = $"{newMV.title}-video.jpg";

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