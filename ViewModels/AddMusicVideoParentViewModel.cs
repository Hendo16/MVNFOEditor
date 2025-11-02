using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using MVNFOEditor.DB;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using MVNFOEditor.Settings;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using YoutubeDLSharp;

namespace MVNFOEditor.ViewModels;

public partial class AddMusicVideoParentViewModel : ObservableObject
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(AddMusicVideoParentViewModel));
    private static ISettings _settings;
    private readonly AppleMusicDLHelper _amDLHelper;
    private readonly bool _edit;
    private readonly SearchSource _searchSource;
    private readonly YTDLHelper _ytDLHelper;
    private readonly ISukiDialogManager DialogManager;
    private readonly ISukiToastManager ToastManager;

    [ObservableProperty] private object _currentContent;
    private MusicDbContext _dbContext;
    [ObservableProperty] private string _errorText;
    private ManualMusicVideoViewModel? _manualVM;
    [ObservableProperty] private bool _navVisible;
    private VideoResultsViewModel? _resultsVM;
    [ObservableProperty] private bool _showError;
    [ObservableProperty] private bool _toggleValue;
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

    public event EventHandler<bool> RefreshAlbumEvent;

    public async void SaveAMVideo(object? sender, VideoResultViewModel selectedVideo)
    {
        var waveVM = new WaveProgressViewModel();
        currentDownload = waveVM;
        CurrentContent = waveVM;
        //If the artist folder doesn't exist yet, create it
        if (!Directory.Exists($"{_settings.RootFolder}\\{selectedVideo.Artist.Name}"))
            Directory.CreateDirectory($"{_settings.RootFolder}\\{selectedVideo.Artist.Name}");
        var result = await _amDLHelper.DownloadVideo(selectedVideo, waveVM);
        if (result == AppleMusicDownloadResponse.Success)
        {
            await selectedVideo.GenerateNFO(
                $"{_settings.RootFolder}/{selectedVideo.Artist.Name}/{selectedVideo.Title}.mp4",
                SearchSource.AppleMusic);
            selectedVideo.HandleDownload();
            CurrentContent = _resultsVM;
            NavVisible = true;
        }
        else
        {
            DialogManager.DismissDialog();
            ToastHelper.ShowError("Download Failed", "Error downloading video, please check logs");
            Log.Error("Error in AddMusicVideo->SaveAMVideo: Download failed");
        }
    }

    public async void SaveYTMVideo(object? sender, VideoResultViewModel? _resultVM)
    {
        /*
        NavVisible = false;
        var _result = _resultVM.GetResult();
        var waveVM = new WaveProgressViewModel(true, circleVisible: true);
        waveVM.HeaderText = $"Downloading {_result.Name} - ";
        CurrentContent = waveVM;
        var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress));
        var downloadResult = await _ytDLHelper.DownloadVideo(_result.SourceId,
            $"{_settings.RootFolder}/{_result.Artist.Name}", _result.Name, progress);
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
            ToastHelper.ShowError("Download Error", errorContent[0]);
            Log.Error($"Error in AddMusicVideo->SaveYTMVideo: {_result.SourceId} failed");
            foreach (var err in downloadResult.ErrorOutput) Log.Error(err);
        }
        */
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
                var result = await SaveAppleMusicVideo();
                switch (result)
                {
                    case AppleMusicDownloadResponse.Success:
                        Console.WriteLine("Done!");
                        break;
                    case AppleMusicDownloadResponse.Failure:
                        ToastHelper.ShowError("Download Failed", "Download failed, please check logs");
                        Log.Error($"Error in AddMusicVideo->HandleSave: {_resultsVM.SelectedVideos[0]} failed");
                        Close();
                        break;
                    case AppleMusicDownloadResponse.InvalidUserToken:
                        ToastHelper.ShowError("Download Error", "Invalid user token provided! Please update token in config");
                        Log.Error("Error in AddMusicVideo->HandleSave: Invalid user token");
                        Close();
                        break;
                    case AppleMusicDownloadResponse.InvalidDeviceFiles:
                        ToastHelper.ShowError("Download Error", "Invalid or missing device files! Please store these in assets folder");
                        Log.Error("Error in AddMusicVideo->HandleSave: Invalid device files");
                        Close();
                        break;
                }

                break;
            case SearchSource.Manual:
                if (_manualVM.YTVisible)
                {
                    //If only downloading 1 manual YT video, add it to the download list
                    if (_manualVM.ManualItems.Count == 0) _manualVM.AddSingleToList();
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
        var waveVM = new WaveProgressViewModel(circleVisible: true);
        var selectedVideo = _resultsVM.SelectedVideos[0];
        CurrentContent = waveVM;
        //If the artist folder doesn't exist yet, create it
        if (!Directory.Exists($"{_settings.RootFolder}\\{selectedVideo.Artist.Name}"))
            Directory.CreateDirectory($"{_settings.RootFolder}\\{selectedVideo.Artist.Name}");
        var result = await _amDLHelper.DownloadVideo(selectedVideo, waveVM);
        if (result == AppleMusicDownloadResponse.Success)
        {
            await selectedVideo.GenerateNFO(
                $"{_settings.RootFolder}/{selectedVideo.Artist.Name}/{selectedVideo.Title}.mp4",
                SearchSource.AppleMusic);
            selectedVideo.HandleDownload();
            CurrentContent = _resultsVM;
            NavVisible = true;
        }

        return result;
    }

    public async Task<bool> SaveMultipleManualVideos()
    {
        
        /*
    var waveVM = new WaveProgressViewModel(true, circleVisible: true);
    CurrentContent = waveVM;
    NavVisible = false;
    for (var i = 0; i < _manualVM.ManualItems.Count; i++)
    {
        var headerText = "";
        if (_manualVM.ManualItems.Count > 1)
            headerText = $"Downloading {_manualVM.ManualItems[i].Title} {i + 1}/{_manualVM.ManualItems.Count}";
        else
            headerText = $"Downloading {_manualVM.ManualItems[i].Title}";

        waveVM.HeaderText = headerText;
        var progressTest = new ProgressBar { Value = 0, ShowProgressText = true };
        var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress, ref progressTest));
        var toastTest = App.GetVM().GetToastManager().CreateToast()
            .WithTitle(headerText)
            .WithContent(progressTest)
            .Queue();
        var downResult = await _ytDLHelper.DownloadVideo(_manualVM.ManualItems[i].VidID,
            $"{_settings.RootFolder}/{_manualVM.Artist.Name}", _manualVM.ManualItems[i].Title, progress);
        App.GetVM().GetToastManager().Dismiss(toastTest);
        if (downResult.Success)
        {
            RefreshAlbumEvent?.Invoke(null, true);
            if (_edit)
                //Special edit method to handle replacing a pre-existing video - still uses the same post download process to reset values
                await _manualVM.GenerateManualNFO(downResult.Data, true).ContinueWith(
                    t => PostDownloadProcess(t, waveVM, i, downResult),
                    TaskScheduler.FromCurrentSynchronizationContext());
            else
                await _manualVM.GenerateManualNFO(downResult.Data, i).ContinueWith(
                    t => PostDownloadProcess(t, waveVM, i, downResult),
                    TaskScheduler.FromCurrentSynchronizationContext());
        }
        else
        {
            ToastHelper.ShowError("Download Error", $"Something went wrong with downloading {_manualVM.ManualItems[i].Title}");
            Log.Error($"Error in AddMusicVideo->SaveMultipleManualVideos: {_manualVM.ManualItems[i].VidID} failed");
            foreach (var err in downResult.ErrorOutput) Log.Error(err);
            break;
        }
    }

    NavVisible = true;
    return true;
            */
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
            ToastHelper.ShowError("Download Error", $"Something went wrong with downloading {_manualVM.ManualItems[i].Title}");
            Log.Error($"Error in AddMusicVideo->SaveMultipleManualVideos: {_manualVM.ManualItems[i].VidID} failed");
            foreach (var err in downResult.ErrorOutput) Log.Error(err);
        }
    }

    public async Task<bool> SaveMultipleVideos()
    {
        /*
        var waveVM = new WaveProgressViewModel(true, circleVisible: true);
        CurrentContent = waveVM;
        NavVisible = false;
        for (var i = 0; i < _resultsVM.SelectedVideos.Count; i++)
        {
            var headerText = "";
            if (_resultsVM.SelectedVideos.Count > 1)
                headerText =
                    $"Downloading {_resultsVM.SelectedVideos[i].Title} {i + 1}/{_resultsVM.SelectedVideos.Count}";
            else
                headerText = $"Downloading {_resultsVM.SelectedVideos[i].Title}";

            waveVM.HeaderText = headerText;
            var progressTest = new ProgressBar { Value = 0, ShowProgressText = true };
            var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress, ref progressTest));
            var currResult = _resultsVM.SelectedVideos[i].HandleDownload();
            var toastTest = App.GetVM().GetToastManager().CreateToast()
                .WithTitle(headerText)
                .WithContent(progressTest)
                .Queue();
            var downResult = await _ytDLHelper.DownloadVideo(currResult.SourceId,
                $"{_settings.RootFolder}\\{currResult.Artist.Name}", currResult.Name, progress);
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
                        ToastHelper.ShowError("NFO Error", $"Something went wrong with generating the NFO for {currResult.Name}");
                        Log.Error($"Error in AddMusicVideo->SaveMultipleVideos->GenerateNFO: {currResult.SourceId} failed");
                        foreach (var err in downResult.ErrorOutput) Log.Error(err);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                ToastHelper.ShowError("Download Error", $"Something went wrong with downloading {_resultsVM.SelectedVideos[i].Title}");
                Log.Error($"Error in AddMusicVideo->SaveMultipleVideos: {currResult.SourceId} failed");
                foreach (var err in downResult.ErrorOutput) Log.Error(err);
                break;
            }
        }

        NavVisible = true;
        */
        return true;
    }

    public void SaveManualVideo()
    {
        string newPath;
        //If the local file already matches the 'expected file naming structure', don't rename it further
        if (Path.GetFileName(_manualVM.VideoPath) !=
            $"{_manualVM.Title}{Path.GetExtension(_manualVM.VideoPath)}")
            newPath =
                $"{_settings.RootFolder}\\{_manualVM.Artist.Name}\\{_manualVM.Title}{Path.GetExtension(_manualVM.VideoPath)}";
        else
            newPath = $"{_settings.RootFolder}\\{_manualVM.Artist.Name}\\{Path.GetFileName(_manualVM.VideoPath)}";
        //If the artist folder doesn't exist yet, create it
        if (!Directory.Exists($"{_settings.RootFolder}\\{_manualVM.Artist.Name}"))
            Directory.CreateDirectory($"{_settings.RootFolder}\\{_manualVM.Artist.Name}");
        if (_edit)
        {
            //If for some reason someone selects a video in the destination path...
            if (_manualVM.PreviousVideo.vidPath != newPath)
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

    public void Close()
    {
        _resultsVM = null;
        _manualVM = null;
        DialogManager.DismissDialog();
    }
}