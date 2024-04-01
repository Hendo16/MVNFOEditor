using Material.Icons;
using MVNFOEditor.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using MVNFOEditor.DB;
using MVNFOEditor.Helpers;
using Avalonia.Media.Imaging;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using SukiUI.Controls;
using Avalonia.Controls;

namespace MVNFOEditor.ViewModels
{
    public partial class ArtistDetailsViewModel : ObservableValidator
    {
        private ArtistListParentViewModel _parentVM;
        private AddMusicVideoParentViewModel _addMVVM;
        private Artist _artist;
        private string _artistName;
        private MusicDBHelper DBHelper;
        private YTMusicHelper ytMusicHelper;
        private ArtistDetailsBannerViewModel _bannerVM;
        private NewAlbumDialogViewModel _currAlbumDialog;

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _busyText;
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
                    BannerVM = new ArtistDetailsBannerViewModel(Bitmap.DecodeToHeight(imageStream, 800));
                }
            }
            else
            {
                await using (var imageStream = await _artist.LoadLocalLargeBannerBitmapAsync())
                {
                    BannerVM = new ArtistDetailsBannerViewModel(Bitmap.DecodeToHeight(imageStream, 800));
                }
            }
        }

        public ArtistDetailsViewModel()
        {
            DBHelper = App.GetDBHelper();
            ytMusicHelper = App.GetYTMusicHelper();
            _parentVM = App.GetVM().GetParentView();
            _parentVM.SetDetailsVM(this);
        }

        public void AddAlbum()
        {
            if (_artist.YTMusicAlbumResults == null)
            {
                ManualAlbumViewModel manualVM = new ManualAlbumViewModel(_artist);
                NewAlbumDialogViewModel newAlbumVM = new NewAlbumDialogViewModel(manualVM, _artist);
                SukiHost.ShowToast("Error", "No YouTube Music Albums Available");
                SukiHost.ShowDialog(newAlbumVM);
                return;
            }
            JArray AlbumList = _artist.YTMusicAlbumResults;
            ObservableCollection<AlbumResultViewModel> results = new ObservableCollection<AlbumResultViewModel>();
            for (int i = 0; i < AlbumList.Count; i++)
            {
                var currAlbum = AlbumList[i];
                AlbumResult currResult = new AlbumResult();

                currResult.Title = currAlbum["title"].ToString();
                try { currResult.Year = currAlbum["year"].ToString(); } catch (NullReferenceException e) { }
                currResult.browseId = currAlbum["browseId"].ToString();
                currResult.thumbURL = App.GetYTMusicHelper().GetHighQualityArt((JObject)currAlbum);
                currResult.isExplicit = Convert.ToBoolean(currAlbum["isExplicit"]);
                currResult.Artist = _artist;
                AlbumResultViewModel newVM = new AlbumResultViewModel(currResult);
                newVM.LoadThumbnail();
                results.Add(newVM);
            }
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
            SukiHost.ShowDialog(parentVM);
        }

        private async void AddAlbumEventHandler(object? sender, AlbumResult _result)
        {
            await _currAlbumDialog.NextStep(null, _result);
        }

        public async void AddSingle()
        {
            BusyText = "Searching Youtube Music...";
            IsBusy = true;
            string artistID = _artist.YTMusicId;
            JArray videoSearch = ytMusicHelper.get_videos(artistID);
            //Sometimes artists won't have any videos listed, so we need to handle this
            if (videoSearch == null)
            {
                IsBusy = false;
                TextBlock errBox = new TextBlock() { Text = "No Videos Available" };
                SukiHost.ShowDialog(errBox, allowBackgroundClose: true);
                return;
            }
            ObservableCollection<SyncResultViewModel> results = await ytMusicHelper.GenerateSyncResultList(videoSearch, _artist);
            if (results.Count == 0)
            {
                IsBusy = false;
                TextBlock errBox = new TextBlock() { Text = "No Videos Available" };
                SukiHost.ShowDialog(errBox, allowBackgroundClose: true);
            }
            else
            {
                SyncDialogViewModel resultsVM = new SyncDialogViewModel(results);
                AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM);
                parentVM.RefreshAlbumEvent += RefreshDetailsView;
                _addMVVM = parentVM;
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    result.ProgressStarted += parentVM.BuildProgressVM;
                }
                IsBusy = false;
                //Open Dialog
                SukiHost.ShowDialog(parentVM);
            }
        }

        private void RefreshDetailsView(object? sender, bool e)
        {
            LoadAlbums();
            
        }

        public async void AddManualSingle()
        {
            ManualMusicVideoViewModel newVM = new ManualMusicVideoViewModel(_artist);
            AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(newVM);
            SukiHost.ShowDialog(parentVM, allowBackgroundClose: true);
        }

        public async void SetArtist(Artist artist)
        {
            _artist = artist;
            ArtistName = _artist.Name;
            LoadAlbums();
            await LoadBanner();
        }

        public async void LoadAlbums()
        {
            BusyText = "Loading Albums...";
            IsBusy = true;
            AlbumCards = await DBHelper.GenerateAlbums(_artist);
            SingleCards = await DBHelper.GenerateSingles(_artist);
            for (int i = 0; i < AlbumCards.Count; i++)
            {
                AlbumCards[i].SyncStarted += LoadingSync;
            }
            IsBusy = false;
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