using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Shapes;
using System.Xml.Linq;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FFMpegCore;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using MVNFOEditor.Settings;
using SukiUI.Controls;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels
{
    public partial class MusicVideoDetailsViewModel : ObservableObject
    {
        private ArtistListParentViewModel _parentVM;
        private MusicVideo _musicVideo;
        private List<Album?> _albums;
        private Album _currAlbum;
        private MusicDBHelper DBHelper;
        private Bitmap? _thumbnail;
        
        [ObservableProperty] private string _codec;
        [ObservableProperty] private string _duration;
        [ObservableProperty] private string _resolution;
        [ObservableProperty] private string _title;
        [ObservableProperty] private string _year;
        [ObservableProperty] private string _source;
        [ObservableProperty] private string _bitrate;
        [ObservableProperty] private string _aspectRatio;

        public MusicVideoDetailsViewModel()
        {
            DBHelper = App.GetDBHelper();
            _parentVM = App.GetVM().GetParentView();
            ISettings settingsData = App.GetSettings();
            GlobalFFOptions.Configure(new FFOptions { 
                BinaryFolder = Directory.GetParent(settingsData.FFPROBEPath).FullName,
                TemporaryFilesFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", "FFPROBE", "tmp") 
            });
        }

        public Bitmap? Thumbnail
        {
            get { return _thumbnail; }
            set
            {
                _thumbnail = value;
                OnPropertyChanged(nameof(Thumbnail));
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

        public async void UpdateMusicVideo()
        {
            string originalTitle = _musicVideo.title;

            _musicVideo.title = Title;
            _musicVideo.year = Year;
            //TODO: Handle better blank album entry
            //if (_currAlbum.Title == "") {_musicVideo.album = new Album();} else{ _musicVideo.album = _currAlbum; }

            //Handle title changes
            if (originalTitle != Title)
            {
                var nfoPath = _musicVideo.nfoPath;
                var folderPath = System.IO.Path.GetDirectoryName(nfoPath);
                var thumbFileName = folderPath + "/" + _musicVideo.thumb;
                if (File.Exists(thumbFileName))
                {
                    File.Move(thumbFileName, folderPath + $"/{Title}.jpg");
                    _musicVideo.thumb = $"{Title}.jpg";
                }

                if (File.Exists(nfoPath))
                {
                    File.Move(nfoPath, folderPath + $"/{Title}.nfo");
                    _musicVideo.nfoPath = folderPath + $"\\{Title}.nfo";
                }

                //Get Video
                var videoPath = _musicVideo.vidPath;
                if (videoPath.Length > 0)
                {
                    var ext = System.IO.Path.GetExtension(videoPath);
                    File.Move(videoPath, folderPath + $"/{Title}{ext}");
                    _musicVideo.vidPath = folderPath + $"\\{Title}{ext}";
                }
            }
            int success = await DBHelper.UpdateMusicVideo(_musicVideo);
            if (success == 0)
            {
                Debug.WriteLine(success);
            }
            NavigateBack();
        }

        public void UpdateVideoSource()
        {
            ManualMusicVideoViewModel newVM = new ManualMusicVideoViewModel(_musicVideo);
            AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(newVM, true);
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => parentVM)
                .Dismiss().ByClickingBackground()
                .TryShow();
        }

        public void SetVideo(MusicVideo video)
        {
            _musicVideo = video;
            Title = _musicVideo.title;
            Year = _musicVideo.year;
            Source = _musicVideo.source;
            Albums = DBHelper.GetAlbums(video.artist.Id);
            if (video.album != null)
            {
                CurrAlbum = Albums.Find(a => a.Id == video.album.Id);
            }
            //Albums.Insert(0, new Album() { Title = "" });
        }

        public void NewAlbum()
        {
            ManualAlbumViewModel manualVM = new ManualAlbumViewModel(_musicVideo.artist);
            NewAlbumDialogViewModel newAlbumVM = new NewAlbumDialogViewModel(manualVM, _musicVideo.artist, true);
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => newAlbumVM)
                .OnDismissed(_ => { Albums = DBHelper.GetAlbums(_musicVideo.artist.Id);})
                .TryShow();
        }

        public async void AnalyzeVideo()
        {
            var info = await FFProbe.AnalyseAsync(_musicVideo.vidPath);
            int minutes = (int)info.PrimaryVideoStream.Duration.TotalMinutes;
            int seconds = (int)((info.PrimaryVideoStream.Duration.TotalMinutes - (int)info.PrimaryVideoStream.Duration.TotalMinutes) * 60);
            Codec = $"Codec: {info.PrimaryVideoStream.CodecName}";
            Duration = minutes != 0 ? $"Duration: {minutes:D2}:{seconds:D2}" : "Duration: unavaible";
            Resolution = $"Resolution: {info.PrimaryVideoStream.Width}"+"x"+$"{info.PrimaryVideoStream.Height}";
            Bitrate = info.PrimaryVideoStream.BitRate/ 1000000.0 >= 1 ? $"AverageBitrate: {Math.Round(info.PrimaryVideoStream.BitRate / 1000000.0)} Mbps" : $"AverageBitrate: {Math.Round(info.PrimaryVideoStream.BitRate / 1000.0)} kbps";
            AspectRatio = info.PrimaryVideoStream.DisplayAspectRatio.Width != 0 ? $"Aspect Ratio: {info.PrimaryVideoStream.DisplayAspectRatio.Width}:{info.PrimaryVideoStream.DisplayAspectRatio.Height}" : "Aspect Ratio unavaliable";
        }

        public async Task LoadThumbnail()
        {
            await using (var imageStream = await _musicVideo.LoadThumbnailBitmapAsync())
            {
                if (imageStream != null)
                {
                    try { Thumbnail = new Bitmap(imageStream); } catch (ArgumentException e) { }
                }
            }
        }

        public void NavigateBack()
        {

            _parentVM.BackToDetails(true);
        }

        public void DeleteVideo()
        {
            if (DBHelper.DeleteVideo(_musicVideo) != 1)
            {
                App.GetVM().GetToastManager().CreateToast()
                    .WithTitle("Error Deleting Video")
                    .WithContent($"Please manually delete the video for {_musicVideo.title}")
                    .OfType(NotificationType.Error)
                    .Queue();
            }
            NavigateBack();
        }
    }
}