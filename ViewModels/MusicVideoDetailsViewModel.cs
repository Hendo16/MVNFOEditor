using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using SukiUI.Controls;

namespace MVNFOEditor.ViewModels
{
    public class MusicVideoDetailsViewModel : ObservableObject
    {
        private ArtistListParentViewModel _parentVM;
        private MusicVideo _musicVideo;
        private List<Album> _albums;
        private Album _currAlbum;
        private string _title;
        private string _year;
        private string _source;
        private MusicDBHelper DBHelper;

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
            _musicVideo.album = _currAlbum;
            //Handle title changes
            if (originalTitle != Title)
            {
                var nfoPath = _musicVideo.filePath;
                var folderPath = Path.GetDirectoryName(nfoPath);
                if (File.Exists(nfoPath))
                {
                    File.Move(nfoPath, folderPath + $"/{Title}-video.nfo");
                    _musicVideo.filePath = folderPath + $"/{Title}-video.nfo";
                }
                var thumbFileName = folderPath + "/" + _musicVideo.thumb;
                if (File.Exists(thumbFileName))
                {
                    File.Move(thumbFileName, folderPath + $"/{Title}-video.jpg");
                }

                //Get Video
                var videoPath = Directory.GetFiles(folderPath + "/", _musicVideo.title + "-video.*");
                if (videoPath.Length > 0)
                {
                    var ext = Path.GetExtension(videoPath[0]);
                    File.Move(videoPath[0], folderPath + $"/{Title}-video.{ext}");
                }
            }
            int success = await DBHelper.UpdateMusicVideo(_musicVideo);
            if (success == 0)
            {
                Debug.WriteLine(success);
                _parentVM.BackToDetails(true);
            }
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
        }

        public void NavigateBack()
        {
            _parentVM.BackToDetails();
        }

        public void DeleteVideo()
        {
            if (DBHelper.DeleteVideo(_musicVideo) != 1)
            {
                SukiHost.ShowToast("Error Deleting Video", "Please manually delete the video for " + _musicVideo.title);
            }
            _parentVM.BackToDetails(true);
        }
    }
}