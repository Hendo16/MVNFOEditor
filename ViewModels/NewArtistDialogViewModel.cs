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
        private SyncDialogViewModel? _syncDialogParentVM;

        private ManualArtistViewModel? _manualArtVM;
        private ManualAlbumViewModel? _manualAlbumVM;
        private ManualMusicVideoViewModel? _manualMusicVideoVM;

        private YTMusicHelper ytMusicHelper;
        private YTDLHelper _ytDLHelper;
        private MusicDbContext _dbContext;
        private SettingsData localData;
        private Artist _artist;

        [ObservableProperty] private int _stepIndex;
        [ObservableProperty] private bool _toggleEnable;
        [ObservableProperty] private bool _toggleVisible;
        [ObservableProperty] private bool _toggleValue;
        [ObservableProperty] private bool _isAlbumView;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _busyText;
        [ObservableProperty] private string _skipText;
        [ObservableProperty] private string _backButtonText;
        [ObservableProperty] private object _currentContent;
        [ObservableProperty] private ObservableCollection<string> _steps;

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
            Steps = ["Select Artist", "Select Album", "Select Videos"];
            SkipText = "Skip";
            BackButtonText = "Exit";
            ToggleEnable = true;
            ToggleVisible = true;
            ToggleValue = true;
            IsAlbumView = false;
            CurrentContent = _resultVM;
        }

        public async void NextStep(object? sender, ArtistResult newArtist)
        {
            BusyText = "Getting Albums...";
            IsBusy = true;
            //Prevent duplicates being stored
            if (!_dbContext.Artist.Any(a => a.YTMusicId == newArtist.browseId))
            {
                _artist = new Artist();
                _artist.Name = newArtist.Name;
                _artist.YTMusicId = newArtist.browseId;
                _artist.CardBannerURL = ytMusicHelper.GetArtistBanner(_artist.YTMusicId, 540);
                _artist.LargeBannerURL = ytMusicHelper.GetArtistBanner(_artist.YTMusicId, 1080);
                _artist.YTMusicAlbumResults = ytMusicHelper.GetAlbums(_artist.YTMusicId);
                _dbContext.Artist.Add(_artist);
                _dbContext.SaveChanges();
            }
            else
            {
                _artist = _dbContext.Artist.First(a => a.YTMusicId == newArtist.browseId);
            }
            
            //Refresh list to display the new artist
            _parentVM.RefreshList();

            if(_artist.YTMusicAlbumResults == null){ToSingles();return;}

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
            IsBusy = false;
            CurrentContent = resultsVM;
        }

        public async void ToSingles()
        {
            BusyText = "Getting Videos...";
            SkipText = "Exit";
            StepIndex++;
            ToggleVisible = false;
            string artistID = _artist.YTMusicId;
            JArray videoSearch = ytMusicHelper.get_videos(artistID);
            ObservableCollection<SyncResultViewModel> results = await ytMusicHelper.GenerateSyncResultList(videoSearch, _artist);
            SyncDialogViewModel resultsVM = new SyncDialogViewModel(results);
            AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM);
            _syncDialogParentVM = resultsVM;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.ProgressStarted += parentVM.BuildProgressVM;
            }
            IsBusy = false;
            CurrentContent = resultsVM;
        }

        public async void ToVideos(object? sender, AlbumResult newAlbum)
        {
            BusyText = "Getting Videos...";
            IsBusy = true;
            Album album = new Album();
            if (!_dbContext.Album.Any(a => a.ytMusicBrowseID == newAlbum.browseId))
            {
                album = new Album(newAlbum);
                _dbContext.Album.Add(album);
                _dbContext.SaveChanges();
            }
            else
            {
                album = _dbContext.Album.First(a => a.ytMusicBrowseID == newAlbum.browseId);
            }

            string artistID = album.Artist.YTMusicId;
            JArray videoSearch = ytMusicHelper.get_videos(artistID);
            JObject fullAlbumDetails = ytMusicHelper.get_album(album.ytMusicBrowseID);
            ObservableCollection<SyncResultViewModel> results = await ytMusicHelper.GenerateSyncResultList(videoSearch, fullAlbumDetails, null, album);
            SyncDialogViewModel resultsVM = new SyncDialogViewModel(results);
            AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM);
            _syncDialogParentVM = resultsVM;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.ProgressStarted += parentVM.BuildProgressVM;
            }
            IsBusy = false;
            CurrentContent = resultsVM;
        }

        public void ManualNextStep()
        {
            Artist newArtist = new Artist();
            newArtist.Name = _manualArtVM.ArtistNameText;
            if (_manualArtVM.BannerPath != null)
            {
                newArtist.SaveManualBanner(_manualArtVM.BannerPath);
            }
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
            if (_manualAlbumVM.CoverPath != null)
            {
                newAlbum.SaveManualCover(_manualAlbumVM.CoverPath);
            }

            _dbContext.Album.Add(newAlbum);
            _dbContext.SaveChanges();

            ManualMusicVideoViewModel newVM = new ManualMusicVideoViewModel(newAlbum);
            _manualMusicVideoVM = newVM;
            CurrentContent = newVM;
        }

        public async void ManualSaveVideo()
        {
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            waveVM.HeaderText = "Downloading " + _manualMusicVideoVM.Title;
            CurrentContent = waveVM;
            var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress));
            RunResult<string> downloadResult = await _ytDLHelper.DownloadVideo(_manualMusicVideoVM._vidData.ID, $"{localData.RootFolder}/{_artist.Name}", _manualMusicVideoVM.Title, progress);
            if (downloadResult.Success)
            {
                GenerateManualNFO(downloadResult.Data);
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
            //Get Current Page
            Type currentType = CurrentContent.GetType();
            switch (isIncrement)
            {
                case true when StepIndex > Steps.Count() - 1:
                case false when StepIndex <= 0:
                    if(_artist != null){ HandleExit(); } else{ SukiHost.CloseDialog(); }
                    return;
                default:
                    StepIndex += isIncrement ? 1 : -1;
                    BackButtonText = StepIndex != 0 ? "Back" : "Exit";
                    break;
            }
            #region Manual Navigation
            if (currentType == typeof(ManualArtistViewModel))
            {
                ToggleVisible = false;
                IsAlbumView = true;
                ManualNextStep();
            }
            if (currentType == typeof(ManualAlbumViewModel))
            {
                IsAlbumView = false;
                if (isIncrement)
                {
                    ToggleVisible = false;
                    SkipText = "Exit";
                    ManualToVideos();
                }
                else
                {
                    ToggleEnable = true;
                    CurrentContent = ToggleVisible ? _resultVM : _manualArtVM;
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
                    ToggleVisible = true;
                    IsAlbumView = true;
                    SkipText = "Skip";
                    CurrentContent = _manualAlbumVM;
                }
            }
            #endregion

            #region Auto Navigation
            if (currentType == typeof(ArtistResultsViewModel))
            {
                IsAlbumView = true;
                NextStep(null, _resultVM.SelectedArtist.GetResult());
            }

            if (currentType == typeof(AlbumResultsViewModel))
            {
                IsAlbumView = false;
                if (isIncrement)
                {
                    ToggleVisible = false;
                    SkipText = "Exit";
                    ToVideos(null, _albumResultsVM.SelectedAlbum.GetResult());
                }
                else
                {
                    CurrentContent = _resultVM;
                }
            }

            if (currentType == typeof(SyncDialogViewModel))
            {
                if (isIncrement)
                {
                    SaveMultipleVideos();
                }
                else
                {
                    ToggleVisible = true;
                    SkipText = "Skip";
                    CurrentContent = _albumResultsVM;
                }
            }
            #endregion
        }
        private async void SaveMultipleVideos()
        {
            SettingsData localData = _dbContext.SettingsData.First();
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            IsAlbumView = false;
            CurrentContent = waveVM;
            for (int i = 0; i < _syncDialogParentVM.SelectedVideos.Count; i++)
            {
                if (_syncDialogParentVM.SelectedVideos.Count > 1)
                {
                    waveVM.HeaderText = $"Downloading {_syncDialogParentVM.SelectedVideos[i].Title} {i + 1}/{_syncDialogParentVM.SelectedVideos.Count}";
                }
                else
                {
                    waveVM.HeaderText = $"Downloading {_syncDialogParentVM.SelectedVideos[i].Title}";
                }
                var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress));
                var currResult = _syncDialogParentVM.SelectedVideos[i].HandleDownload();
                var downResult = await _ytDLHelper.DownloadVideo(currResult.vidID, $"{localData.RootFolder}/{currResult.Artist.Name}", currResult.Title, progress);
                if (downResult.Success)
                {
                    _syncDialogParentVM.SelectedVideos[i].GenerateNFO(downResult.Data).ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            waveVM.UpdateProgress(0);
                        }
                        else
                        {
                            SukiHost.ShowToast("Error", $"Something went wrong with downloading {_syncDialogParentVM.SelectedVideos[i].Title}");
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    SukiHost.ShowToast("Error", $"Something went wrong with downloading {_syncDialogParentVM.SelectedVideos[i].Title}");
                    break;
                }
            }

            HandleExit();
        }

        public async void HandleSkip()
        {
            if (CurrentContent.GetType() == typeof(ManualMusicVideoViewModel) || CurrentContent.GetType() == typeof(SyncDialogViewModel))
            {
                HandleExit();
            }
            ToggleVisible = false;
            StepIndex++;

            //Automatic
            if (ToggleValue)
            {
                string artistID = _artist.YTMusicId;
                JArray videoSearch = ytMusicHelper.get_videos(artistID);
                ObservableCollection<SyncResultViewModel> results = await ytMusicHelper.GenerateSyncResultList(videoSearch, _artist);
                SyncDialogViewModel resultsVM = new SyncDialogViewModel(results);
                AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM);
                _syncDialogParentVM = resultsVM;
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    result.ProgressStarted += parentVM.BuildProgressVM;
                }
                CurrentContent = resultsVM;
            }
            else
            {
                ManualMusicVideoViewModel newVM = new ManualMusicVideoViewModel(_artist);
                _manualMusicVideoVM = newVM;
                CurrentContent = newVM;
            }
        }

        public void HandleExit()
        {
            var defaultVM = App.GetVM().GetParentView();
            ArtistDetailsViewModel artDetailsVM = new ArtistDetailsViewModel();
            artDetailsVM.SetArtist(_artist);
            defaultVM.SetDetailsVM(artDetailsVM);
            defaultVM.CurrentContent = artDetailsVM;
            CloseDialog();
        }

        public void HandleChangedMode(bool? changeValue)
        {
            Type currentType = CurrentContent.GetType();
            if (currentType == typeof(ArtistResultsViewModel) || currentType == typeof(ManualArtistViewModel))
            {
                if ((bool)!changeValue)
                {
                    ManualArtistViewModel newVM = new ManualArtistViewModel();
                    Steps = ["Create Artist", "Create Album", "Create Video"];
                    CurrentContent = newVM;
                    _manualArtVM = newVM;
                }
                else
                {
                    Steps = ["Select Artist", "Select Album", "Select Videos"];
                    CurrentContent = _resultVM;
                }
            }

            if (currentType == typeof(AlbumResultsViewModel) || currentType == typeof(ManualAlbumViewModel))
            {
                if ((bool)!changeValue)
                {
                    ManualAlbumViewModel newVM = new ManualAlbumViewModel(_artist);
                    Steps = ["Create Artist", "Create Album", "Create Video"];
                    CurrentContent = newVM;
                    _manualAlbumVM = newVM;
                }
                else
                {
                    Steps = ["Select Artist", "Select Album", "Select Videos"];

                    CurrentContent = _albumResultsVM;
                }
            }
        }

        private async void GenerateManualNFO(string vidPath)
        {
            MusicVideo newMV = new MusicVideo();
            newMV.title = _manualMusicVideoVM.Title;
            newMV.year = _manualMusicVideoVM.Year;
            newMV.artist = _artist;
            newMV.vidPath = vidPath;
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
            newMV.nfoPath = $"{localData.RootFolder}/{newMV.artist.Name}/{newMV.title}-video.nfo";

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
