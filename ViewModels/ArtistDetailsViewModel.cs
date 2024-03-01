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
        private ObservableCollection<AlbumViewModel> _albumCards;
        private ObservableCollection<SingleViewModel> _singleCards;
        private MusicDBHelper DBHelper;
        private YTMusicHelper ytMusicHelper;
        private ArtistDetailsBannerViewModel _bannerVM;

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _busyText;


        public ObservableCollection<AlbumViewModel> AlbumCards
        {
            get {return _albumCards; }
            set
            {
                _albumCards = value;
                OnPropertyChanged(nameof(AlbumCards));
            }
        }
        public ObservableCollection<SingleViewModel> SingleCards
        {
            get { return _singleCards; }
            set
            {
                _singleCards = value;
                OnPropertyChanged(nameof(SingleCards));
            }
        }

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
                    BannerVM = new ArtistDetailsBannerViewModel(new Bitmap(imageStream));
                }
            }
            else
            {
                await using (var imageStream = await _artist.LoadLocalLargeBannerBitmapAsync())
                {
                    BannerVM = new ArtistDetailsBannerViewModel(new Bitmap(imageStream));
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
                TextBlock errBox = new TextBlock() { Text = "No Albums Available" };
                SukiHost.ShowDialog(errBox, allowBackgroundClose: true);
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
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.NextPage += parentVM.NextStep;

            }
            parentVM.ClosePageEvent += ReturnToPreviousView;
            //Open Dialog
            SukiHost.ShowDialog(parentVM);
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
                SukiHost.ShowDialog(parentVM, allowBackgroundClose: true);
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

        public void NavigateBack()
        {
            _parentVM.BackToList();
        }
        
        public void ReturnToPreviousView(object? sender, bool t)
        {
            _parentVM.BackToList(true);
        }
    }
}