using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Shapes;
using System.Xml.Linq;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FFMpegCore;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using SukiUI.Controls;

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

        public MusicVideoDetailsViewModel()
        {
            DBHelper = App.GetDBHelper();
            _parentVM = App.GetVM().GetParentView();
        }

        public async void UpdateMusicVideo()
        {
            string originalTitle = _musicVideo.title;

            _musicVideo.title = Title;
            _musicVideo.year = Year;
            if (_currAlbum.Title == "") {_musicVideo.album = null;} else{ _musicVideo.album = _currAlbum; }

            //Handle title changes
            if (originalTitle != Title)
            {
                var nfoPath = _musicVideo.nfoPath;
                var folderPath = System.IO.Path.GetDirectoryName(nfoPath);
                var thumbFileName = folderPath + "/" + _musicVideo.thumb;
                if (File.Exists(thumbFileName))
                {
                    File.Move(thumbFileName, folderPath + $"/{Title}-video.jpg");
                    _musicVideo.thumb = $"{Title}-video.jpg";
                }

                if (File.Exists(nfoPath))
                {
                    File.Move(nfoPath, folderPath + $"/{Title}-video.nfo");
                    _musicVideo.nfoPath = folderPath + $"\\{Title}-video.nfo";
                }

                //Get Video
                var videoPath = _musicVideo.vidPath;
                if (videoPath.Length > 0)
                {
                    var ext = System.IO.Path.GetExtension(videoPath);
                    File.Move(videoPath, folderPath + $"/{Title}-video{ext}");
                    _musicVideo.vidPath = folderPath + $"\\{Title}-video{ext}";
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
            SukiHost.ShowDialog(parentVM, allowBackgroundClose: true);
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
            Albums.Insert(0, new Album(){Title=""});
        }

        public async void AnalyzeVideo()
        {
            var info = await FFProbe.AnalyseAsync(_musicVideo.vidPath);
            Codec = $"Codec: {info.PrimaryVideoStream.CodecName}";
            Duration = $"Duration: {info.PrimaryVideoStream.Duration.TotalMinutes}";
            Resolution = $"Resolution: {info.PrimaryVideoStream.Width}"+"x"+$"{info.PrimaryVideoStream.Height}";
            Bitrate = $"Bitrate: {info.PrimaryVideoStream.BitRate}";
            AspectRatio = $"Aspect Ratio: {info.PrimaryVideoStream.DisplayAspectRatio.Width}:{info.PrimaryVideoStream.DisplayAspectRatio.Height}";
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
                SukiHost.ShowToast("Error Deleting Video", "Please manually delete the video for " + _musicVideo.title);
            }
            NavigateBack();
        }
    }
}