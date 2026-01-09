using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using MVNFOEditor.Views;
using YoutubeDLSharp;

namespace MVNFOEditor.ViewModels;

public partial class NewResultDialogViewModel : ObservableObject
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(NewResultDialogViewModel));
    private SearchSource currentSource;
    private readonly Type _currentType;
    [ObservableProperty] private bool _amChecked;
    [ObservableProperty] private bool _manualChecked;
    [ObservableProperty] private bool _ytChecked = true;
    //Artist Result Handler
    internal NewResultDialogViewModel(Type resultType)
    {
        _currentType = resultType;
    }

    public static NewResultDialogViewModel CreateResultSearch(Type resultType, 
        Artist? currArtist = null,
        Album? currAlbum = null,
        SearchSource source = SearchSource.YouTubeMusic)
    {
        NewResultDialogViewModel newVm = new NewResultDialogViewModel(resultType);
        if (currAlbum != null && currAlbum.Metadata.Any(am => am.SourceId != SearchSource.YouTubeMusic))
        {
            if (currAlbum.Metadata.Any(am => am.SourceId == SearchSource.Manual))
            {
                source = SearchSource.Manual;
                newVm.YtChecked = newVm.AmChecked = false;
                newVm.ManualChecked = true;
            }
            else
            {
                source = SearchSource.AppleMusic;
                newVm.YtChecked = newVm.ManualChecked = false;
                newVm.AmChecked = true;
            }
        }
        newVm.SetupUi(resultType, source, currArtist, currAlbum);
        newVm.currentSource = source;
        return newVm;
    }


    #region UI Bindings

    #region Enabled

    [ObservableProperty] private bool _stepperEnabled = true;
    [ObservableProperty] private bool _ytEnabled = true;
    [ObservableProperty] private bool _amEnabled = App.GetSettings().AM_AccessToken != "n/a";

    #endregion

    #region Visible

    [ObservableProperty] private bool _navVisible = true;
    [ObservableProperty] private bool _showError;
    [ObservableProperty] private bool _showStepper;

    #endregion

    #region Text

    [ObservableProperty] private string _backButtonText;
    [ObservableProperty] private string _nextButtonText;
    [ObservableProperty] private string _busyText;
    [ObservableProperty] private string _errorText;

    #endregion

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private int _stepIndex;
    [ObservableProperty] private object _currentContent;
    [ObservableProperty] private ObservableCollection<string> _steps;

    #endregion

    #region Event Handlers

    public event EventHandler<bool> ClosePageEvent;

    #endregion
    
    #region Result Handlers
    //Artist Result Handler
    private ArtistResultsViewModel ArtistResultsSetup()
    {
        var newVm = new ArtistResultsViewModel();
        newVm.ValidSearch += ShowNav;
        newVm.DisplayError += DisplayError;
        newVm.HideError += HideError;
        return newVm;
    }

    //Album Result Handler
    private async Task<object> AlbumResultsSetup(Artist artist,
        SearchSource source)
    {
        if (source == SearchSource.Manual)
        {
            ManualChecked = true;
            return new ManualAlbumViewModel(artist);
        }
        var newVm = await AlbumResultsViewModel.CreateViewModel(artist, source);
        newVm.ShowNav += ShowNav;
        return newVm;
    }

    //Video Result Handler
    private async Task<object?> VideoResultsSetup(Artist artist, SearchSource source, Album? album = null)
    {
        if (source == SearchSource.Manual)
        {
            ManualChecked = true;
            return album != null ? new ManualMusicVideoViewModel(album) : new ManualMusicVideoViewModel(artist);
        }
        var newVm = await VideoResultsViewModel.CreateViewModel(source, artist, album);
        return newVm;
    }
    #endregion
    
    #region UI Handler
    private async void SetupUi(Type resultType,
        SearchSource source, 
        Artist? currArtist = null,
        Album? currAlbum = null)
    {
        NextButtonText = "Next";
        BackButtonText = "Exit";
        ShowStepper = true;
        switch (resultType.Name)
        {
            case nameof(ArtistResult):
                Steps = ["Select Artist", "Select Album", "Select Videos"];
                CurrentContent = ArtistResultsSetup();
                break;
            case nameof(AlbumResult):
                if (currArtist is null)
                {
                    ToastHelper.ShowError("Result Error", "Couldn't find Artist, please check logs");
                    Log.Error("Error: SetupUi provided null artist for AlbumResults");
                    CloseDialog();
                    return;
                }
                DisableCheckers(currArtist);
                Steps = ["Select Album", "Select Videos"];
                SetProcessing(true, "Getting Albums...");
                StepIndex++;
                CurrentContent = await AlbumResultsSetup(currArtist, source);
                //Results will be loading in
                SetProcessing(false);
                break;
            case nameof(VideoResult):
                if (currArtist is null)
                {
                    ToastHelper.ShowError("Result Error", "Couldn't find Artist, please check logs");
                    Log.Error("Error: SetupUi provided null artist for AlbumResults");
                    CloseDialog();
                    return;
                }

                if (currAlbum != null)
                {
                    DisableCheckers(currAlbum);
                }
                else
                {
                    DisableCheckers(currArtist);
                }
                Steps = ["Select Videos"];
                NextButtonText = "Download";
                SetProcessing(true, "Getting Videos...");
                var vidResultsView = await VideoResultsSetup(currArtist, source, currAlbum);
                if (vidResultsView is null)
                {
                    ToastHelper.ShowError("Result Error", "Couldn't load Videos, please check logs");
                    Log.Error("Error: VideoResultsSetup provided null vidResultsView");
                    CloseDialog();
                    return;
                }
                StepIndex++;
                CurrentContent = vidResultsView;
                //Results will be loading in
                SetProcessing(false);
                break;
        }
    }

    public void ShowNav(object? sender, bool result)
    {
        NavVisible = result;
    }

    public void HideError(object? sender, bool result)
    {
        ShowError = false;
    }

    public void DisplayError(object? sender, string text)
    {
        ShowError = true;
        ErrorText = text;
    }

    private void SetProcessing(bool isLoading, string text = "")
    {
        IsBusy = isLoading;
        BusyText = text;
    }
    #endregion

    #region Stepper Navigation

    public void ManualSelected()
    {
        Console.WriteLine("Manual!");
        currentSource = SearchSource.Manual;
        SetupManual();
    }

    
    public void YtSelected()
    {
        Console.WriteLine("YT!");
        currentSource = SearchSource.YouTubeMusic;
        RefreshResultList();
    }

    public void AmSelected()
    {
        Console.WriteLine("AM!");
        currentSource = SearchSource.AppleMusic;
        RefreshResultList();
    }

    private void DisableCheckers(Album album)
    {
        if (currentSource == SearchSource.Manual)
        {
            AmEnabled = YtEnabled = false;
            return;
        }
        AmEnabled = album.GetSources().Contains(SearchSource.AppleMusic);
        YtEnabled = album.GetSources().Contains(SearchSource.YouTubeMusic);
    }

    private void DisableCheckers(Artist artist)
    {
        if (currentSource == SearchSource.Manual)
        {
            AmEnabled = YtEnabled = false;
            return;
        }
        AmEnabled = artist.GetSources().Contains(SearchSource.AppleMusic);
        YtEnabled = artist.GetSources().Contains(SearchSource.YouTubeMusic);
        if (AmEnabled && !YtEnabled)
        {
            YtChecked = false;
            AmChecked = true;
        }
    }

    private void DisableCheckers(SearchSource selectedSource)
    {
        switch (selectedSource)
        {
            case SearchSource.AppleMusic:
                YtEnabled = false;
                break;
            case SearchSource.YouTubeMusic:
                AmEnabled = false;
                break;
            case SearchSource.Manual:
                YtEnabled = AmEnabled = false;
                break;
        }
    }

    #endregion

    #region Dialog Navigation

    public void BackTrigger()
    {
        HandleNavigation(false);
    }

    public void NextTrigger()
    {
        HandleNavigation(true);
    }

    private async void HandleNavigation(bool isIncrement)
    {
        Result? selectedResult = null;
        if (!isIncrement)
        {
            CloseDialog();
            return;
        }
        switch (CurrentContent)
        {
            case ArtistResultsViewModel artistList:
                SetProcessing(true, "Getting Albums...");
                selectedResult = artistList.SelectedArtist.GetResult();
                Artist? selectedArtist = await ResultHelper.ArtistResultHelper((ArtistResult)selectedResult);
                if (selectedArtist == null)
                {
                    DisplayError(this, "Error: Couldn't process selected artist, please select another");
                    SetProcessing(false);
                    return;
                }
                DisableCheckers(selectedArtist);
                //Now we have a valid Artist, generate AlbumResult List and change the current content to that
                var albumResultsVm = await AlbumResultsViewModel.CreateViewModel(selectedArtist, selectedResult.Source);
                CurrentContent = albumResultsVm;
                break;
            case AlbumResultsViewModel albumList:
                SetProcessing(true, "Getting Videos...");
                selectedResult = albumList.SelectedAlbum.GetResult();
                Album? selectedAlbum = await ResultHelper.AlbumResultHelper((AlbumResult)selectedResult);
                if (selectedAlbum == null)
                {
                    DisplayError(this, "Error: Couldn't process selected album, please select another");
                    SetProcessing(false);
                    return;
                }
                DisableCheckers(selectedAlbum.Artist);
                var vidResultsVm = await VideoResultsViewModel.CreateViewModel(selectedResult.Source, selectedAlbum.Artist,selectedAlbum);
                if (vidResultsVm == null)
                {
                    DisplayError(this, "Error: Couldn't generate video results, please check logs");
                    SetProcessing(false);
                    return;
                }
                else if (selectedResult.Source !=
                         vidResultsVm.SearchResults[0].GetResult().Source)
                {
                    DisplayError(this, $"{selectedResult.Source.ToString()} had no videos, moved to {vidResultsVm.SearchResults[0].GetResult().Source.ToString()}");
                    DisableCheckers(vidResultsVm.SearchResults[0].GetResult().Source);
                }
                CurrentContent = vidResultsVm;
                break;
            case VideoResultsViewModel videoList:
                //Store the current list so we can return to it later
                var currentList = CurrentContent;
                //Setup Loading
                var waveVm = new WaveProgressViewModel();
                //Cycle Through Results
                selectedResult = videoList.SelectedVideos[0].GetResult();
                ShowStepper = NavVisible = false;
                CurrentContent = waveVm;
                switch (selectedResult.Source)
                {
                    case SearchSource.YouTubeMusic:
                        //YTDLSharp's download progress is broken right now so we just have to have an infinite spinning wheel
                        waveVm.IsIndeterminate = waveVm.CircleVisible = true;
                        waveVm.IsTextVisible = false;
                        await DownloadYTM(videoList.SelectedVideos, waveVm);
                        break;
                    case SearchSource.AppleMusic:
                        waveVm.WaveVisible = true;
                        await DownloadAM(videoList.SelectedVideos, waveVm);
                        break;
                }
                ShowStepper = NavVisible = true;
                CurrentContent = currentList;
                break;
            
            case ManualArtistViewModel manArtist:
                Artist? manualArtist = await ResultHelper.ArtistResultHelper(manArtist.ArtistNameText);
                if (manualArtist == null)
                {
                    DisplayError(this, "Error: Error creating manual artist, please check logs");
                    SetProcessing(false);
                    return;
                }
                DisableCheckers(SearchSource.Manual);
                CurrentContent = new ManualAlbumViewModel(manualArtist);
                break;
            case ManualAlbumViewModel manAlbum:
                AlbumResult albData = manAlbum.GetResult();
                Album? manualAlbum = await ResultHelper.AlbumResultHelper(albData);
                if (manualAlbum == null)
                {
                    DisplayError(this, "Error: Error creating manual album, please check logs");
                    SetProcessing(false);
                    return;
                }
                DisableCheckers(SearchSource.Manual);
                CurrentContent = new ManualMusicVideoViewModel(manualAlbum);
                break;
            case ManualMusicVideoViewModel vidAlbum:
                //Setup Loading
                var ytWave = new WaveProgressViewModel();
                var manualList = vidAlbum.ManualItems;
                //Cycle Through Results
                selectedResult = manualList[0].GetResult();
                ShowStepper = NavVisible = false;
                CurrentContent = ytWave;
                switch (selectedResult.Source)
                {
                    case SearchSource.YouTubeMusic:
                        //YTDLSharp's download progress is broken right now so we just have to have an infinite spinning wheel
                        ytWave.IsIndeterminate = ytWave.CircleVisible = true;
                        ytWave.IsTextVisible = false;
                        //await DownloadYTM(vidAlbum.ManualItems, ytWave);
                        break;
                    case SearchSource.Manual:
                        await ProcessManualVideo(manualList, ytWave);
                        break;
                }
                ShowStepper = NavVisible = true;
                
                break;
        }
        if (selectedResult != null)
        {
            Console.WriteLine(selectedResult.Name);
        }
        SetProcessing(false);
        //CloseDialog();
    }

    private void SetupManual()
    {
        switch (CurrentContent)
        {
            case ArtistResultsViewModel _:
                CurrentContent = new ManualArtistViewModel();
                break;
            case AlbumResultsViewModel albumList:
                CurrentContent = new ManualAlbumViewModel(albumList.CurrentArtist);
                break;
            case VideoResultsViewModel videoList:
                if (videoList.CurrentAlbum == null)
                {
                    CurrentContent = new ManualMusicVideoViewModel(videoList.CurrentArtist);
                }
                else
                {
                    CurrentContent = new ManualMusicVideoViewModel(videoList.CurrentAlbum);
                }
                break;
        }
    }

    private async void RefreshResultList()
    {
        switch (CurrentContent)
        {
            case ManualArtistViewModel _:
                CurrentContent = ArtistResultsSetup();
                break;
            case ArtistResultsViewModel artistList:
                artistList.SelectedSource = currentSource;
                if (artistList.SearchInput != "")
                {
                    artistList.SearchArtist();
                }
                break;
            case AlbumResultsViewModel albumList:
                SetProcessing(true, "Getting Albums...");
                await albumList.GenerateNewResults(currentSource);
                SetProcessing(false);
                break;
            case VideoResultsViewModel videoList:
                SetProcessing(true, "Getting Videos...");
                await videoList.RefreshList(currentSource);
                SetProcessing(false);
                break;
        }
    }
    
    #region Result Handlers
    private async Task<bool> DownloadYTM(ObservableCollection<VideoResultViewModel> videoList, WaveProgressViewModel waveVm)
    {
        //Check that the Artist Folder exists before proceeding
        if (!Directory.Exists($"{App.GetSettings().RootFolder}\\{videoList[0].Artist.Name}"))
            Directory.CreateDirectory($"{App.GetSettings().RootFolder}\\{videoList[0].Artist.Name}");
        for (int i = 0; i < videoList.Count(); i++)
        {
            var selectedVideo = videoList[i];
            waveVm.HeaderText =
                $"Downloading {selectedVideo.Title} {i + 1}/{videoList.Count}";
            if (!await DownloadYT(selectedVideo, waveVm))
            {
                return false;
            }
        }
        return true;
    }

    private async Task<bool> ProcessManualVideo(ObservableCollection<VideoResultViewModel> videoList,
        WaveProgressViewModel waveVm)
    {
        //Check that the Artist Folder exists before proceeding
        if (!Directory.Exists($"{App.GetSettings().RootFolder}\\{videoList[0].Artist.Name}"))
            Directory.CreateDirectory($"{App.GetSettings().RootFolder}\\{videoList[0].Artist.Name}");
        for (int i = 0; i < videoList.Count(); i++)
        {
            var selectedVideo = videoList[i];
            //You can manually add a YT video so we need to check for that
            if (selectedVideo.GetResult().Source == SearchSource.YouTubeMusic)
            {
                await DownloadYT(selectedVideo, waveVm);
            }

            await selectedVideo.GenerateNFO(selectedVideo.GetResult().VidPath);
        }

        return true;
    }
    #endregion
    
    #region Download Handler
    private async Task<bool> DownloadAM(ObservableCollection<VideoResultViewModel> videoList, WaveProgressViewModel waveVm)
    {
        for (int i = 0; i < videoList.Count(); i++)
        {
            var selectedVideo = videoList[i];
            //Check that the Artist Folder exists before proceeding
            if (!Directory.Exists($"{App.GetSettings().RootFolder}\\{selectedVideo.Artist.Name}"))
                Directory.CreateDirectory($"{App.GetSettings().RootFolder}\\{selectedVideo.Artist.Name}");
            waveVm.HeaderText =
                $"Downloading {selectedVideo.Title} {i + 1}/{videoList.Count}";
            var downResult = await App.GetAppleMusicDLHelper().DownloadVideo(selectedVideo, waveVm);
            switch (downResult)
            {
                case AppleMusicDownloadResponse.Success:
                    await selectedVideo.GenerateNFO($"{App.GetSettings().RootFolder}/{selectedVideo.Artist.Name}/{selectedVideo.Title}.mp4");
                    selectedVideo.HandleDownload();
                    break;
                case AppleMusicDownloadResponse.Failure:
                    ToastHelper.ShowError("Download Failed", $"Download for {selectedVideo.Title} failed, please check logs");
                    Log.Error($"Error in AddMusicVideo->HandleSave: {selectedVideo.Title} failed");
                    break;
                case AppleMusicDownloadResponse.InvalidUserToken:
                    ToastHelper.ShowError("Download Error", "Invalid user token provided! Please update token in config");
                    Log.Error("Error in AddMusicVideo->HandleSave: Invalid user token");
                    break;
                case AppleMusicDownloadResponse.InvalidDeviceFiles:
                    ToastHelper.ShowError("Download Error", "Invalid or missing device files! Please store these in assets folder");
                    Log.Error("Error in AddMusicVideo->HandleSave: Invalid device files");
                    break;
            }
        }

        return true;
    }
    private async Task<bool> DownloadYT(VideoResultViewModel currVid, WaveProgressViewModel waveVm)
    {
        var downResult = await App.GetYTDLHelper().DownloadVideo(currVid);
        if (!downResult.Success)
        {
            ToastHelper.ShowError("Download Error", $"Something went wrong with downloading {currVid.Title}.");
            Log.Error($"Error in AddMusicVideo->SaveMultipleVideos: {currVid.GetResult().SourceId} failed");
            foreach (var err in downResult.ErrorOutput) Log.Error(err);
            if (downResult.ErrorOutput.Contains("Signature extraction failed"))
            {
                ToastHelper.ShowError("YT-DLP Outdated", $"Yt-dlp version may be outdated, please confirm you are using latest version.", NotificationType.Warning);
            }

            return false;
        }
        await currVid.GenerateNFO(downResult.Data).ContinueWith(t =>
        {
            if (!t.IsCompletedSuccessfully)
            {
                
                ToastHelper.ShowError("NFO Error", $"Something went wrong with generating the NFO for {currVid.Title}");
                Log.Error($"Error in AddMusicVideo->SaveMultipleVideos->GenerateNFO: {currVid.GetResult().SourceId} failed");
                foreach (var err in downResult.ErrorOutput) Log.Error(err);
            }
            currVid.HandleDownload();
            waveVm.UpdateProgress(0);
        }, TaskScheduler.FromCurrentSynchronizationContext());
        return true;
    }
    #endregion

    //Depending on where we have originated, we can skip straight to a list of videos.
    //This should handle that based on the current EventHandler
    [RelayCommand]
    public void HandleSkip()
    {
        Console.WriteLine("Skip to videos");
    }

    [RelayCommand]
    public void CloseDialog()
    {
        App.GetVM().GetParentView().RefreshArtistList();
        App.GetVM().GetDialogManager().DismissDialog();
    }

    #endregion
}