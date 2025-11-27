using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels;

public partial class AddAlbumSourceViewModel : ObservableObject
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(AddAlbumSourceViewModel));
    private readonly Album currentAlbum;
    [ObservableProperty] private bool _amChecked;
    [ObservableProperty] private bool _amEnabled = true;
    [ObservableProperty] private string _errorText;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private AlbumResultsViewModel _resultsVM;
    [ObservableProperty] private bool _showError;
    [ObservableProperty] private bool _ytChecked;
    [ObservableProperty] private bool _ytEnabled = true;
    private SearchSource selectedSource;

    internal AddAlbumSourceViewModel(Album existingAlbum)
    {
        currentAlbum = existingAlbum;
        var newSource = SetupSources();
        if (newSource == null)
        {
            ToastHelper.ShowError("New Album Source", "Album has no new source options");
            CloseDialog();
            return;
        }
    }

    public async static Task<AddAlbumSourceViewModel> CreateNewViewModel(Album existingAlbum)
    {
        AddAlbumSourceViewModel newVm = new AddAlbumSourceViewModel(existingAlbum);
        newVm.ResultsVM = await AlbumResultsViewModel.CreateViewModel(existingAlbum.Artist, newVm.selectedSource, existingAlbum.Title);
        return newVm;
    }

    private async Task<bool> GenerateNewResults(SearchSource source)
    {
        return await ResultsVM.GenerateNewResults(source);
    }

    private SearchSource? SetupSources()
    {
        YtEnabled = currentAlbum.Metadata.All(meta => meta.SourceId != SearchSource.YouTubeMusic);
        AmEnabled = currentAlbum.Metadata.All(meta => meta.SourceId != SearchSource.AppleMusic);
        if (!YtEnabled && !AmEnabled) return null;
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
        if (!await GenerateNewResults(SearchSource.YouTubeMusic))
            ToastHelper.ShowError("New Album Source", "No Albums found on YouTube Music");
        //ResultsVM.selectedSource = SearchSource.YouTubeMusic;
        //ResultsVM.SearchResults.Clear();
    }

    public async void AppleMusicChecked()
    {
        if (!await GenerateNewResults(SearchSource.AppleMusic))
            //ResultsVM.selectedSource = SearchSource.AppleMusic;
            //ResultsVM.SearchResults.Clear();
            ToastHelper.ShowError("New Album Source", "No Albums found on Apple Music");
    }

    public async void SaveSource()
    {
        try
        {
            var resultCard = ResultsVM.SelectedAlbum.GetResult();
            var newMetadata = await AlbumMetadata.GetNewMetadata(resultCard, currentAlbum);
            if (newMetadata == null)
            {
                Log.Error("No Album Metadata found");
                return;
            }
            App.GetDBContext().AlbumMetadata.Add(newMetadata);
            await App.GetDBContext().SaveChangesAsync();
            CloseDialog();
        }
        catch (Exception e)
        {
            Log.ErrorFormat("Error adding {0} source for {1}", selectedSource.ToString() , currentAlbum.Title);
            Log.Error(e);
        }
    }

    public void CloseDialog()
    {
        App.GetVM().GetDialogManager().DismissDialog();
    }
}