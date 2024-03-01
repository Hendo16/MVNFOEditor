using Avalonia.Media.Imaging;
using MVNFOEditor.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MVNFOEditor.Helpers;
using ReactiveUI;
using Avalonia.Interactivity;
using MVNFOEditor.DB;
using System.Xml.Linq;
using SkiaSharp;
using Avalonia.Controls;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace MVNFOEditor.ViewModels
{
    public class SyncResultViewModel : ReactiveObject
    {
        private readonly SyncResult _result;
        private Bitmap? _thumbnail;
        private string _borderColor;
        private string _downloadBtnText;
        private string _downloadEnabled;
        private MusicDbContext _dbContext;
        private YTMusicHelper _musicHelper;
        private YTDLHelper _ytDLHelper;

        public event EventHandler<SyncResult> ProgressStarted;

        private Album? _album;

        public string Title => _result.Title;
        public Artist Artist => _result.Artist;
        public string Duration => _result.Duration;
        public string DownloadBtnText
        {
            get => _downloadBtnText;
            set => this.RaiseAndSetIfChanged(ref _downloadBtnText, value);
        }
        public string DownloadEnabled
        {
            get => _downloadEnabled;
            set => this.RaiseAndSetIfChanged(ref _downloadEnabled, value);
        }
        public string BorderColor
        {
            get => _borderColor;
            set => this.RaiseAndSetIfChanged(ref _borderColor, value);
        }
        public Bitmap? Thumbnail
        {
            get => _thumbnail;
            private set => this.RaiseAndSetIfChanged(ref _thumbnail, value);
        }

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

        public void OpenResult()
        {
            GenerateNFO();
            OnProgressTrigger();
            BorderColor = "Green";
            DownloadEnabled = "False";
            DownloadBtnText = "Downloaded";
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
        
        private async void GenerateNFO()
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
            newMV.filePath = $"{localData.RootFolder}/{newMV.artist.Name}/{newMV.title}-video.nfo";

            await SaveThumbnailAsync($"{localData.RootFolder}/{newMV.artist.Name}");
            newMV.thumb = $"{newMV.title}-video.jpg";

            newMV.SaveToNFO();
            _dbContext.MusicVideos.Add(newMV);
            _dbContext.SaveChanges();
        }

        protected virtual void OnProgressTrigger()
        {
            ProgressStarted?.Invoke(this, _result);
        }
    }
}