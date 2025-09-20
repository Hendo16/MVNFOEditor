using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using Newtonsoft.Json.Linq;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels
{
    public partial class AlbumViewModel : ObservableObject
    {
        //Show/Hide Source Options
        [ObservableProperty] private List<Bitmap> _sourceIcons = new();
        [ObservableProperty] private bool _hasYTMusic;
        [ObservableProperty] private bool _hasAppleMusic;
        private ArtistListParentViewModel _parentVM;
        private readonly Album _album;
        public event EventHandler<bool> SyncStarted;
        private YTMusicHelper ytMusicHelper;
        private iTunesAPIHelper _iTunesApiHelper;

        private ISukiToastManager ToastManager;

        public AlbumViewModel(Album album)
        {
            _parentVM = App.GetVM().GetParentView();
            ToastManager = App.GetVM().GetToastManager();
            _album = album;
            ytMusicHelper = App.GetYTMusicHelper();
            _iTunesApiHelper = App.GetiTunesHelper();
            
            foreach (var metadata in album.Metadata)
            {
                string path = metadata.SourceIconPath;
                SourceIcons.Add(new Bitmap(path));
                if (metadata.SourceId == SearchSource.YouTubeMusic)
                {
                    HasYTMusic = true;
                }
                if (metadata.SourceId == SearchSource.AppleMusic)
                {
                    HasAppleMusic = true;
                }
            }
            
        }

        public Artist Artist => _album.Artist;

        public string Title => _album.Title;

        public string Year => _album.Year;

        private List<MusicVideo> _songs;
        
        public List<MusicVideo> Songs
        {
            get { return _songs; }
            set
            {
                _songs = value;
                OnPropertyChanged();
            }
        }

        private Bitmap? _cover;
        public Bitmap? Cover
        {
            get { return _cover; }
            set
            {
                _cover = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadCover()
        {
            await using (var imageStream = await _album.LoadCoverBitmapAsync())
            {
                Cover = Bitmap.DecodeToWidth(imageStream, 400);
            }
        }

        public async Task SaveCoverAsync()
        {
            var bitmap = Cover;
            await Task.Run(() =>
            {
                using (var fs = _album.SaveCoverBitmapStream())
                {
                    bitmap.Save(fs);
                }
            });
        }

        public void GenerateSongs()
        {
            var dbContext = App.GetDBContext();
            Songs = dbContext.MusicVideos.Where(mv => mv.album.Id == _album.Id).ToList();
        }
        public async void HandleSongClick(int songID)
        {
            var dbContext = App.GetDBContext();
            MusicVideo songObj = dbContext.MusicVideos.First(mv => mv.Id == songID);
            if (File.Exists(songObj.vidPath))
            {
                MusicVideoDetailsViewModel mvVM = new MusicVideoDetailsViewModel();
                mvVM.SetVideo(songObj);
                mvVM.AnalyzeVideo();
                await mvVM.LoadThumbnail();
                _parentVM.CurrentContent = mvVM;
            }
            else
            {
                //Handle deleted video
                ToastManager.CreateToast()
                    .WithTitle("Error")
                    .WithContent($"{songObj.title} has been deleted - removing from db...")
                    .OfType(NotificationType.Error)
                    .Queue();
                App.GetDBContext().MusicVideos.Remove(songObj);
                App.GetDBContext().SaveChanges();
                _parentVM.RefreshDetails();
            }
        }

        public async void AddAppleMusicVideo()
        {
            OnSyncTrigger(true);
            ObservableCollection<VideoResultViewModel> results = await _iTunesApiHelper.GenerateVideoResultList(_album, null);
            //Sometimes artists won't have any videos listed, so we need to handle this
            if (results.Count == 0)
            {
                OnSyncTrigger(false);
                App.GetVM().GetDialogManager().CreateDialog()
                    .WithContent("No Videos Available")
                    .Dismiss().ByClickingBackground()
                    .TryShow();
                return;
            }
            VideoResultsViewModel resultsVM = new VideoResultsViewModel(results);
            ArtistMetadata artistMetadata = Artist.GetArtistMetadata(SearchSource.AppleMusic);
            AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.ProgressStarted += parentVM.SaveAMVideo;
            }
            resultsVM.BusyText = "Searching Apple Music...";
            OnSyncTrigger(false);
            //Open Dialog
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => parentVM)
                //.OnDismissed(e => parentVM.HandleDismiss())
                .TryShow();
        }

        public async void AddYTMVideo()
        {
            OnSyncTrigger(true);
            ArtistMetadata artistMetadata = Artist.GetArtistMetadata(SearchSource.YouTubeMusic);
            string artistID = artistMetadata.BrowseId;
            List<VideoResult>? videos = await Artist.GetVideos(SearchSource.YouTubeMusic);
            //Sometimes artists won't have any videos listed, so we need to handle this
            if (videos == null)
            {
                OnSyncTrigger(false);
                App.GetVM().GetDialogManager().CreateDialog()
                    .WithContent("No Videos Available")
                    .Dismiss().ByClickingBackground()
                    .TryShow();
                return;
            }
            YtMusicNet.Models.Album? fullAlbum = await ytMusicHelper.GetAlbum(_album.AlbumBrowseID);
            ObservableCollection<VideoResultViewModel> results = await ytMusicHelper.GenerateVideoResultList(videos, fullAlbum, Songs, _album);

            if (results.Count == 0)
            {
                OnSyncTrigger(false);
                App.GetVM().GetDialogManager().CreateDialog()
                    .WithContent("No Videos Available")
                    .Dismiss().ByClickingBackground()
                    .TryShow();
            }
            else
            {
                VideoResultsViewModel resultsVM = new VideoResultsViewModel(results);
                AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    result.ProgressStarted += parentVM.SaveYTMVideo;
                }

                OnSyncTrigger(false);
                //Open Dialog
                App.GetVM().GetDialogManager().CreateDialog()
                    .WithViewModel(dialog => parentVM)
                    .TryShow();
            }
        }

        public void AddManualSingle()
        {
            ManualMusicVideoViewModel newVM = new ManualMusicVideoViewModel(_album);
            AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(newVM);
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => parentVM)
                .Dismiss().ByClickingBackground()
                .TryShow();
        }

        public async void EditAlbum()
        {
            EditAlbumDialogViewModel editVM = new EditAlbumDialogViewModel(_album, Cover);
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => editVM)
                .TryShow();
        }

        protected virtual void OnSyncTrigger(bool isTaskStarted)
        {
            SyncStarted?.Invoke(this, isTaskStarted);
        }
    }
}