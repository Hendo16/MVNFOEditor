using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;
using System.Collections.ObjectModel;
using MVNFOEditor.Helpers;
using Avalonia.Media.Imaging;
using Newtonsoft.Json.Linq;
using Avalonia.Controls.Notifications;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels
{
    public partial class ArtistDetailsViewModel : ObservableValidator
    {
        private ArtistListParentViewModel _parentVM;
        private Artist _artist;
        private string _artistName;
        private MusicDBHelper DBHelper;
        private YTMusicHelper ytMusicHelper;
        private iTunesAPIHelper _iTunesApiHelper;
        private ArtistDetailsBannerViewModel _bannerVM;
        private NewAlbumDialogViewModel _currAlbumDialog;
        private ISukiToastManager ToastManager;

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _hasAlbums;
        [ObservableProperty] private bool _hasSingles;
        [ObservableProperty] private string _busyText;
        [ObservableProperty] private SearchSource _source;
        [ObservableProperty] private ObservableCollection<AlbumViewModel> _albumCards;
        [ObservableProperty] private ObservableCollection<SingleViewModel> _singleCards;
        public string ArtistName
        {
            get { return _artistName; }
            set
            {
                _artistName = value;
                OnPropertyChanged(nameof(ArtistName));
            }
        }


        public ArtistDetailsBannerViewModel BannerVM
        {
            get { return _bannerVM; }
            set
            {
                _bannerVM = value;
                OnPropertyChanged(nameof(BannerVM));
            }
        }

        public async Task LoadBanner()
        {
            if (_artist.LargeBannerURL != null)
            {
                await using (var imageStream = await _artist.LoadLargeBannerBitmapAsync())
                {
                    BannerVM = new ArtistDetailsBannerViewModel(Bitmap.DecodeToHeight(imageStream, 800), this);
                }
            }
            else
            {
                await using (var imageStream = await _artist.LoadLocalLargeBannerBitmapAsync())
                {
                    BannerVM = new ArtistDetailsBannerViewModel(Bitmap.DecodeToHeight(imageStream, 800), this);
                }
            }
        }

        public ArtistDetailsViewModel()
        {
            DBHelper = App.GetDBHelper();
            ytMusicHelper = App.GetYTMusicHelper();
            _iTunesApiHelper = App.GetiTunesHelper();
            _parentVM = App.GetVM().GetParentView();
            _parentVM.SetDetailsVM(this);
            ToastManager = App.GetVM().GetToastManager();
        }

        public void AddAlbum()
        {
            //TODO: When adding album, how do we select a primary source??? Just assume AppleMusic for now
            ArtistMetadata artistMetadata = _artist.GetArtistMetadata(SearchSource.AppleMusic);
            switch (artistMetadata.SourceId)
            {
                case SearchSource.YouTubeMusic:
                    AddYTMusicAlbum();
                    break;
                case SearchSource.AppleMusic:
                    AddAMAlbum();
                    break;
            }
        }

        private async void AddAMAlbum()
        {
            ArtistMetadata artistMetadata = _artist.GetArtistMetadata(SearchSource.AppleMusic);
            if (artistMetadata.AlbumResults.Count == 0)
            {
                ManualAlbumViewModel manualVM = new ManualAlbumViewModel(_artist);
                NewAlbumDialogViewModel newAlbumVM = new NewAlbumDialogViewModel(manualVM, _artist);
                ToastManager.CreateToast()
                    .WithTitle("No Apple Music Albums Available")
                    .WithContent($"Please provide videos manually")
                    .OfType(NotificationType.Warning)
                    .Queue();
                App.GetVM().GetDialogManager().CreateDialog()
                    .WithViewModel(dialog => newAlbumVM)
                    .TryShow();
                return;
            }
            ObservableCollection<AlbumResultViewModel> results = await _iTunesApiHelper.GenerateAlbumResultList(_artist);
            AlbumResultsViewModel resultsVM = new AlbumResultsViewModel(results);
            NewAlbumDialogViewModel parentVM = new NewAlbumDialogViewModel(resultsVM, _artist);
            _currAlbumDialog = parentVM;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.NextPage += AddAlbumEventHandler;
            }
            parentVM.ClosePageEvent += ReturnToPreviousView;
            //Open Dialog
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => parentVM)
                .TryShow();
            
        }

        private async void AddYTMusicAlbum()
        {
            ArtistMetadata artistMetadata = _artist.GetArtistMetadata(SearchSource.YouTubeMusic);
            if (artistMetadata.AlbumResults.Count == 0)
            {
                ManualAlbumViewModel manualVM = new ManualAlbumViewModel(_artist);
                NewAlbumDialogViewModel newAlbumVM = new NewAlbumDialogViewModel(manualVM, _artist);
                ToastManager.CreateToast()
                    .WithTitle("No YouTube Music Albums Available")
                    .WithContent($"Please provide videos manually")
                    .OfType(NotificationType.Warning)
                    .Queue();
                App.GetVM().GetDialogManager().CreateDialog()
                    .WithViewModel(dialog => newAlbumVM)
                    .TryShow();
                return;
            }
            ObservableCollection<AlbumResultViewModel> results = await App.GetYTMusicHelper().GenerateAlbumResultList(_artist);
            AlbumResultsViewModel resultsVM = new AlbumResultsViewModel(results);
            NewAlbumDialogViewModel parentVM = new NewAlbumDialogViewModel(resultsVM, _artist);
            _currAlbumDialog = parentVM;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.NextPage += AddAlbumEventHandler;

            }
            parentVM.ClosePageEvent += ReturnToPreviousView;
            //Open Dialog
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => parentVM)
                .TryShow();
        }

        private async void AddAlbumEventHandler(object? sender, AlbumResult _result)
        {
            await _currAlbumDialog.NextStep(null, _result);
        }

        public async void AddYTMVideo()
        {
            BusyText = "Searching Youtube Music...";
            IsBusy = true;
            ArtistMetadata artistMetadata = _artist.GetArtistMetadata(SearchSource.YouTubeMusic);
            string artistID = artistMetadata.BrowseId;
            JArray videoSearch = ytMusicHelper.get_videos(artistID);
            //Sometimes artists won't have any videos listed, so we need to handle this
            if (videoSearch == null)
            {
                IsBusy = false;
                App.GetVM().GetDialogManager().CreateDialog()
                    .WithContent("No Videos Available")
                    .Dismiss().ByClickingBackground()
                    .TryShow();
                return;
            }
            ObservableCollection<VideoResultViewModel> results = await ytMusicHelper.GenerateVideoResultList(videoSearch, _artist);
            if (results.Count == 0)
            {
                IsBusy = false;
                App.GetVM().GetDialogManager().CreateDialog()
                    .WithContent("No Videos Available")
                    .Dismiss().ByClickingBackground()
                    .TryShow();
            }
            else
            {
                VideoResultsViewModel resultsVM = new VideoResultsViewModel(results);
                AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);
                parentVM.RefreshAlbumEvent += RefreshDetailsView;
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    result.ProgressStarted += parentVM.SaveYTMVideo;
                }
                IsBusy = false;
                //Open Dialog
                App.GetVM().GetDialogManager().CreateDialog()
                    .WithViewModel(dialog => parentVM)
                    .Dismiss().ByClickingBackground()
                    .TryShow();
            }
        }

        public async void AddAppleMusicVideo()
        {
            BusyText = "Searching Apple Music...";
            IsBusy = true;            
            ArtistMetadata artistMetadata = _artist.GetArtistMetadata(SearchSource.AppleMusic);
            string artistID = artistMetadata.BrowseId;
            JArray videoSearch = await _iTunesApiHelper.GetVideosByArtistId(artistID);
            //Sometimes artists won't have any videos listed, so we need to handle this
            if (videoSearch == null)
            {
                IsBusy = false;
                App.GetVM().GetDialogManager().CreateDialog()
                    .WithContent("No Videos Available")
                    .Dismiss().ByClickingBackground()
                    .TryShow();
                return;
            }
            ObservableCollection<VideoResultViewModel> results = await _iTunesApiHelper.GenerateVideoResultList(videoSearch, _artist);
            if (results.Count == 0)
            {
                IsBusy = false;
                App.GetVM().GetDialogManager().CreateDialog()
                    .WithContent("No Videos Available")
                    .Dismiss().ByClickingBackground()
                    .TryShow();
            }
            else
            {
                VideoResultsViewModel resultsVM = new VideoResultsViewModel(results);
                AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);
                parentVM.RefreshAlbumEvent += RefreshDetailsView;
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    result.ProgressStarted += parentVM.SaveYTMVideo;
                }
                IsBusy = false;
                //Open Dialog
                App.GetVM().GetDialogManager().CreateDialog()
                    .WithViewModel(dialog => parentVM)
                    .Dismiss().ByClickingBackground()
                    .TryShow();
            }
        }
        
        private async void RefreshDetailsView(object? sender, bool e)
        {
            await LoadAlbums();
        }

        public async void AddManualVideo()
        {
            ManualMusicVideoViewModel newVM = new ManualMusicVideoViewModel(_artist);
            AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(newVM);
            App.GetVM().GetDialogManager().CreateDialog()
                .WithViewModel(dialog => parentVM)
                .Dismiss().ByClickingBackground()
                .TryShow();
        }

        public async void SetArtist(Artist artist)
        {
            _artist = artist;
            //TODO: When adding album, how do we select a primary source??? Just assume AppleMusic for now
            ArtistMetadata artistMetadata = _artist.GetArtistMetadata(SearchSource.AppleMusic);
            Source = artistMetadata.SourceId;
            ArtistName = _artist.Name;
            await LoadAlbums();
            await LoadBanner();
        }

        public async Task<bool> LoadAlbums()
        {
            BusyText = "Loading Albums...";
            IsBusy = true;
            AlbumCards = await DBHelper.GenerateAlbums(_artist);
            SingleCards = await DBHelper.GenerateSingles(_artist);
            for (int i = 0; i < AlbumCards.Count; i++)
            {
                AlbumCards[i].SyncStarted += LoadingSync;
            }
            HasAlbums = AlbumCards.Count > 0;
            HasSingles = SingleCards.Count > 0;
            IsBusy = false;
            return true;
        }

        private void LoadingSync(object sender, bool isSyncTriggered)
        {
            BusyText = "Searching Youtube Music...";
            IsBusy = isSyncTriggered;
        }

        public void ClearImages()
        {
            for (int i = 0; i < AlbumCards.Count; i++)
            {
                if (AlbumCards[i].Cover != null)
                {
                    AlbumCards[i].Cover.Dispose();
                }
            }
            for (int i = 0; i < SingleCards.Count; i++)
            {
                if (SingleCards[i].Thumbnail != null)
                {
                    SingleCards[i].Thumbnail.Dispose();
                }
            }
            AlbumCards.Clear();
            SingleCards.Clear();
            BannerVM.ArtistBanner.Dispose();
        }


        public void ReturnToPreviousView(object? sender, bool t)
        {
            //_parentVM.BackToList(true);
            LoadAlbums();
        }
    }
}