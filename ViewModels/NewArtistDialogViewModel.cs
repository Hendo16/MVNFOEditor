using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVNFOEditor.DB;
using MVNFOEditor.Helpers;
using CommunityToolkit.Mvvm.Input;
using MVNFOEditor.Models;
using Newtonsoft.Json.Linq;
using SukiUI.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using MVNFOEditor.Views;
using YoutubeDLSharp;

namespace MVNFOEditor.ViewModels
{
    public partial class NewArtistDialogViewModel : ObservableObject
    {
        private ArtistListParentViewModel _parentVM;
        private ArtistResultsViewModel _resultVM;
        private AlbumResultsViewModel? _albumResultsVM;
        private AddMusicVideoParentViewModel? _syncDialogParentVM;

        private ManualArtistViewModel? _manualArtVM;
        private ManualAlbumViewModel? _manualAlbumVM;
        private ManualMusicVideoViewModel? _manualMusicVideoVM;

        private YTMusicHelper ytMusicHelper;
        private YTDLHelper _ytDLHelper;
        private MusicDbContext _dbContext;
        private SettingsData localData;
        private Artist _artist;


        public IEnumerable<string> Steps { get; } = [
        "Select Artist",
        "Select Albums",
        "Select Videos"
        ];

        private bool _toggleEnable;

        public bool ToggleEnable
        {
            get { return _toggleEnable; }
            set
            {
                _toggleEnable = value;
                OnPropertyChanged(nameof(ToggleEnable));
            }
        }

        private bool _toggleValue;

        public bool ToggleValue
        {
            get { return _toggleValue; }
            set
            {
                _toggleValue = value;
                OnPropertyChanged(nameof(ToggleValue));
            }
        }

        private bool _isAlbumView;

        public bool IsAlbumView
        {
            get {return _isAlbumView;}
            set
            {
                _isAlbumView = value;
                OnPropertyChanged(nameof(IsAlbumView));
            }
        }

        private bool _nextEnabled;

        public bool NextEnabled
        {
            get { return _nextEnabled; }
            set
            {
                _nextEnabled = value;
                OnPropertyChanged(nameof(NextEnabled));
            }
        }

        private string _backButtonText;
        public string BackButtonText
        {
            get { return _backButtonText; }
            set
            {
                _backButtonText = value;
                OnPropertyChanged(nameof(BackButtonText));
            }
        }

        private object _currentContent;
        public object CurrentContent
        {
            get { return _currentContent; }
            set
            {
                _currentContent = value;
                OnPropertyChanged(nameof(CurrentContent));
            }
        }
        private int _stepIndex = 0;
        public int StepIndex
        {
            get { return _stepIndex; }
            set
            {
                _stepIndex = value;
                OnPropertyChanged(nameof(StepIndex));
            }
        }

        public NewArtistDialogViewModel()
        {
            _albumResultsVM = null;
            _syncDialogParentVM = null;
            _resultVM = new ArtistResultsViewModel();
            _resultVM.NextPage += NextStep;
            ytMusicHelper = App.GetYTMusicHelper();
            _ytDLHelper = App.GetYTDLHelper();
            _dbContext = App.GetDBContext();
            _parentVM = App.GetVM().GetParentView();
            localData = _dbContext.SettingsData.First();
            BackButtonText = "Exit";
            ToggleEnable = true;
            ToggleValue = true;
            IsAlbumView = false;
            NextEnabled = false;
            CurrentContent = _resultVM;
        }

        public async void NextStep(object? sender, ArtistResult newArtist)
        {
            HandleNavigation(true);

            _artist = new Artist();
            _artist.Name = newArtist.Name;
            _artist.YTMusicId = newArtist.browseId;
            _artist.CardBannerURL = ytMusicHelper.GetArtistBanner(_artist.YTMusicId, 540);
            _artist.LargeBannerURL = ytMusicHelper.GetArtistBanner(_artist.YTMusicId, 1080);
            _artist.YTMusicAlbumResults = ytMusicHelper.GetAlbums(_artist.YTMusicId);
            _dbContext.Artist.Add(_artist);
            _dbContext.SaveChanges();

            //Refresh list to display the new artist
            _parentVM.RefreshList();

            if(_artist.YTMusicAlbumResults == null){ToSingles();}

            JArray AlbumList = _artist.YTMusicAlbumResults;
            ObservableCollection<AlbumResultViewModel> results = new ObservableCollection<AlbumResultViewModel>();
            for (int i = 0; i < AlbumList.Count; i++)
            {
                var currAlbum = AlbumList[i];
                AlbumResult currResult = new AlbumResult();

                currResult.Title = currAlbum["title"].ToString();
                try{ currResult.Year = currAlbum["year"].ToString(); } catch (NullReferenceException e){}
                currResult.browseId = currAlbum["browseId"].ToString();
                currResult.thumbURL = App.GetYTMusicHelper().GetHighQualityArt((JObject)currAlbum);
                currResult.isExplicit = Convert.ToBoolean(currAlbum["isExplicit"]);
                currResult.Artist = _artist;
                AlbumResultViewModel newVM = new AlbumResultViewModel(currResult);
                await newVM.LoadThumbnail();
                results.Add(newVM);
            }
            AlbumResultsViewModel resultsVM = new AlbumResultsViewModel(results);
            _albumResultsVM = resultsVM;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.NextPage += ToVideos;
            }
            CurrentContent = resultsVM;
        }

