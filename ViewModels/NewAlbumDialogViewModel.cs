using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVNFOEditor.Models;
using MVNFOEditor.Views;
using MVNFOEditor.Helpers;
using SukiUI.Controls;
using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Threading;
using Newtonsoft.Json.Linq;
using MVNFOEditor.DB;
using ReactiveUI;
using YoutubeDLSharp;

namespace MVNFOEditor.ViewModels
{
    public partial class NewAlbumDialogViewModel : ObservableObject
    {
        private AddMusicVideoParentViewModel _mvParentVM;
        private ArtistListParentViewModel _parentVM;
        private AlbumResultsViewModel _resultVM;
        private ManualAlbumViewModel? _manualAlbumVM;
        private ManualMusicVideoViewModel? _manualMVVM;
        private SyncDialogViewModel? _syncVM;
        private YTMusicHelper ytMusicHelper;
        private YTDLHelper _ytDLHelper;
        private MusicDbContext _dbContext;
        private Artist _artist;

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _skipVisible;
        [ObservableProperty] private bool _toggleVisible;
        [ObservableProperty] private bool _toggleValue;
        [ObservableProperty] private bool _toggleEnable;
        [ObservableProperty] private bool _showError;
        [ObservableProperty] private bool _notDownload;
        [ObservableProperty] private string _backButtonText;
        [ObservableProperty] private string _busyText;
        [ObservableProperty] private string _errorText;
        [ObservableProperty] private object _currentContent;
        [ObservableProperty] private ObservableCollection<string> _steps;
        public event EventHandler<bool> ClosePageEvent;
        
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

        public NewAlbumDialogViewModel(ManualAlbumViewModel vm, Artist artist)
        {
            Steps = ["Create Album", "Create Video"];
            BackButtonText = "Exit";
            ytMusicHelper = App.GetYTMusicHelper();
            _ytDLHelper = App.GetYTDLHelper();
            _dbContext = App.GetDBContext();
            _parentVM = App.GetVM().GetParentView();
            _artist = artist;
            ToggleVisible = false;
            ToggleEnable = false;
            ToggleValue = false;
            NotDownload = true;
            _syncVM = null;
            _manualAlbumVM = vm;
            CurrentContent = vm;
        }

        public NewAlbumDialogViewModel(AlbumResultsViewModel vm, Artist artist)
        {
            Steps = ["Select Album", "Select Videos"];
            _resultVM = vm;
            CurrentContent = vm;
            _syncVM = null;
            ytMusicHelper = App.GetYTMusicHelper();
            _ytDLHelper = App.GetYTDLHelper();
            _dbContext = App.GetDBContext();
            ToggleEnable = true;
            ToggleVisible = true;
            ToggleValue = true;
            NotDownload = true;
            BackButtonText = "Exit";
            _parentVM = App.GetVM().GetParentView();
            _artist = artist;
        }

