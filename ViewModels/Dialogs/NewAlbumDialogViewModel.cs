using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using Microsoft.EntityFrameworkCore;
using MVNFOEditor.DB;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using MVNFOEditor.Settings;
using SukiUI.Toasts;
using YoutubeDLSharp;

namespace MVNFOEditor.ViewModels;

public partial class NewAlbumDialogViewModel : ObservableObject
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(AddMusicVideoParentViewModel));
    private static ISettings _settings;
    private readonly MusicDbContext _dbContext;
    private readonly iTunesAPIHelper _iTunesApiHelper;
    private readonly ArtistListParentViewModel _parentVM;
    private readonly AlbumResultsViewModel _resultVM;
    private readonly YTDLHelper _ytDLHelper;
    private readonly bool JustAlbum;
    private readonly YTMusicHelper ytMusicHelper;
    [ObservableProperty] private bool _amChecked;
    [ObservableProperty] private bool _amEnabled;
    [ObservableProperty] private string _backButtonText;
    [ObservableProperty] private string _busyText;

    [ObservableProperty] private Artist _currArtist;
    [ObservableProperty] private object _currentContent;
    [ObservableProperty] private string _errorText;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _manChecked;
    private ManualAlbumViewModel? _manualAlbumVM;
    private ManualMusicVideoViewModel? _manualMVVM;

    [ObservableProperty] private bool _navVisible;
    [ObservableProperty] private bool _notDownload;
    [ObservableProperty] private string _saveButtonText;
    [ObservableProperty] private bool _showError;

    private int _stepIndex;
    [ObservableProperty] private ObservableCollection<string> _steps;
    private VideoResultsViewModel? _syncVM;
    [ObservableProperty] private bool _toggleEnable;
    [ObservableProperty] private bool _toggleValue;
    [ObservableProperty] private bool _toggleVisible;
    [ObservableProperty] private bool _ytChecked;
    [ObservableProperty] private bool _ytEnabled;

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

    public NewAlbumDialogViewModel(AlbumResultsViewModel vm, Artist currArtist, SearchSource source)
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
        SetupSource(source);
    }

    public int StepIndex
    {
        get => _stepIndex;
        set
        {
            _stepIndex = value;
            OnPropertyChanged();
        }
    }

    public event EventHandler<bool> ClosePageEvent;

    public async Task<bool> GenerateNewResults(SearchSource source)
    {
        return await _resultVM.GenerateNewResults(source);
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
        switch (newAlbum.Source)
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
        var currentType = CurrentContent.GetType();
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
            if (ValidateManualAlbum())
            {
                ToggleVisible = false;
                ToggleEnable = false;
                var album = await _manualAlbumVM.SaveAlbum();
                if (!JustAlbum)
                {
                    var newVM = new ManualMusicVideoViewModel(album);
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
                    CurrentContent = _resultVM;
                else
                    CurrentContent = _manualAlbumVM;
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
                SaveMultipleVideos();
            else
                CurrentContent = _resultVM;
        }
    }

    private async Task<bool> GetAMVideos(AlbumResult newAlbum)
    {
        /*
        Album album;
        if (!_dbContext.Album.Any(a => a.AlbumBrowseID == newAlbum.SourceId))
        {
            album = await Album.CreateAlbum(newAlbum, SearchSource.AppleMusic);
            _dbContext.Album.Add(album);
            await _dbContext.SaveChangesAsync();
            _parentVM.RefreshDetails();
        }
        else
        {
            album = _dbContext.Album.Include(alb => alb.Artist).First(a => a.AlbumBrowseID == newAlbum.SourceId);
        }

        var results = await _iTunesApiHelper.GenerateVideoResultList(album);
        if (results.Count == 0)
        {
            var manualVM = new ManualMusicVideoViewModel(_currArtist);
            ToastHelper.ShowError("Album Videos", "No Videos Available");
            manualVM.CurrAlbum = album;
            await Dispatcher.UIThread.InvokeAsync(() => { CurrentContent = manualVM; });
            return true;
        }

        var resultsVM = new VideoResultsViewModel(results);
        var artistMetadata = album.Artist.GetArtistMetadata(SearchSource.AppleMusic);
        var parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);

        _syncVM = resultsVM;
        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            result.ProgressStarted += parentVM.SaveAMVideo;
        }

        await Dispatcher.UIThread.InvokeAsync(() => { CurrentContent = resultsVM; });
        */
        return true;
    }

    private async Task<bool> GetYTMusicVideos(AlbumResult newAlbum)
    {
        /*
        Album album;
        if (!_dbContext.Album.Any(a => a.AlbumBrowseID == newAlbum.SourceId))
        {
            album = await Album.CreateAlbum(newAlbum, SearchSource.YouTubeMusic);
            _dbContext.Album.Add(album);
            await _dbContext.SaveChangesAsync();
            _parentVM.RefreshDetails();
        }
        else
        {
            album = _dbContext.Album.Include(alb => alb.Artist).First(a => a.AlbumBrowseID == newAlbum.SourceId);
        }

        var artistMetadata = album.Artist.GetArtistMetadata(SearchSource.YouTubeMusic);
        var artistID = artistMetadata.BrowseId;
        var videos = await album.Artist.GetVideos(SearchSource.YouTubeMusic);
        var fullAlbum = await ytMusicHelper.GetAlbum(album.AlbumBrowseID);
        var results = await ytMusicHelper.GenerateVideoResultList(videos, fullAlbum, album);

        if (results.Count == 0)
        {
            var manualVM = new ManualMusicVideoViewModel(_currArtist);
            ToastHelper.ShowError("Album Videos", "No Videos Available");
            manualVM.CurrAlbum = album;
            await Dispatcher.UIThread.InvokeAsync(() => { CurrentContent = manualVM; });
            return true;
        }

        var resultsVM = new VideoResultsViewModel(results);
        var parentVM = new AddMusicVideoParentViewModel(resultsVM, artistMetadata.SourceId);

        _syncVM = resultsVM;
        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            result.ProgressStarted += parentVM.SaveYTMVideo;
        }

        await Dispatcher.UIThread.InvokeAsync(() => { CurrentContent = resultsVM; });
        */
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
        /*
        var waveVM = new WaveProgressViewModel(true, circleVisible: true);
        CurrentContent = waveVM;
        NavVisible = false;
        for (var i = 0; i < _manualMVVM.ManualItems.Count; i++)
        {
            var headerText = "";
            if (_manualMVVM.ManualItems.Count > 1)
                headerText = $"Downloading {_manualMVVM.ManualItems[i].Title} {i + 1}/{_manualMVVM.ManualItems.Count}";
            else
                headerText = $"Downloading {_manualMVVM.ManualItems[i].Title}";

            waveVM.HeaderText = headerText;
            var progressTest = new ProgressBar { Value = 0, ShowProgressText = true };
            var progress = new Progress<DownloadProgress>(p => waveVM.UpdateProgress(p.Progress, ref progressTest));
            var toastTest = App.GetVM().GetToastManager().CreateToast()
                .WithTitle(headerText)
                .WithContent(progressTest)
                .Queue();
            var downResult = await _ytDLHelper.DownloadVideo(_manualMVVM.ManualItems[i].VidID,
                $"{_settings.RootFolder}/{_manualMVVM.Artist.Name}", _manualMVVM.ManualItems[i].Title, progress);
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
                            .Dismiss().ByClicking()
                            .Queue();
                        Log.Error(
                            $"Error in AddMusicVideo->SaveMultipleManualVideos: {_manualMVVM.ManualItems[i].VidID} failed");
                        foreach (var err in downResult.ErrorOutput) Log.Error(err);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                App.GetVM().GetToastManager().CreateToast()
                    .WithTitle("Error")
                    .WithContent($"Something went wrong with downloading {_manualMVVM.ManualItems[i].Title}")
                    .OfType(NotificationType.Error)
                    .Dismiss().ByClicking()
                    .Queue();
                Log.Error(
                    $"Error in AddMusicVideo->SaveMultipleManualVideos: {_manualMVVM.ManualItems[i].VidID} failed");
                foreach (var err in downResult.ErrorOutput) Log.Error(err);
                break;
            }
        }

        CloseDialog();
        */
    }

    private async void SaveMultipleVideos()
    {
        /*
        NavVisible = false;
        var waveVm = new WaveProgressViewModel(true, circleVisible: true);
        CurrentContent = waveVm;
        for (var i = 0; i < _syncVM.SelectedVideos.Count; i++)
        {
            if (_syncVM.SelectedVideos.Count > 1)
                waveVm.HeaderText =
                    $"Downloading {_syncVM.SelectedVideos[i].Title} {i + 1}/{_syncVM.SelectedVideos.Count}";
            else
                waveVm.HeaderText = $"Downloading {_syncVM.SelectedVideos[i].Title}";
            var progress = new Progress<DownloadProgress>(p => waveVm.UpdateProgress(p.Progress));
            var currResult = _syncVM.SelectedVideos[i].HandleDownload();
            var downResult = await _ytDLHelper.DownloadVideo(currResult.SourceId,
                $"{_settings.RootFolder}/{currResult.Artist.Name}", currResult.Name, progress);
            if (downResult.Success)
            {
                _syncVM.SelectedVideos[i].GenerateNFO(downResult.Data, SearchSource.YouTubeMusic).ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        waveVm.UpdateProgress(0);
                    }
                    else
                    {
                        App.GetVM().GetToastManager().CreateToast()
                            .WithTitle("Error")
                            .WithContent($"Something went wrong with downloading {_syncVM.SelectedVideos[i].Title}")
                            .OfType(NotificationType.Error)
                            .Queue();
                        Log.Error($"Error in NewAlbum->SaveMultipleVideos: {currResult.SourceId} failed");
                        foreach (var err in downResult.ErrorOutput) Log.Error(err);
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
                Log.Error($"Error in NewAlbum->SaveMultipleVideos: {currResult.SourceId} failed");
                foreach (var err in downResult.ErrorOutput) Log.Error(err);
                break;
            }
        }

        CloseDialog();
        */
    }

    public void BackTrigger()
    {
        HandleNavigation(false);
    }

    public void NextTrigger()
    {
        HandleNavigation(true);
    }

    [RelayCommand]
    public void CloseDialog()
    {
        ClosePageEvent?.Invoke(this, true);
        App.GetVM().GetDialogManager().DismissDialog();
    }

    #region StepperChecked

    public void ManualChecked()
    {
        var newVM = new ManualAlbumViewModel(CurrArtist);
        Steps = ["Create Album", "Create Video"];
        CurrentContent = newVM;
        _manualAlbumVM = newVM;
    }

    public async void YouTubeChecked()
    {
        if (!await GenerateNewResults(SearchSource.YouTubeMusic))
            App.GetVM().GetToastManager().CreateToast()
                .WithTitle("Error")
                .WithContent("No Albums Found on YouTube")
                .OfType(NotificationType.Error)
                .Dismiss()
                .After(TimeSpan.FromSeconds(5))
                .Queue();
    }

    public async void AppleMusicChecked()
    {
        if (!await GenerateNewResults(SearchSource.AppleMusic))
            App.GetVM().GetToastManager().CreateToast()
                .WithTitle("Error")
                .WithContent("No Albums Found on Apple Music")
                .OfType(NotificationType.Error)
                .Dismiss()
                .After(TimeSpan.FromSeconds(5))
                .Queue();
    }

    #endregion
}