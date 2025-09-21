using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using MVNFOEditor.Models;
using MVNFOEditor.Views;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels;

public partial class AddAlbumSourceViewModel : ObservableObject
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(AddAlbumSourceViewModel));
    private Album currentAlbum;
    private SearchSource selectedSource;
    [ObservableProperty] private AlbumResultsViewModel _resultsVM;
    [ObservableProperty] private bool _ytEnabled = true;
    [ObservableProperty] private bool _amEnabled = true;
    [ObservableProperty] private bool _ytChecked;
    [ObservableProperty] private bool _amChecked;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _showError;
    [ObservableProperty] private string _errorText;
    
    public AddAlbumSourceViewModel(Album existingAlbum)
    {
        currentAlbum = existingAlbum;
        SearchSource? newSource = SetupSources();
        if (newSource == null)
        {
            App.GetToastManager().CreateToast()
                .OfType(NotificationType.Error)
                .WithContent($"Error: Artist has no new source options")
                .Dismiss().ByClicking()
                .Queue();
            CloseDialog();
            return;
        }
        ResultsVM = new AlbumResultsViewModel((SearchSource)newSource, existingAlbum.Title);
    }
    
    public async Task<bool> GenerateNewResults(SearchSource source)
    {
        List<AlbumResult>? albumResults = await currentAlbum.Artist.GetAlbums(source);
        if (albumResults == null || albumResults.Count == 0)
        {
            return false;
        }
        ResultsVM.GenerateNewResults(albumResults);
        return true;
    }

    public SearchSource? SetupSources()
    {
        YtEnabled = currentAlbum.Metadata.All(meta => meta.SourceId != SearchSource.YouTubeMusic);
        AmEnabled = currentAlbum.Metadata.All(meta => meta.SourceId != SearchSource.AppleMusic);
        if (!YtEnabled && !AmEnabled)
        {
            return null;
        }
        //Handle UI so that the right radio button looks 'checked'
        selectedSource = YtEnabled ? SearchSource.YouTubeMusic : SearchSource.AppleMusic;
        switch (selectedSource)
        {
            case SearchSource.YouTubeMusic:
                YtChecked = true;
                break;
            case SearchSource.AppleMusic:
                AmChecked = true;
                break;
        }
        return selectedSource;
    }
    
    public async void YouTubeChecked()
    {
        if (! await GenerateNewResults(SearchSource.YouTubeMusic))
        {
            App.GetVM().GetToastManager().CreateToast()
                .WithTitle("Error")
                .WithContent("No Albums Found on YouTube Music")
                .OfType(NotificationType.Error)
                .Dismiss()
                .After(TimeSpan.FromSeconds(5))
                .Queue();
        }
        //ResultsVM.selectedSource = SearchSource.YouTubeMusic;
        //ResultsVM.SearchResults.Clear();
    }
    public async void AppleMusicChecked()
    {
        if (! await GenerateNewResults(SearchSource.AppleMusic))
        {
            //ResultsVM.selectedSource = SearchSource.AppleMusic;
            //ResultsVM.SearchResults.Clear();
            App.GetVM().GetToastManager().CreateToast()
                .WithTitle("Error")
                .WithContent("No Albums Found on Apple Music")
                .OfType(NotificationType.Error)
                .Dismiss()
                .After(TimeSpan.FromSeconds(5))
                .Queue();
        }
    }
    public async void SaveSource()
    {
        var resultCard = ResultsVM.SelectedAlbum.GetResult();
        AlbumMetadata? newMetadata = await AlbumMetadata.GetNewMetadata(resultCard, currentAlbum);
        App.GetDBContext().AlbumMetadata.Add(newMetadata);
        await App.GetDBContext().SaveChangesAsync();
        CloseDialog();
    }
    
    public void CloseDialog()
    {
        App.GetVM().GetDialogManager().DismissDialog();
    }
}