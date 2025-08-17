using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MVNFOEditor.DB;
using MVNFOEditor.Helpers;
using CommunityToolkit.Mvvm.Input;
using MVNFOEditor.Models;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.IO;
using YoutubeDLSharp;
using Avalonia.Threading;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using log4net;
using Microsoft.EntityFrameworkCore;
using MVNFOEditor.Settings;
using SukiUI.Controls;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels
{
    public partial class NewArtistDialogViewModel : ObservableObject
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AddMusicVideoParentViewModel));
        private ArtistListParentViewModel _parentVM;
        private ArtistResultsViewModel _resultVM;
        private AlbumResultsViewModel? _albumResultsVM;
        private VideoResultsViewModel? _videoResultsParentVM;

        private ManualArtistViewModel? _manualArtVM;
        private ManualAlbumViewModel? _manualAlbumVM;
        private ManualMusicVideoViewModel? _manualMusicVideoVM;

        private iTunesAPIHelper _iTunesApiHelper;
        private YTMusicHelper ytMusicHelper;
        private YTDLHelper _ytDLHelper;
        private AppleMusicDLHelper _amDLHelper;
        private MusicDbContext _dbContext;
        private static ISettings _settings;
        private Artist _artist;

        [ObservableProperty] private int _stepIndex;
        [ObservableProperty] private bool _toggleEnable;
        [ObservableProperty] private bool _toggleVisible;
        [ObservableProperty] private bool _stepperEnabled;
        [ObservableProperty] private bool _toggleValue;
        [ObservableProperty] private bool _isAlbumView;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _navVisible;
        [ObservableProperty] private bool _showError;
        [ObservableProperty] private string _busyText;
        [ObservableProperty] private string _errorText;
        [ObservableProperty] private string _backButtonText;
        [ObservableProperty] private string _nextButtonText;
        [ObservableProperty] private object _currentContent;
        [ObservableProperty] private ObservableCollection<string> _steps;
        public event EventHandler<bool> ClosePageEvent;

        public NewArtistDialogViewModel()
        {
            _albumResultsVM = null;
            _videoResultsParentVM = null;
            _resultVM = new ArtistResultsViewModel(SearchSource.YouTubeMusic);
            _resultVM.NextPage += NextStep;
            _resultVM.ValidSearch += ValidSearch;
            ytMusicHelper = App.GetYTMusicHelper();
            _ytDLHelper = App.GetYTDLHelper();
            _amDLHelper = App.GetAppleMusicDLHelper();
            _iTunesApiHelper = App.GetiTunesHelper();
            _dbContext = App.GetDBContext();
            _settings = App.GetSettings();
            _parentVM = App.GetVM().GetParentView();
            Steps = ["Select Artist", "Select Album", "Select Videos"];
            BackButtonText = "Exit";
            NextButtonText = "Next";
            ToggleEnable = true;
            ToggleVisible = true;
            NavVisible = true;
            ToggleValue = true;
            IsAlbumView = false;
            CurrentContent = _resultVM;
        }

        public void ValidSearch(object? sender, bool result)
        {
            NavVisible = result;
        }

        public void BackTrigger(){HandleNavigation(false);}
        public void NextTrigger(){ HandleNavigation(true); }
        
        #region StepperChecked

        public void ManualChecked()
        {
            Type currentType = CurrentContent.GetType();
            //Artist Creation
            if (currentType == typeof(ArtistResultsViewModel) || currentType == typeof(ManualArtistViewModel))
            {
                ManualArtistViewModel newVM = new ManualArtistViewModel();
                Steps = ["Create Artist", "Create Album", "Create Video"];
                CurrentContent = newVM;
                _manualArtVM = newVM;
            }
            //Album Creation
            if (currentType == typeof(AlbumResultsViewModel) || currentType == typeof(ManualAlbumViewModel))
            {
                ManualAlbumViewModel newVM = new ManualAlbumViewModel(_artist);
                Steps = ["Create Artist", "Create Album", "Create Video"];
                CurrentContent = newVM;
                _manualAlbumVM = newVM;
            }
        }

        public void YouTubeChecked()
        {
            Type currentType = CurrentContent.GetType();
            //Artist Creation
            if (currentType == typeof(ArtistResultsViewModel) || currentType == typeof(ManualArtistViewModel))
            {
                Steps = ["Select Artist", "Select Album", "Select Videos"];
                _resultVM.selectedSource = SearchSource.YouTubeMusic;
                _resultVM.SearchResults.Clear();
                CurrentContent = _resultVM;
            }

            //Album Creation
            if (currentType == typeof(AlbumResultsViewModel) || currentType == typeof(ManualAlbumViewModel))
            {
                Steps = ["Select Artist", "Select Album", "Select Videos"];
                CurrentContent = _albumResultsVM;
            }
        }
        public void AppleMusicChecked()
        {
            Type currentType = CurrentContent.GetType();
            //Artist Creation
            if (currentType == typeof(ArtistResultsViewModel) || currentType == typeof(ManualArtistViewModel))
            {
                Steps = ["Select Artist", "Select Album", "Select Videos"];
                _resultVM.selectedSource = SearchSource.AppleMusic;
                _resultVM.SearchResults.Clear();
                CurrentContent = _resultVM;
            }

            //Album Creation
            if (currentType == typeof(AlbumResultsViewModel) || currentType == typeof(ManualAlbumViewModel))
            {
                Steps = ["Select Artist", "Select Album", "Select Videos"];
                CurrentContent = _albumResultsVM;
            }
        }
        #endregion
        
        #region Navigation

        [RelayCommand]
        public void HandleNavigation(bool isIncrement)
        {
            //Undo Errors
            ShowError = false;
            ErrorText = "";
            //Get Current Page
            Type currentType = CurrentContent.GetType();
            switch (isIncrement)
            {
                case true when StepIndex > Steps.Count() - 1:
                case false when StepIndex <= 0:
                    if(_artist != null){ HandleExit(); } else{ App.GetVM().GetDialogManager().DismissDialog(); }
                    return;
                default:
                    StepIndex += isIncrement ? 1 : -1;
                    BackButtonText = StepIndex != 0 ? "Back" : "Exit";
                    break;
            }
            #region Manual Navigation
            if (currentType == typeof(ManualArtistViewModel))
            {
                if (ValidateManualArtist())
                {
                    ToggleVisible = false;
                    IsAlbumView = true;
                    ManualNextStep();
                }
            }
            if (currentType == typeof(ManualAlbumViewModel))
            {
                IsAlbumView = false;
                if (isIncrement)
                {
                    if (ValidateManualAlbum())
                    {
                        ToggleVisible = false;
                        ManualToVideos();
                    }
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
                    CurrentContent = _manualAlbumVM;
                }
            }
            #endregion

            #region Auto Navigation
            if (currentType == typeof(ArtistResultsViewModel))
            {
                //Artist Results -> Album Results
                IsAlbumView = true;
                NextStep(null, _resultVM.SelectedArtist.GetResult());
            }

            if (currentType == typeof(AlbumResultsViewModel))
            {
                IsAlbumView = false;
                //AlbumResults -> VideoResults
                if (isIncrement)
                {
                    ToggleVisible = false;
                    ToVideos(null, _albumResultsVM.SelectedAlbum.GetResult());
                }
                //AlbumResults -> ArtistResults
                else
                {
                    //Re-enable source selector
                    StepperEnabled = true;
                    CurrentContent = _resultVM;
                }
            }

            if (currentType == typeof(VideoResultsViewModel))
            {
                //VideoResults -> Downloading Videos
                if (isIncrement)
                {
                    switch (_resultVM.selectedSource)
                    {
                        case SearchSource.YouTubeMusic:
                            SaveMultipleVideos();
                            break;
                        case SearchSource.AppleMusic:
                            SaveAppleMusicVideo();
                            break;
                    }
                }
                //VideoResults -> AlbumResults
                else
                {
                    ToggleVisible = true;
                    NextButtonText = "Next";
                    CurrentContent = _albumResultsVM;
                    /* TODO: Handle Artist -> Single and back to Artist
                    if (IsAlbumView)
                    {
                    }
                    else
                    {
                        CurrentContent = _resultVM;
                        BackButtonText = "Exit";
                    }
                    */
                }
            }
            #endregion
        }
        [RelayCommand]
        public void CloseDialog()
        {
            ClosePageEvent?.Invoke(this, true);
            App.GetVM().GetDialogManager().DismissDialog();
        }
        public void NextStep(object? sender, ArtistResultCard newArtist)
        {
            BusyText = "Getting Albums...";
            IsBusy = true;
            NavVisible = false;
            ToggleVisible = false;
            switch (_resultVM.selectedSource)
            {
                case SearchSource.YouTubeMusic:
                    YTMusicGetAlbums(newArtist);
                    return;
                case SearchSource.AppleMusic:
                    AppleMusicGetAlbums(newArtist);
                    return;
            }
        }

        public void ToVideos(object? sender, AlbumResult newAlbum)
        {
            BusyText = "Getting Videos...";
            IsBusy = true;
            NavVisible = false;
            switch (_resultVM.selectedSource)
            {
                case SearchSource.YouTubeMusic:
                    YTMusicToVideos(newAlbum);
                    return;
                case SearchSource.AppleMusic:
                    AppleMusicToVideos(newAlbum);
                    return;
            }
        }
        
        public async void HandleSkip()
        {
            ToggleVisible = false;
            IsAlbumView = false;
            StepIndex++;
            //Automatic
            if (ToggleValue)
            {
                BusyText = "Getting All Videos...";
                NextButtonText = "Download";
                IsBusy = true;
                NavVisible = false;
                ArtistMetadata artistMetadata = _artist.GetArtistMetadata();
                string artistID = artistMetadata.BrowseId;
                List<VideoResult>? videos = await _artist.GetVideos();
                //Run this check here because if videoSearch is null, the artist has no videos and the next command will throw an exception, crashing the app
                if(videos == null)
                {
                    HandleNoVideos();
                    return;
                }
                ObservableCollection<VideoResultViewModel> results = await ytMusicHelper.GenerateVideoResultList(videos, _artist);
                if (results.Count == 0)
                {
                    HandleNoVideos();
                    return;
                }
                VideoResultsViewModel resultsVM = new VideoResultsViewModel(results);
                AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);
                _videoResultsParentVM = resultsVM;
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    result.ProgressStarted += parentVM.SaveYTMVideo;
                }
                IsBusy = false;
                NavVisible = true;
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
        #endregion
        
        #region AppleMusic

        private async void AppleMusicGetAlbums(ArtistResultCard newArtist)
        {
            //Prevent duplicates being stored
            if (!_dbContext.ArtistMetadata.Any(am => am.SourceId == SearchSource.AppleMusic && am.BrowseId == newArtist.browseId))
            {
                _artist = await Artist.CreateArtist(newArtist, SearchSource.AppleMusic);
                
                _dbContext.Artist.Add(_artist);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                _artist = _dbContext.ArtistMetadata.Include(am => am.Artist).First(am => am.SourceId == SearchSource.AppleMusic && am.BrowseId == newArtist.browseId).Artist;
            }
            StepperEnabled = false;
            //ArtistMetadata artistMetadata = _artist.GetArtistMetadata(SearchSource.AppleMusic);
            
            //Refresh list to display the new artist
            _parentVM.RefreshList();
            
            //If no albums are found, go straight to video view
            //if(artistMetadata.AlbumResults.Count == 0) {IsAlbumView=false;ToSingles();return;}
            
            
            List<AlbumResult> AlbumList = await _artist.GetAlbums(SearchSource.AppleMusic);
            if(AlbumList == null || AlbumList.Count == 0) {IsAlbumView=false;ToSingles();return;}
            ObservableCollection<AlbumResultViewModel> results = new ObservableCollection<AlbumResultViewModel>();
            for (int i = 0; i < AlbumList.Count; i++)
            {
                var currResult = AlbumList[i];
                currResult.Artist = _artist;
                AlbumResultViewModel newVM = new AlbumResultViewModel(currResult);
                await newVM.LoadThumbnail();
                results.Add(newVM);
            }
            AlbumResultsViewModel resultsVM = new AlbumResultsViewModel(results, SearchSource.AppleMusic);
            _albumResultsVM = resultsVM;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.NextPage += ToVideos;
            }
            IsBusy = false;
            NavVisible = true;
            ToggleVisible = true;
            CurrentContent = resultsVM;
            /*
            //Process Album Results
            ObservableCollection<AlbumResultViewModel> results = await _iTunesApiHelper.GenerateAlbumResultList(_artist);
            
            //Creating the view, binding the results to the generated cards and setting is as the current content
            AlbumResultsViewModel resultsVM = new AlbumResultsViewModel(results);
            _albumResultsVM = resultsVM;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.NextPage += ToVideos;
            }
            IsBusy = false;
            NavVisible = true;
            ToggleVisible = true;
            CurrentContent = resultsVM;
            */
        }
        public async void AppleMusicToVideos(AlbumResult newAlbum)
        {
            Album album;
            ArtistMetadata artistMetadata = _artist.GetArtistMetadata(SearchSource.AppleMusic);
            if (!_dbContext.Album.Any(a => a.Artist.Metadata.Any(am => am.SourceId == SearchSource.AppleMusic) && a.AlbumBrowseID == newAlbum.browseId))
            {
                album = new Album(newAlbum);
                _dbContext.Album.Add(album);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                album = _dbContext.Album.Include(alb => alb.Artist)
                    .ThenInclude(artist => artist.Metadata)
                    .First(a => a.Artist.Metadata.Any(am => am.SourceId == SearchSource.AppleMusic) && a.AlbumBrowseID == newAlbum.browseId);
            }

            ObservableCollection<VideoResultViewModel> results = await _iTunesApiHelper.GenerateVideoResultList(album, null);
            VideoResultsViewModel resultsVM = new VideoResultsViewModel(results);
            AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);
            _videoResultsParentVM = resultsVM;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.ProgressStarted += parentVM.SaveYTMVideo;
            }
            IsBusy = false;
            NavVisible = true;
            NextButtonText = "Download";
            CurrentContent = resultsVM;
        }

        public async Task<AppleMusicDownloadResponse> SaveAppleMusicVideo()
        {
            NavVisible = false;
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            CurrentContent = waveVM;
            VideoResultViewModel selectedVideo = _videoResultsParentVM.SelectedVideos[0];
            //If the artist folder doesn't exist yet, create it
            if (!Directory.Exists($"{_settings.RootFolder}\\{selectedVideo.Artist.Name}"))
            {
                Directory.CreateDirectory($"{_settings.RootFolder}\\{selectedVideo.Artist.Name}");
            }
            AppleMusicDownloadResponse result = await _amDLHelper.DownloadVideo(selectedVideo, waveVM);
            selectedVideo.HandleDownload();
            CurrentContent = _videoResultsParentVM;
            NavVisible = true;
            return result;
        }
        #endregion

        #region YTMusic
        private async void YTMusicGetAlbums(ArtistResultCard newArtist)
        {
            //Prevent duplicates being stored
            if (!_dbContext.ArtistMetadata.Any(am => am.SourceId == SearchSource.YouTubeMusic && am.BrowseId == newArtist.browseId))
            {
                _artist = await Artist.CreateArtist(newArtist, SearchSource.YouTubeMusic);
                
                _dbContext.Artist.Add(_artist);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                _artist = _dbContext.ArtistMetadata.Include(am => am.Artist).First(am => am.SourceId == SearchSource.YouTubeMusic && am.BrowseId == newArtist.browseId).Artist;
            }
            
            
            //Refresh list to display the new artist
            _parentVM.RefreshList();

            List<AlbumResult> AlbumList = await _artist.GetAlbums(SearchSource.YouTubeMusic);
            if(AlbumList == null || AlbumList.Count == 0) {IsAlbumView=false;ToSingles();return;}

            ObservableCollection<AlbumResultViewModel> results = new ObservableCollection<AlbumResultViewModel>();
            for (int i = 0; i < AlbumList.Count; i++)
            {
                var currResult = AlbumList[i];
                currResult.Artist = _artist;
                AlbumResultViewModel newVM = new AlbumResultViewModel(currResult);
                await newVM.LoadThumbnail();
                results.Add(newVM);
            }
            AlbumResultsViewModel resultsVM = new AlbumResultsViewModel(results, SearchSource.YouTubeMusic);
            _albumResultsVM = resultsVM;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.NextPage += ToVideos;
            }
            IsBusy = false;
            NavVisible = true;
            ToggleVisible = true;
            CurrentContent = resultsVM;
        }
        public async void YTMusicToVideos(AlbumResult newAlbum){
            Album album;
            if (!_dbContext.Album.Any(a => a.Artist.Metadata.Any(am => am.SourceId == SearchSource.YouTubeMusic) && a.AlbumBrowseID == newAlbum.browseId))
            {
                album = new Album(newAlbum);
                _dbContext.Album.Add(album);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                album = _dbContext.Album.Include(alb => alb.Artist)
                    .ThenInclude(artist => artist.Metadata)
                    .First(a => a.Artist.Metadata.Any(am => am.SourceId == SearchSource.YouTubeMusic) && a.AlbumBrowseID == newAlbum.browseId);
            }

            ArtistMetadata artistMetadata = _artist.GetArtistMetadata(SearchSource.YouTubeMusic);
            string artistID = artistMetadata.BrowseId;
            List<VideoResult>? videos = await album.Artist.GetVideos(SearchSource.YouTubeMusic);
            YtMusicNet.Models.Album? fullAlbum = await ytMusicHelper.GetAlbum(album.AlbumBrowseID);
            ObservableCollection<VideoResultViewModel> results = await ytMusicHelper.GenerateVideoResultList(videos, fullAlbum, null, album);
            VideoResultsViewModel resultsVM = new VideoResultsViewModel(results);
            AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);
            _videoResultsParentVM = resultsVM;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.ProgressStarted += parentVM.SaveYTMVideo;
            }
            IsBusy = false;
            NavVisible = true;
            NextButtonText = "Download";
            CurrentContent = resultsVM;
        }

        public async void ToSingles()
        {
            BusyText = "Getting Videos...";
            StepIndex++;
            IsBusy = true;
            NavVisible = false;
            ToggleVisible = false;
            ArtistMetadata artistMetadata = _artist.GetArtistMetadata();
            List<VideoResult>? videos = await _artist.GetVideos();
            if(videos == null)
            {
                HandleNoVideos();
                return;
            }
            ObservableCollection<VideoResultViewModel> results = await ytMusicHelper.GenerateVideoResultList(videos, _artist);
            VideoResultsViewModel resultsVM = new VideoResultsViewModel(results);
            AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);
            _videoResultsParentVM = resultsVM;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.ProgressStarted += parentVM.SaveYTMVideo;
            }
            IsBusy = false;
            NavVisible = true;
            NextButtonText = "Download";
            CurrentContent = resultsVM;
        }
        private async void SaveMultipleVideos()
        {
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            IsAlbumView = false;
            NavVisible = false;
            CurrentContent = waveVM;
            for (int i = 0; i < _videoResultsParentVM.SelectedVideos.Count; i++)
            {
                if (_videoResultsParentVM.SelectedVideos.Count > 1)
                {
                    waveVM.HeaderText = $"Downloading {_videoResultsParentVM.SelectedVideos[i].Title} {i + 1}/{_videoResultsParentVM.SelectedVideos.Count}";
                }
                else
                {
                    waveVM.HeaderText = $"Downloading {_videoResultsParentVM.SelectedVideos[i].Title}";
                }
                var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress));
                var currResult = _videoResultsParentVM.SelectedVideos[i].HandleDownload();
                var downResult = await _ytDLHelper.DownloadVideo(currResult.VideoID, $"{_settings.RootFolder}/{currResult.Artist.Name}", currResult.Title, progress);
                if (downResult.Success)
                {
                    _videoResultsParentVM.SelectedVideos[i].GenerateNFO(downResult.Data).ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            if(i == _videoResultsParentVM.SelectedVideos.Count)
                            {
                                HandleExit();
                                return;
                            }
                            waveVM.UpdateProgress(0);
                        }
                        else
                        {
                            App.GetVM().GetToastManager().CreateToast()
                                .WithTitle("Error")
                                .WithContent($"Something went wrong with downloading {_videoResultsParentVM.SelectedVideos[i].Title}")
                                .OfType(NotificationType.Error)
                                .Queue();
                            Log.Error($"Error in NewArtist->SaveMultipleVideos: {currResult.VideoID} failed");
                            foreach (var err in downResult.ErrorOutput)
                            {
                                Log.Error(err);
                            }
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    App.GetVM().GetToastManager().CreateToast()
                        .WithTitle("Error")
                        .WithContent($"Something went wrong with downloading {_videoResultsParentVM.SelectedVideos[i].Title}")
                        .OfType(NotificationType.Error)
                        .Queue();
                    Log.Error($"Error in NewArtist->SaveMultipleVideos: {currResult.VideoID} failed");
                    foreach (var err in downResult.ErrorOutput)
                    {
                        Log.Error(err);
                    }
                    break;
                }
            }
        }
        
        #endregion
        
        #region Manual
        public void ManualNextStep()
        {
            //Ensure no duplicates are saved
            if (!_dbContext.Artist.Any(e => e.Name.ToLower() == _manualArtVM.ArtistNameText.ToLower()))
            {
                Artist newArtist = new Artist();
                newArtist.Name = _manualArtVM.ArtistNameText;
                _dbContext.Artist.Add(newArtist);
                _dbContext.SaveChanges();
                _artist = newArtist;
                
                ManualAlbumViewModel newVM = new ManualAlbumViewModel(newArtist);
                _manualAlbumVM = newVM;
                CurrentContent = newVM;
            }
            else
            {
                Log.Error($"Error in NewArtist->ManualNextStep: {_manualArtVM.ArtistNameText} already exists in db");
                App.GetVM().GetToastManager().CreateToast()
                    .WithTitle("Error")
                    .WithContent($"Already have artist with the same name!")
                    .OfType(NotificationType.Error)
                    .Queue();
            }
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
            //Ensure no duplicates
            if (!_dbContext.Album.Any(e =>
                    e.Title == newAlbum.Title && e.Artist == newAlbum.Artist && e.Year == newAlbum.Year))
            {
                _dbContext.Album.Add(newAlbum);
                _dbContext.SaveChanges();
            }

            ManualMusicVideoViewModel newVM = new ManualMusicVideoViewModel(newAlbum);
            _manualMusicVideoVM = newVM;
            CurrentContent = newVM;
        }
        
        public async void ManualSaveVideo()
        {
            NavVisible = false;
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            waveVM.HeaderText = "Downloading " + _manualMusicVideoVM.Title;
            CurrentContent = waveVM;
            var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress));
            RunResult<string> downloadResult = await _ytDLHelper.DownloadVideo(_manualMusicVideoVM._vidData.ID, $"{_settings.RootFolder}/{_artist.Name}", _manualMusicVideoVM.Title, progress);
            if (downloadResult.Success)
            {
                GenerateManualNFO(downloadResult.Data);
                _manualMusicVideoVM.ClearData();
                CurrentContent = _manualMusicVideoVM;
                NavVisible = true;
            }
            else
            {
                string[] errorContent = downloadResult.ErrorOutput;
                App.GetVM().GetToastManager().CreateToast()
                    .WithTitle("Download Error")
                    .WithContent(errorContent[0])
                    .OfType(NotificationType.Error)
                    .Queue();
                Log.Error($"Error in NewArtist->ManualSaveVideo: {_manualMusicVideoVM._vidData.ID} failed");
                foreach (var err in downloadResult.ErrorOutput)
                {
                    Log.Error(err);
                }
            }
        }

        public bool ValidateManualArtist()
        {
            if (_manualArtVM.ArtistNameText == null)
            {
                IsBusy = false;
                NavVisible = true;
                ShowError = true;
                ErrorText = "Artist Name cannot be blank!";
                StepIndex--;
                BackButtonText = "Exit";
                return false;
            }
            return true;
        }

        public bool ValidateManualAlbum()
        {
            if (_manualAlbumVM.AlbumNameText == null)
            {
                IsBusy = false;
                NavVisible = true;
                ShowError = true;
                ErrorText = "Album Name cannot be blank!";
                StepIndex--;
                BackButtonText = "Exit";
                return false;
            }
            if (_manualAlbumVM.AlbumYear == null)
            {
                IsBusy = false;
                NavVisible = true;
                ShowError = true;
                ErrorText = "Year cannot be blank!";
                StepIndex--;
                BackButtonText = "Exit";
                return false;
            }
            return true;
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
            newMV.nfoPath = $"{_settings.RootFolder}/{newMV.artist.Name}/{newMV.title}.nfo";

            await _manualMusicVideoVM.SaveThumbnailAsync($"{_settings.RootFolder}/{newMV.artist.Name}", newMV.title);
            newMV.thumb = $"{newMV.title}.jpg";

            newMV.SaveToNFO();
            _dbContext.MusicVideos.Add(newMV);
            _dbContext.SaveChanges();
        }
        #endregion

        private async void HandleNoVideos()
        {
            IsBusy = false;
            IsAlbumView = false;
            NavVisible = true;
            ManualMusicVideoViewModel manualVM = new ManualMusicVideoViewModel(_artist);
            _manualMusicVideoVM = manualVM;
            App.GetVM().GetToastManager().CreateToast()
                .WithTitle("Error")
                .WithContent("No Videos Available")
                .OfType(NotificationType.Error)
                .Queue();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentContent = manualVM;
            });
        }

    }
}
