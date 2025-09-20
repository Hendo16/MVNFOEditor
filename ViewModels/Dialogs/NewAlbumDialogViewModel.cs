using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MVNFOEditor.Models;
using MVNFOEditor.Helpers;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using Newtonsoft.Json.Linq;
using MVNFOEditor.DB;
using YoutubeDLSharp;
using Avalonia.Controls.Notifications;
using Flurl.Util;
using log4net;
using Microsoft.EntityFrameworkCore;
using MVNFOEditor.Settings;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels
{
    public partial class NewAlbumDialogViewModel : ObservableObject
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AddMusicVideoParentViewModel));
        
        private AddMusicVideoParentViewModel _mvParentVM;
        private ArtistListParentViewModel _parentVM;
        private AlbumResultsViewModel _resultVM;
        private ManualAlbumViewModel? _manualAlbumVM;
        private ManualMusicVideoViewModel? _manualMVVM;
        private VideoResultsViewModel? _syncVM;
        private YTMusicHelper ytMusicHelper;
        private iTunesAPIHelper _iTunesApiHelper;
        private YTDLHelper _ytDLHelper;
        private static ISettings _settings;
        private MusicDbContext _dbContext;

        [ObservableProperty] private Artist _currArtist;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _navVisible;
        [ObservableProperty] private bool _toggleVisible;
        [ObservableProperty] private bool _toggleValue;
        [ObservableProperty] private bool _toggleEnable;
        [ObservableProperty] private bool _showError;
        [ObservableProperty] private bool _notDownload;
        [ObservableProperty] private string _backButtonText;
        [ObservableProperty] private string _saveButtonText;
        [ObservableProperty] private string _busyText;
        [ObservableProperty] private string _errorText;
        [ObservableProperty] private object _currentContent;
        [ObservableProperty] private bool _ytEnabled;
        [ObservableProperty] private bool _amEnabled;
        [ObservableProperty] private bool _manChecked;
        [ObservableProperty] private bool _ytChecked;
        [ObservableProperty] private bool _amChecked;
        [ObservableProperty] private ObservableCollection<string> _steps;
        public event EventHandler<bool> ClosePageEvent;
        
        private int _stepIndex = 0;
        private bool JustAlbum;
        public int StepIndex
        {
            get { return _stepIndex; }
            set
            {
                _stepIndex = value;
                OnPropertyChanged(nameof(StepIndex));
            }
        }

        public NewAlbumDialogViewModel(ManualAlbumViewModel vm, Artist currArtist, bool justAlbum = false)
        {
            Steps = justAlbum ? ["Create Album"] : ["Create Album", "Create Video"];
            SaveButtonText = justAlbum ? "Save" : "Next";
            BackButtonText = "Exit";
            ytMusicHelper = App.GetYTMusicHelper();
            _ytDLHelper = App.GetYTDLHelper();
            _iTunesApiHelper = App.GetiTunesHelper();
            _dbContext = App.GetDBContext();
            _settings = App.GetSettings();
            _parentVM = App.GetVM().GetParentView();
            _currArtist = currArtist;
            ToggleVisible = false;
            ToggleEnable = false;
            ToggleValue = false;
            NavVisible = true;
            NotDownload = true;
            JustAlbum = justAlbum;
            _syncVM = null;
            _manualAlbumVM = vm;
            CurrentContent = vm;
        }

        public NewAlbumDialogViewModel(AlbumResultsViewModel vm, Artist currArtist)
        {
            Steps = ["Select Album", "Select Videos"];
            BackButtonText = "Exit";
            SaveButtonText = "Next";
            _resultVM = vm;
            CurrentContent = _resultVM;
            _syncVM = null;
            ytMusicHelper = App.GetYTMusicHelper();
            _ytDLHelper = App.GetYTDLHelper();
            _iTunesApiHelper = App.GetiTunesHelper();
            _dbContext = App.GetDBContext();
            _settings = App.GetSettings();
            ToggleEnable = true;
            ToggleVisible = true;
            ToggleValue = true;
            NavVisible = true;
            NotDownload = true;
            _parentVM = App.GetVM().GetParentView();
            _currArtist = currArtist;
            SetupSource(_resultVM.selectedSource);
        }

        public async Task<bool> GenerateNewResults(SearchSource source)
        {
            List<AlbumResult>? albumResults = await CurrArtist.GetAlbums(source);
            if (albumResults == null || albumResults.Count == 0)
            {
                return false;
            }
            _resultVM.GenerateNewResults(albumResults);
            return true;
        }

        private void SetupSource(SearchSource source)
        {
            switch (source)
            {
                case SearchSource.YouTubeMusic:
                    YtChecked = true;
                    break;
                case SearchSource.AppleMusic:
                    AmChecked = true;
                    break;
                case SearchSource.Manual:
                    ManChecked = true;
                    break;
            }

            YtEnabled = CurrArtist.Metadata.Any(meta => meta.SourceId == SearchSource.YouTubeMusic) && !ManChecked;
            AmEnabled = CurrArtist.Metadata.Any(meta => meta.SourceId == SearchSource.AppleMusic) && !ManChecked;
        }

        public async Task NextStep(object? sender, AlbumResult newAlbum)
        {
            NavVisible = false;
            switch (newAlbum.SearchSource)
            {
                case SearchSource.YouTubeMusic:
                    await GetYTMusicVideos(newAlbum);
                    break;
                case SearchSource.AppleMusic:
                    await GetAMVideos(newAlbum);
                    break;
            }
            NavVisible = true;
        }
        
        public async void HandleNavigation(bool isIncrement)
        {
            IsBusy = true;
            NavVisible = false;
            //Undo Errors
            ShowError = false;
            ErrorText = "";
            //Get Current Page
            Type currentType = CurrentContent.GetType();
            switch (isIncrement)
            {
                case true when StepIndex > Steps.Count() - 1:
                case false when StepIndex <= 0:
                    App.GetVM().GetDialogManager().DismissDialog();
                    return;
                default:
                    StepIndex += isIncrement ? 1 : -1;
                    BackButtonText = StepIndex != 0 ? "Back" : "Exit";
                    SaveButtonText = StepIndex != 0 ? "Download" : "Next";
                    break;
            }


            if (currentType == typeof(ManualAlbumViewModel))
            {
                if (ValidateManualAlbum())
                {
                    ToggleVisible = false;
                    ToggleEnable = false;
                    var album = await _manualAlbumVM.SaveAlbum();
                    if (!JustAlbum)
                    {
                        ManualMusicVideoViewModel newVM = new ManualMusicVideoViewModel(album);
                        _manualMVVM = newVM;
                        CurrentContent = newVM;
                        NavVisible = true;
                        IsBusy = false;
                    }
                    else
                    {
                        App.GetVM().GetDialogManager().DismissDialog();
                    }
                }
            }

            if (currentType == typeof(ManualMusicVideoViewModel))
            {
                if (isIncrement)
                {
                    //SaveVideo();
                    SaveMultipleManualVideos();
                }
                else
                {
                    ToggleVisible = true;
                    ToggleEnable = true;
                    if (ToggleValue)
                    {
                        CurrentContent = _resultVM;
                    }
                    else
                    {
                        CurrentContent = _manualAlbumVM;
                    }
                }
            }
            
            if (currentType == typeof(AlbumResultsViewModel))
            {
                BusyText = "Getting videos...";
                ToggleVisible = false;
                await NextStep(null, _resultVM.SelectedAlbum.GetResult());
                NavVisible = true;
                IsBusy = false;
            }

            if (currentType == typeof(VideoResultsViewModel))
            {
                if (isIncrement)
                {
                    SaveMultipleVideos();
                }
                else
                {
                    CurrentContent = _resultVM;
                }
            }
        }

        #region StepperChecked

        public void ManualChecked()
        {
            ManualAlbumViewModel newVM = new ManualAlbumViewModel(CurrArtist);
            Steps = ["Create Album", "Create Video"];
            CurrentContent = newVM;
            _manualAlbumVM = newVM;
        }
        public async void YouTubeChecked()
        {
            if (! await GenerateNewResults(SearchSource.YouTubeMusic))
            {
                App.GetVM().GetToastManager().CreateToast()
                    .WithTitle("Error")
                    .WithContent("No Albums Found on YouTube")
                    .OfType(NotificationType.Error)
                    .Dismiss()
                    .After(TimeSpan.FromSeconds(5))
                    .Queue();
            }
        }
        public async void AppleMusicChecked()
        {
            if (! await GenerateNewResults(SearchSource.AppleMusic))
            {
                App.GetVM().GetToastManager().CreateToast()
                    .WithTitle("Error")
                    .WithContent("No Albums Found on Apple Music")
                    .OfType(NotificationType.Error)
                    .Dismiss()
                    .After(TimeSpan.FromSeconds(5))
                    .Queue();
            }
        }
        #endregion
        
        private async Task<bool> GetAMVideos(AlbumResult newAlbum)
        {
            Album album;
            if (!_dbContext.Album.Any(a => a.AlbumBrowseID == newAlbum.browseId))
            {
                album = await Album.CreateAlbum(newAlbum, SearchSource.AppleMusic);
                _dbContext.Album.Add(album);
                await _dbContext.SaveChangesAsync();
                _parentVM.RefreshDetails();
            }
            else
            {
                album = _dbContext.Album.Include(alb => alb.Artist).First(a => a.AlbumBrowseID == newAlbum.browseId);
            }

            ObservableCollection<VideoResultViewModel> results = await _iTunesApiHelper.GenerateVideoResultList(album, null);
            if (results.Count == 0)
            {
                ManualMusicVideoViewModel manualVM = new ManualMusicVideoViewModel(_currArtist);
                App.GetVM().GetToastManager().CreateToast()
                    .WithTitle("Error")
                    .WithContent("No Videos Available")
                    .OfType(NotificationType.Error)
                    .Queue();
                manualVM.CurrAlbum = album;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    CurrentContent = manualVM;
                });
                return true;
            }
            VideoResultsViewModel resultsVM = new VideoResultsViewModel(results);
            ArtistMetadata artistMetadata = album.Artist.GetArtistMetadata(SearchSource.AppleMusic);
            AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);

            _syncVM = resultsVM;
            _mvParentVM = parentVM;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.ProgressStarted += parentVM.SaveAMVideo;
            }
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentContent = resultsVM;
            });
            return true;
        }

        private async Task<bool> GetYTMusicVideos(AlbumResult newAlbum)
        {
            Album album;
            if (!_dbContext.Album.Any(a => a.AlbumBrowseID == newAlbum.browseId))
            {
                album = await Album.CreateAlbum(newAlbum, SearchSource.YouTubeMusic);
                _dbContext.Album.Add(album);
                await _dbContext.SaveChangesAsync();
                _parentVM.RefreshDetails();
            }
            else
            {
                album = _dbContext.Album.Include(alb => alb.Artist).First(a => a.AlbumBrowseID == newAlbum.browseId);
            }

            ArtistMetadata artistMetadata = album.Artist.GetArtistMetadata(SearchSource.YouTubeMusic);
            string artistID = artistMetadata.BrowseId;
            List<VideoResult>? videos = await album.Artist.GetVideos(SearchSource.YouTubeMusic);
            YtMusicNet.Models.Album? fullAlbum = await ytMusicHelper.GetAlbum(album.AlbumBrowseID);
            ObservableCollection<VideoResultViewModel> results = await ytMusicHelper.GenerateVideoResultList(videos, fullAlbum, null, album);

            if (results.Count == 0)
            {
                ManualMusicVideoViewModel manualVM = new ManualMusicVideoViewModel(_currArtist);
                App.GetVM().GetToastManager().CreateToast()
                    .WithTitle("Error")
                    .WithContent("No Videos Available")
                    .OfType(NotificationType.Error)
                    .Queue();
                manualVM.CurrAlbum = album;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    CurrentContent = manualVM;
                });
                return true;
            }
            VideoResultsViewModel resultsVM = new VideoResultsViewModel(results);
            AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);

            _syncVM = resultsVM;
            _mvParentVM = parentVM;
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                result.ProgressStarted += parentVM.SaveYTMVideo;
            }
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentContent = resultsVM;
            });
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
        public async void SaveMultipleManualVideos()
        {
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            CurrentContent = waveVM;
            NavVisible = false;
            for (int i = 0; i < _manualMVVM.ManualItems.Count; i++)
            {
                string headerText = "";
                if (_manualMVVM.ManualItems.Count > 1)
                {
                    headerText = $"Downloading {_manualMVVM.ManualItems[i].Title} {i + 1}/{_manualMVVM.ManualItems.Count}";
                }
                else
                {
                    headerText = $"Downloading {_manualMVVM.ManualItems[i].Title}";
                }

                waveVM.HeaderText = headerText;
                var progressTest = new ProgressBar() { Value = 0, ShowProgressText = true };
                var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress, ref progressTest));
                var toastTest = App.GetVM().GetToastManager().CreateToast()
                    .WithTitle(headerText)
                    .WithContent(progressTest)
                    .Queue();
                var downResult = await _ytDLHelper.DownloadVideo(_manualMVVM.ManualItems[i].VidID, $"{_settings.RootFolder}/{_manualMVVM.Artist.Name}", _manualMVVM.ManualItems[i].Title, progress);
                App.GetVM().GetToastManager().Dismiss(toastTest);
                NavVisible = true;
                if (downResult.Success)
                {
                    _manualMVVM.GenerateManualNFO(downResult.Data, i).ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            waveVM.UpdateProgress(0);
                        }
                        else
                        {
                            App.GetVM().GetToastManager().CreateToast()
                                .WithTitle("Error")
                                .WithContent($"Something went wrong with downloading {_manualMVVM.ManualItems[i].Title}")
                                .OfType(NotificationType.Error)
                                .Queue();
                            Log.Error($"Error in AddMusicVideo->SaveMultipleManualVideos: {_manualMVVM.ManualItems[i].VidID} failed");
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
                        .WithContent($"Something went wrong with downloading {_manualMVVM.ManualItems[i].Title}")
                        .OfType(NotificationType.Error)
                        .Queue();
                    Log.Error($"Error in AddMusicVideo->SaveMultipleManualVideos: {_manualMVVM.ManualItems[i].VidID} failed");
                    foreach (var err in downResult.ErrorOutput)
                    {
                        Log.Error(err);
                    }
                    break;
                }
            }
            CloseDialog();
        }

        private async void SaveMultipleVideos()
        {
            NavVisible = false;
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            CurrentContent = waveVM;
            for (int i = 0; i < _syncVM.SelectedVideos.Count; i++)
            {
                if (_syncVM.SelectedVideos.Count > 1)
                {
                    waveVM.HeaderText = $"Downloading {_syncVM.SelectedVideos[i].Title} {i + 1}/{_syncVM.SelectedVideos.Count}";
                }
                else
                {
                    waveVM.HeaderText = $"Downloading {_syncVM.SelectedVideos[i].Title}";
                }
                var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress));
                var currResult = _syncVM.SelectedVideos[i].HandleDownload();
                var downResult = await _ytDLHelper.DownloadVideo(currResult.VideoID, $"{_settings.RootFolder}/{currResult.Artist.Name}", currResult.Title, progress);
                if (downResult.Success)
                {
                    _syncVM.SelectedVideos[i].GenerateNFO(downResult.Data, SearchSource.YouTubeMusic).ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            waveVM.UpdateProgress(0);
                        }
                        else
                        {
                            App.GetVM().GetToastManager().CreateToast()
                                .WithTitle("Error")
                                .WithContent($"Something went wrong with downloading {_syncVM.SelectedVideos[i].Title}")
                                .OfType(NotificationType.Error)
                                .Queue();
                            Log.Error($"Error in NewAlbum->SaveMultipleVideos: {currResult.VideoID} failed");
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
                        .WithContent($"Something went wrong with downloading {_syncVM.SelectedVideos[i].Title}")
                        .OfType(NotificationType.Error)
                        .Queue();
                    Log.Error($"Error in NewAlbum->SaveMultipleVideos: {currResult.VideoID} failed");
                    foreach (var err in downResult.ErrorOutput)
                    {
                        Log.Error(err);
                    }
                    break;
                }
            }
            CloseDialog();
        }

        public void BackTrigger(){HandleNavigation(false); }
        public void NextTrigger() {HandleNavigation(true); }

        [RelayCommand]
        public void CloseDialog()
        {
            ClosePageEvent?.Invoke(this,true);
            App.GetVM().GetDialogManager().DismissDialog();
        }
    }
}