        public async Task NextStep(object? sender, AlbumResult newAlbum)
        {
            Album album = null;
            if (!_dbContext.Album.Any(a => a.ytMusicBrowseID == newAlbum.browseId))
            {
                album = new Album(newAlbum);
                _dbContext.Album.Add(album);
                _dbContext.SaveChanges();
                _parentVM.RefreshDetails();
            }
            else
            {
                album = _dbContext.Album.First(a => a.ytMusicBrowseID == newAlbum.browseId);
            }

            string artistID = album.Artist.YTMusicId;
            JArray videoSearch = ytMusicHelper.get_videos(artistID);
            JObject fullAlbumDetails = ytMusicHelper.get_album(album.ytMusicBrowseID);
            ObservableCollection<SyncResultViewModel> results = await ytMusicHelper.GenerateSyncResultList(videoSearch, fullAlbumDetails, null, album);

            if (results.Count == 0)
            {
                ManualMusicVideoViewModel manualVM = new ManualMusicVideoViewModel(_artist);
                SukiHost.ShowToast("Error", "No Videos Available");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    CurrentContent = manualVM;
                });
                return;
            }
            else
            {
                SyncDialogViewModel resultsVM = new SyncDialogViewModel(results);
                AddMusicVideoParentViewModel parentVM = new AddMusicVideoParentViewModel(resultsVM);
                _syncVM = resultsVM;
                _mvParentVM = parentVM;
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    result.ProgressStarted += parentVM.BuildProgressVM;
                }
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    CurrentContent = resultsVM;
                });
            }
        }
        
        public async void HandleNavigation(bool isIncrement)
        {
            IsBusy = true;
            //Undo Errors
            ShowError = false;
            ErrorText = "";
            //Get Current Page
            Type currentType = CurrentContent.GetType();
            switch (isIncrement)
            {
                case true when StepIndex > Steps.Count() - 1:
                case false when StepIndex <= 0:
                    SukiHost.CloseDialog();
                    return;
                default:
                    SkipVisible = isIncrement;
                    StepIndex += isIncrement ? 1 : -1;
                    BackButtonText = StepIndex != 0 ? "Back" : "Exit";
                    break;
            }


            if (currentType == typeof(ManualAlbumViewModel))
            {
                ToggleVisible = false;
                ToggleEnable = false;
                if (ValidateManualAlbum())
                {
                    var album = await _manualAlbumVM.SaveAlbum();
                    ManualMusicVideoViewModel newVM = new ManualMusicVideoViewModel(album);
                    _manualMVVM = newVM;
                    CurrentContent = newVM;
                    IsBusy = false;
                }
            }

            if (currentType == typeof(ManualMusicVideoViewModel))
            {
                if (isIncrement)
                {
                    SaveVideo();
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
                BusyText = "Searching YouTube Music...";
                ToggleVisible = false;
                await NextStep(null, _resultVM.SelectedAlbum.GetResult());
                IsBusy = false;
            }

            if (currentType == typeof(SyncDialogViewModel))
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
        public bool ValidateManualAlbum()
        {
            if (_manualAlbumVM.AlbumYear == null)
            {
                IsBusy = false;
                ShowError = true;
                ErrorText = "Year cannot be blank!";
                StepIndex--;
                BackButtonText = "Exit";
                return false;
            }
            if (_manualAlbumVM.AlbumNameText == null)
            {
                IsBusy = false;
                ShowError = true;
                ErrorText = "Album Name cannot be blank!";
                StepIndex--;
                BackButtonText = "Exit";
                return false;
            }
            return true;
        }
        private async void SaveVideo()
        {
            NotDownload = false;
            SettingsData localData = _dbContext.SettingsData.First();
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            waveVM.HeaderText = "Downloading " + _manualMVVM.Title;
            CurrentContent = waveVM;
            var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress));
            RunResult<string> downloadResult = await _ytDLHelper.DownloadVideo(_manualMVVM._vidData.ID, $"{localData.RootFolder}/{_manualMVVM.Artist.Name}", _manualMVVM.Title, progress);
            if (downloadResult.Success)
            {
                _manualMVVM.GenerateManualNFO(downloadResult.Data);
                _manualMVVM.ClearData();
                NotDownload = true;
                CurrentContent = _manualMVVM;
            }
            else
            {
                NotDownload = true;
                string[] errorContent = downloadResult.ErrorOutput;
                SukiHost.ShowToast("Download Error", errorContent[0]);
            }
        }

        private async void SaveMultipleVideos()
        {
            SettingsData localData = _dbContext.SettingsData.First();
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            SkipVisible = false;
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
                var downResult = await _ytDLHelper.DownloadVideo(currResult.vidID, $"{localData.RootFolder}/{currResult.Artist.Name}", currResult.Title, progress);
                if (downResult.Success)
                {
                    _syncVM.SelectedVideos[i].GenerateNFO(downResult.Data).ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            waveVM.UpdateProgress(0);
                        }
                        else
                        {
                            SukiHost.ShowToast("Error", $"Something went wrong with downloading {_syncVM.SelectedVideos[i].Title}");
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    SukiHost.ShowToast("Error", $"Something went wrong with downloading {_syncVM.SelectedVideos[i].Title}");
                    break;
                }
            }
            CloseDialog();
        }

        public void HandleChangedMode(bool? changeValue)
        {
            if ((bool)!changeValue)
            {
                ManualAlbumViewModel newVM = new ManualAlbumViewModel(_artist);
                Steps = ["Create Album", "Create Video"];
                CurrentContent = newVM;
                _manualAlbumVM = newVM;
            }
            else
            {
                Steps = ["Select Album", "Select Videos"];
                CurrentContent = _resultVM;
            }
        }

        public void BackTrigger(){HandleNavigation(false); }
        public void NextTrigger() {HandleNavigation(true); }
        
        [RelayCommand]
        public void CloseDialog()
        {
            ClosePageEvent?.Invoke(this,true);
            SukiHost.CloseDialog();
        }
    }
}