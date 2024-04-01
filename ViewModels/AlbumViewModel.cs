using MVNFOEditor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using ReactiveUI;
using System.Diagnostics;
using SukiUI.Controls;
using MVNFOEditor.Helpers;
using Newtonsoft.Json.Linq;
using SimpleInjector;
using Avalonia.Controls;

namespace MVNFOEditor.ViewModels
{
    public class AlbumViewModel : ReactiveObject
    {
        private ArtistListParentViewModel _parentVM;
        private AddMusicVideoParentViewModel _addMVVM;
        private readonly Album _album;
        public event EventHandler<bool> SyncStarted;
        private YTMusicHelper ytMusicHelper;

        public AlbumViewModel(Album album)
        {
            _parentVM = App.GetVM().GetParentView();
            _album = album;
            ytMusicHelper = App.GetYTMusicHelper();
            ShowVideoDownload = true;
            if (album.ytMusicBrowseID == null)
            {
                ShowVideoDownload = false;
            }
        }

        public Artist Artist => _album.Artist;

        public string Title => _album.Title;

        public string Year => _album.Year;

        public bool _showVideoDownload;

        public bool ShowVideoDownload
        {
            get => _showVideoDownload;
            private set => this.RaiseAndSetIfChanged(ref _showVideoDownload, value);
        }

        private List<MusicVideo> _songs;

        public List<MusicVideo> Songs
        {
            get => _songs;
            private set => this.RaiseAndSetIfChanged(ref _songs, value);
        }

        private Bitmap? _cover;

        public Bitmap? Cover
        {
            get => _cover;
            private set => this.RaiseAndSetIfChanged(ref _cover, value);
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
            Songs = dbContext.MusicVideos.Where(mv => mv.album.Title == Title).ToList();
        }
        public async void HandleSongClick(int songID)
        {
            var dbContext = App.GetDBContext();
            MusicVideo songObj = dbContext.MusicVideos.First(mv => mv.Id == songID);
            MusicVideoDetailsViewModel mvVM = new MusicVideoDetailsViewModel();
            mvVM.SetVideo(songObj);
            mvVM.AnalyzeVideo();
            await mvVM.LoadThumbnail();
            _parentVM.CurrentContent = mvVM;
        }

        public async void SyncAlbum()
        {
            OnSyncTrigger(true);
            string artistID = Artist.YTMusicId;
            JArray videoSearch = ytMusicHelper.get_videos(artistID);
            //Sometimes artists won't have any videos listed, so we need to handle this
            if (videoSearch == null)
            {
                OnSyncTrigger(false);
                TextBlock errBox = new TextBlock() { Text = "No Videos Available" };
                SukiHost.ShowDialog(errBox, allowBackgroundClose: true);
                return;
            }
            JObject fullAlbumDetails = ytMusicHelper.get_album(_album.ytMusicBrowseID);
            ObservableCollection<SyncResultViewModel> results = await ytMusicHelper.GenerateSyncResultList(videoSearch, fullAlbumDetails, Songs, _album);

            if (results.Count == 0)
            {
                OnSyncTrigger(false);
                TextBlock errBox = new TextBlock() { Text = "No Videos Available" };
                SukiHost.ShowDialog(errBox, allowBackgroundClose: true);
            }
            else
            {
                SyncDialogViewModel resultsVM = new SyncDialogViewModel(results);
                AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM);
                _addMVVM = parentVM;
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    result.ProgressStarted += parentVM.BuildProgressVM;
                }

                OnSyncTrigger(false);
                //Open Dialog
                SukiHost.ShowDialog(parentVM);
            }
        }

        public void AddManualSingle()
        {
            ManualMusicVideoViewModel newVM = new ManualMusicVideoViewModel(_album);
            AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(newVM);
            SukiHost.ShowDialog(parentVM, allowBackgroundClose: true);
        }

        public async void EditAlbum()
        {
            EditAlbumDialogViewModel editVM = new EditAlbumDialogViewModel(_album, Cover);
            SukiHost.ShowDialog(editVM, allowBackgroundClose: true);
        }

        protected virtual void OnSyncTrigger(bool isTaskStarted)
        {
            SyncStarted?.Invoke(this, isTaskStarted);
        }
    }
}