using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;
using MVNFOEditor.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using YoutubeDLSharp;
using MVNFOEditor.DB;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using log4net;
using MVNFOEditor.Settings;

namespace MVNFOEditor.ViewModels
{
    public partial class AddMusicVideoParentViewModel : ObservableObject
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AddMusicVideoParentViewModel));
        private ManualMusicVideoViewModel? _manualVM;
        private VideoResultsViewModel? _resultsVM;
        private YTDLHelper _ytDLHelper;
        private AppleMusicDLHelper _amDLHelper;
        private MusicDbContext _dbContext;
        private static ISettings _settings;
        private SearchSource _searchSource;
        private ISukiToastManager ToastManager;
        private ISukiDialogManager DialogManager;
        public event EventHandler<bool> RefreshAlbumEvent;

        [ObservableProperty] private object _currentContent;
        [ObservableProperty] private bool _navVisible;
        [ObservableProperty] private bool _toggleValue;
        [ObservableProperty] private bool _showError;
        [ObservableProperty] private string _errorText;
        private bool _edit;
        private WaveProgressViewModel currentDownload;

        public AddMusicVideoParentViewModel(ManualMusicVideoViewModel vm, bool edit = false)
        {
            _ytDLHelper = App.GetYTDLHelper();
            _dbContext = App.GetDBContext();
            _settings = App.GetSettings();
            ToastManager = App.GetVM().GetToastManager();
            DialogManager = App.GetVM().GetDialogManager();
            _searchSource = SearchSource.Manual;
            _manualVM = vm;
            CurrentContent = _manualVM;
            _edit = edit;
            NavVisible = true;
        }

        public AddMusicVideoParentViewModel(VideoResultsViewModel vm, SearchSource source)
        {
            _resultsVM = vm;
            _searchSource = source;
            _ytDLHelper = App.GetYTDLHelper();
            _amDLHelper = App.GetAppleMusicDLHelper();
            _dbContext = App.GetDBContext();
            _settings = App.GetSettings();
            ToastManager = App.GetVM().GetToastManager();
            DialogManager = App.GetVM().GetDialogManager();
            CurrentContent = _resultsVM;
            NavVisible = true;
        }

        public async void SaveAMVideo(object? sender, VideoResultViewModel selectedVideo)
        {
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            currentDownload = waveVM;
            CurrentContent = waveVM;
            //If the artist folder doesn't exist yet, create it
            if (!Directory.Exists($"{_settings.RootFolder}\\{selectedVideo.Artist.Name}"))
            {
                Directory.CreateDirectory($"{_settings.RootFolder}\\{selectedVideo.Artist.Name}");
            }
            AppleMusicDownloadResponse result = await _amDLHelper.DownloadVideo(selectedVideo, waveVM);
            if (result == AppleMusicDownloadResponse.Success)
            {
                await selectedVideo.GenerateNFO($"{_settings.RootFolder}/{selectedVideo.Artist.Name}/{selectedVideo.Title}.mp4",
                    SearchSource.AppleMusic);
                selectedVideo.HandleDownload();
                CurrentContent = _resultsVM;
                NavVisible = true;
            }
            else
            {
                DialogManager.DismissDialog();
                ToastManager.CreateToast()
                    .WithTitle("Download Failed")
                    .WithContent("Error downloading video, please check logs")
                    .OfType(NotificationType.Error)
                    .Queue();
                Log.Error($"Error in AddMusicVideo->SaveAMVideo: Download failed");
            }
        }

        public async void SaveYTMVideo(object? sender, VideoResultViewModel? _resultVM)
        {
            NavVisible = false;
            VideoResult _result = _resultVM.GetResult();
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            waveVM.HeaderText = $"Downloading {_result.Title} - ";
            CurrentContent = waveVM;
            var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress));
            RunResult<string> downloadResult = await _ytDLHelper.DownloadVideo(_result.VideoID, $"{_settings.RootFolder}/{_result.Artist.Name}", _result.Title, progress);
            if (downloadResult.Success)
            {
                await _resultVM.GenerateNFO(downloadResult.Data, SearchSource.YouTubeMusic);
                RefreshAlbumEvent?.Invoke(sender, true);
                CurrentContent = _resultsVM;
                NavVisible = true;
            }
            else
            {
                string[] errorContent = downloadResult.ErrorOutput;
                DialogManager.DismissDialog();
                ToastManager.CreateToast()
                    .WithTitle("Download Error")
                    .WithContent(errorContent[0])
                    .OfType(NotificationType.Error)
                    .Queue();
                Log.Error($"Error in AddMusicVideo->SaveYTMVideo: {_result.VideoID} failed");
                foreach (var err in downloadResult.ErrorOutput)
                {
                    Log.Error(err);
                }
            }
        }

        public async void HandleSave()
        {
            NavVisible = false;
            switch (_searchSource)
            {
                case SearchSource.YouTubeMusic:
                    await SaveMultipleVideos();
                    CurrentContent = _resultsVM;
                    break;
                case SearchSource.AppleMusic:
                    AppleMusicDownloadResponse result = await SaveAppleMusicVideo();
                    switch (result)
                    {
                        case AppleMusicDownloadResponse.Success:
                            Console.WriteLine("Done!");
                            break;
                        case AppleMusicDownloadResponse.Failure:
                            ToastManager.CreateToast()
                                .WithTitle("Download Error")
                                .WithContent("Failure to download. Please check logs")
                                .OfType(NotificationType.Error)
                                .Queue();
                            Log.Error($"Error in AddMusicVideo->HandleSave: {_resultsVM.SelectedVideos[0]} failed");
                            Console.WriteLine("Failure to download");
                            Close();
                            break;
                        case AppleMusicDownloadResponse.InvalidUserToken:
                            ToastManager.CreateToast()
                                .WithTitle("Download Error")
                                .WithContent("Invalid user token provided! Please update token in config")
                                .OfType(NotificationType.Error)
                                .Queue();
                            Log.Error($"Error in AddMusicVideo->HandleSave: Invalid user token");
                            Console.WriteLine("Invalid user token provided!");
                            Close();
                            break;
                        case AppleMusicDownloadResponse.InvalidDeviceFiles:
                            ToastManager.CreateToast()
                                .WithTitle("Download Error")
                                .WithContent("Invalid or missing device files! Please store these in assets folder")
                                .OfType(NotificationType.Error)
                                .Queue();
                            Log.Error($"Error in AddMusicVideo->HandleSave: Invalid device files");
                            Console.WriteLine("Invalid device files provided!");
                            Close();
                            break;
                    }
                    break;
                case SearchSource.Manual:
                    if (_manualVM.YTVisible)
                    {
                        //If only downloading 1 manual YT video, add it to the download list
                        if (_manualVM.ManualItems.Count == 0)
                        {
                            _manualVM.AddSingleToList();
                        }
                        //SaveVideo();
                        SaveMultipleManualVideos();
                    }
                    else
                    {
                        SaveManualVideo();
                    }

                    break;
            }
        }

        public async Task<AppleMusicDownloadResponse> SaveAppleMusicVideo()
        {
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            VideoResultViewModel selectedVideo = _resultsVM.SelectedVideos[0];
            CurrentContent = waveVM;
            //If the artist folder doesn't exist yet, create it
            if (!Directory.Exists($"{_settings.RootFolder}\\{selectedVideo.Artist.Name}"))
            {
                Directory.CreateDirectory($"{_settings.RootFolder}\\{selectedVideo.Artist.Name}");
            }
            AppleMusicDownloadResponse result = await _amDLHelper.DownloadVideo(selectedVideo, waveVM);
            if (result == AppleMusicDownloadResponse.Success)
            {
                await selectedVideo.GenerateNFO($"{_settings.RootFolder}/{selectedVideo.Artist.Name}/{selectedVideo.Title}.mp4",
                    SearchSource.AppleMusic);
                selectedVideo.HandleDownload();
                CurrentContent = _resultsVM;
                NavVisible = true;
            }
            return result;
        }

        public async Task<bool> SaveMultipleManualVideos()
        {
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            CurrentContent = waveVM;
            NavVisible = false;
            for (int i = 0; i < _manualVM.ManualItems.Count; i++)
            {
                string headerText = "";
                if (_manualVM.ManualItems.Count > 1)
                {
                    headerText = $"Downloading {_manualVM.ManualItems[i].Title} {i + 1}/{_manualVM.ManualItems.Count}";
                }
                else
                {
                    headerText = $"Downloading {_manualVM.ManualItems[i].Title}";
                }

                waveVM.HeaderText = headerText;
                var progressTest = new ProgressBar() { Value = 0, ShowProgressText = true };
                var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress, ref progressTest));
                var toastTest = App.GetVM().GetToastManager().CreateToast()
                    .WithTitle(headerText)
                    .WithContent(progressTest)
                    .Queue();
                var downResult = await _ytDLHelper.DownloadVideo(_manualVM.ManualItems[i].VidID, $"{_settings.RootFolder}/{_manualVM.Artist.Name}", _manualVM.ManualItems[i].Title, progress);
                App.GetVM().GetToastManager().Dismiss(toastTest);
                if (downResult.Success)
                {
                    RefreshAlbumEvent?.Invoke(null, true);
                    if (_edit)
                    {
                        //Special edit method to handle replacing a pre-existing video - still uses the same post download process to reset values
                        await _manualVM.GenerateManualNFO(downResult.Data, true).ContinueWith(t => PostDownloadProcess(t, waveVM, i, downResult), TaskScheduler.FromCurrentSynchronizationContext());
                    }
                    else
                    {
                        await _manualVM.GenerateManualNFO(downResult.Data, i).ContinueWith(t => PostDownloadProcess(t, waveVM, i, downResult), TaskScheduler.FromCurrentSynchronizationContext());
                    }
                }
                else
                {
                    ToastManager.CreateToast()
                        .WithTitle("Error")
                        .WithContent($"Something went wrong with downloading {_manualVM.ManualItems[i].Title}")
                        .OfType(NotificationType.Error)
                        .Queue();
                    Log.Error($"Error in AddMusicVideo->SaveMultipleManualVideos: {_manualVM.ManualItems[i].VidID} failed");
                    foreach (var err in downResult.ErrorOutput)
                    {
                        Log.Error(err);
                    }
                    break;
                }
            }
            NavVisible = true;
            return true;
        }

        private void PostDownloadProcess(Task<int> t, WaveProgressViewModel waveVM, int i, RunResult<string> downResult)
        {
            if (t.IsCompletedSuccessfully)
            {
                //Reset wave progress
                waveVM.UpdateProgress(0);
            }
            else
            {
                ToastManager.CreateToast()
                    .WithTitle("Error")
                    .WithContent($"Something went wrong with downloading {_manualVM.ManualItems[i].Title}")
                    .OfType(NotificationType.Error)
                    .Queue();
                Log.Error($"Error in AddMusicVideo->SaveMultipleManualVideos: {_manualVM.ManualItems[i].VidID} failed");
                foreach (var err in downResult.ErrorOutput)
                {
                    Log.Error(err);
                }
            }
        }

        public async Task<bool> SaveMultipleVideos()
        {
            WaveProgressViewModel waveVM = new WaveProgressViewModel();
            CurrentContent = waveVM;
            NavVisible = false;
            for (int i = 0; i < _resultsVM.SelectedVideos.Count; i++)
            {
                string headerText = "";
                if (_resultsVM.SelectedVideos.Count > 1)
                {
                    headerText = $"Downloading {_resultsVM.SelectedVideos[i].Title} {i + 1}/{_resultsVM.SelectedVideos.Count}";
                }
                else
                {
                    headerText = $"Downloading {_resultsVM.SelectedVideos[i].Title}";
                }

                waveVM.HeaderText = headerText;
                var progressTest = new ProgressBar() { Value = 0, ShowProgressText = true };
                var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress, ref progressTest));
                var currResult = _resultsVM.SelectedVideos[i].HandleDownload();
                var toastTest = App.GetVM().GetToastManager().CreateToast()
                    .WithTitle(headerText)
                    .WithContent(progressTest)
                    .Queue();
                var downResult = await _ytDLHelper.DownloadVideo(currResult.VideoID, $"{_settings.RootFolder}\\{currResult.Artist.Name}", currResult.Title, progress);
                App.GetVM().GetToastManager().Dismiss(toastTest);
                if (downResult.Success)
                {
                    RefreshAlbumEvent?.Invoke(null, true);
                    _resultsVM.SelectedVideos[i].GenerateNFO(downResult.Data, SearchSource.YouTubeMusic).ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            waveVM.UpdateProgress(0);
                        }
                        else
                        {
                            ToastManager.CreateToast()
                                .WithTitle("Error")
                                .WithContent($"Something went wrong with downloading {_resultsVM.SelectedVideos[i].Title}")
                                .OfType(NotificationType.Error)
                                .Queue();
                            Log.Error($"Error in AddMusicVideo->SaveMultipleVideos: {currResult.VideoID} failed");
                            foreach (var err in downResult.ErrorOutput)
                            {
                                Log.Error(err);
                            }
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    ToastManager.CreateToast()
                        .WithTitle("Error")
                        .WithContent($"Something went wrong with downloading {_resultsVM.SelectedVideos[i].Title}")
                        .OfType(NotificationType.Error)
                        .Queue();
                    Log.Error($"Error in AddMusicVideo->SaveMultipleVideos: {currResult.VideoID} failed");
                    foreach (var err in downResult.ErrorOutput)
                    {
                        Log.Error(err);
                    }
                    break;
                }
            }
            NavVisible = true;
            return true;
        }

        public void SaveManualVideo()
        {
            string newPath;
            //If the local file already matches the 'expected file naming structure', don't rename it further
            if (Path.GetFileName(_manualVM.VideoPath) !=
                $"{_manualVM.Title}{Path.GetExtension(_manualVM.VideoPath)}")
            { newPath = $"{_settings.RootFolder}\\{_manualVM.Artist.Name}\\{_manualVM.Title}{Path.GetExtension(_manualVM.VideoPath)}"; }
            else { newPath = $"{_settings.RootFolder}\\{_manualVM.Artist.Name}\\{Path.GetFileName(_manualVM.VideoPath)}"; }
            //If the artist folder doesn't exist yet, create it
            if (!Directory.Exists($"{_settings.RootFolder}\\{_manualVM.Artist.Name}"))
            {
                Directory.CreateDirectory($"{_settings.RootFolder}\\{_manualVM.Artist.Name}");
            }
            if (_edit)
            {
                //If for some reason someone selects a video in the destination path...
                if(_manualVM.PreviousVideo.vidPath != newPath)
                {
                    //Delete the original video and thumbnail
                    File.Delete(_manualVM.PreviousVideo.vidPath);
                    File.Delete($"{_settings.RootFolder}\\{_manualVM.Artist.Name}\\{_manualVM.PreviousVideo.thumb}");
                    //Move the new one over
                    File.Move(_manualVM.VideoPath, newPath);
                }
                //Re-generate the NFO
                _manualVM.GenerateManualNFO(newPath, true);
            }
            else
            {
                //Move the local file over
                File.Move(_manualVM.VideoPath, newPath);
                //Generate a new NFO
                _manualVM.GenerateManualNFO(newPath, false);
            }
            NavVisible = true;
        }

        public void HandleDismiss()
        {        
            App.GetToastManager().CreateToast()
            .WithTitle(currentDownload.HeaderText)
            .WithLoadingState(true)
            .WithContent($"{currentDownload.ProgressValue}%/100%")
            .Dismiss().ByClicking()
            .Queue();
        }

        public void Close()
        {
            _resultsVM = null;
            _manualVM = null;
            DialogManager.DismissDialog();
        }
    }
}