        public async void ToSingles()
        {
            StepIndex++;
            string artistID = _artist.YTMusicId;
            JArray videoSearch = ytMusicHelper.get_videos(artistID);
            ObservableCollection<SyncResultViewModel> results = await ytMusicHelper.GenerateSyncResultList(videoSearch, _artist);
            SyncDialogViewModel resultsVM = new SyncDialogViewModel(results);
            AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM);
            _syncDialogParentVM = parentVM;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.ProgressStarted += parentVM.BuildProgressVM;
            }
            CurrentContent = parentVM;
        }

        public async void ToVideos(object? sender, AlbumResult newAlbum)
        {
            HandleNavigation(true);

            Album album = new Album(newAlbum);
            _dbContext.Album.Add(album);
            _dbContext.SaveChanges();

            string artistID = album.Artist.YTMusicId;
            JArray videoSearch = ytMusicHelper.get_videos(artistID);
            JObject fullAlbumDetails = ytMusicHelper.get_album(album.ytMusicBrowseID);
            ObservableCollection<SyncResultViewModel> results = await ytMusicHelper.GenerateSyncResultList(videoSearch, fullAlbumDetails, null, album);
            SyncDialogViewModel resultsVM = new SyncDialogViewModel(results);
            AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM);
            _syncDialogParentVM = parentVM;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.ProgressStarted += parentVM.BuildProgressVM;
            }
            CurrentContent = parentVM;
        }

        public void ManualNextStep()
        {
            Artist newArtist = new Artist();
            newArtist.Name = _manualArtVM.ArtistNameText;
            newArtist.SaveManualBanner(_manualArtVM.BannerPath);
            _artist = newArtist;
            _dbContext.Artist.Add(_artist);
            _dbContext.SaveChanges();

            ManualAlbumViewModel newVM = new ManualAlbumViewModel(newArtist);
            _manualAlbumVM = newVM;
            CurrentContent = newVM;
        }

        public void ManualToVideos()
        {
            Album newAlbum = new Album();
            newAlbum.Artist = _artist;
            newAlbum.Title = _manualAlbumVM.AlbumNameText;
            newAlbum.Year = _manualAlbumVM.AlbumYear;
            newAlbum.SaveManualCover(_manualAlbumVM.CoverPath);

            _dbContext.Album.Add(newAlbum);
            _dbContext.SaveChanges();

            ManualMusicVideoViewModel newVM = new ManualMusicVideoViewModel(newAlbum);
            _manualMusicVideoVM = newVM;
            CurrentContent = newVM;
        }

        public async void ManualSaveVideo()
        {
            GenerateManualNFO();
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            waveVM.HeaderText = "Downloading " + _manualMusicVideoVM.Title;
            CurrentContent = waveVM;
            var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress));
            RunResult<string> downloadResult = await _ytDLHelper.DownloadVideo(_manualMusicVideoVM._vidData.ID, $"{localData.RootFolder}/{_artist.Name}", _manualMusicVideoVM.Title, progress);
            if (downloadResult.Success)
            {
                _manualMusicVideoVM.ClearData();
                CurrentContent = _manualMusicVideoVM;
            }
            else
            {
                string[] errorContent = downloadResult.ErrorOutput;
                SukiHost.ShowToast("Download Error", errorContent[0]);
            }
        }

        [RelayCommand]
        public void HandleNavigation(bool isIncrement)
        {
            if (!isIncrement && !NextEnabled){ NextEnabled = true; }
            //Get Current Page
            Type currentType = CurrentContent.GetType();
            switch (isIncrement)
            {
                case true when StepIndex >= Steps.Count() - 1:
                case false when StepIndex <= 0:
                    SukiHost.CloseDialog();
                    return;
                default:
                    StepIndex += isIncrement ? 1 : -1;
                    BackButtonText = StepIndex != 0 ? "Back" : "Exit";
                    break;
            }
            #region Manual Navigation
            if (currentType == typeof(ManualArtistViewModel))
            {
                ToggleEnable = false;
                IsAlbumView = true;
                ManualNextStep();
            }
            if (currentType == typeof(ManualAlbumViewModel))
            {
                IsAlbumView = false;
                if (isIncrement)
                {
                    //NextEnabled = false;
                    ManualToVideos();
                }
                else
                {
                    ToggleEnable = true;
                    CurrentContent = _manualArtVM;
                }
            }
            if (currentType == typeof(ManualMusicVideoViewModel))
            {
                if (isIncrement)
                {
                    ManualSaveVideo();
                }
                else
                {
                    IsAlbumView = true;
                    CurrentContent = _manualAlbumVM;
                }
            }
            #endregion

            #region Auto Navigation
            if (currentType == typeof(ArtistResultsViewModel))
            {
                ToggleEnable = false;
                IsAlbumView = true;
                CurrentContent = _albumResultsVM;
            }
            if (currentType == typeof(AlbumResultsViewModel))
            {
                IsAlbumView = false;
                if (isIncrement)
                {
                    NextEnabled = false;
                    CurrentContent = _syncDialogParentVM;
                }
                else
                {
                    ToggleEnable = true;
                    CurrentContent = _resultVM;
                }
            }
            if (currentType == typeof(AddMusicVideoParentViewModel))
            {
                if (_albumResultsVM != null)
                {
                    IsAlbumView = true;
                    CurrentContent = _albumResultsVM;
                }
                else
                {
                    StepIndex--;
                    CurrentContent = _resultVM;
                }
            }
            #endregion
        }

        public async void HandleSkip()
        {
            //Automatic
            if (ToggleValue)
            {
                string artistID = _artist.YTMusicId;
                JArray videoSearch = ytMusicHelper.get_videos(artistID);
                ObservableCollection<SyncResultViewModel> results = await ytMusicHelper.GenerateSyncResultList(videoSearch, _artist);
                SyncDialogViewModel resultsVM = new SyncDialogViewModel(results);
                AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM);
                _syncDialogParentVM = parentVM;
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    result.ProgressStarted += parentVM.BuildProgressVM;
                }
                CurrentContent = parentVM;
            }
            else
            {
                ManualMusicVideoViewModel newVM = new ManualMusicVideoViewModel(_artist);
                _manualMusicVideoVM = newVM;
                CurrentContent = newVM;
            }
        }

        public void HandleChangedMode(bool? changeValue)
        {
            if ((bool)!changeValue)
            {
                ManualArtistViewModel newVM = new ManualArtistViewModel();
                CurrentContent = newVM;
                _manualArtVM = newVM;
                NextEnabled = true;
            }
            else
            {
                CurrentContent = _resultVM;
            }
        }

        private async void GenerateManualNFO()
        {
            MusicVideo newMV = new MusicVideo();
            newMV.title = _manualMusicVideoVM.Title;
            newMV.year = _manualMusicVideoVM.Year;
            newMV.artist = _artist;
            if (_manualMusicVideoVM.CurrAlbum != null)
            {
                newMV.album = _manualMusicVideoVM.CurrAlbum;
            }
            else
            {
                newMV.album = null;
            }

            if (_manualMusicVideoVM._vidData != null)
            {
                newMV.videoID = _manualMusicVideoVM._vidData.ID;
                newMV.source = "youtube";
            }
            else
            {
                newMV.source = "local";
            }
            newMV.filePath = $"{localData.RootFolder}/{newMV.artist.Name}/{newMV.title}-video.nfo";

            await _manualMusicVideoVM.SaveThumbnailAsync($"{localData.RootFolder}/{newMV.artist.Name}");
            newMV.thumb = $"{newMV.title}-video.jpg";

            newMV.SaveToNFO();
            _dbContext.MusicVideos.Add(newMV);
            _dbContext.SaveChanges();
        }

        public void BackTrigger(){HandleNavigation(false);}
        public void NextTrigger(){ HandleNavigation(true); }
                
        [RelayCommand]
        public void CloseDialog() => SukiHost.CloseDialog();
    }
}
