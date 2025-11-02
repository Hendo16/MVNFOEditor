using System;
using System.Linq;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels;

public partial class AddArtistSourceViewModel : ObservableObject
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(AddArtistSourceViewModel));
    private readonly Artist currentArtist;
    [ObservableProperty] private bool _amChecked;
    [ObservableProperty] private bool _amEnabled = true;
    [ObservableProperty] private string _errorText;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private ArtistResultsViewModel _resultsVM;
    [ObservableProperty] private bool _showError;
    [ObservableProperty] private bool _ytChecked;
    [ObservableProperty] private bool _ytEnabled = true;
    private SearchSource selectedSource;

    public AddArtistSourceViewModel(Artist existingArtist)
    {
        currentArtist = existingArtist;
        var newSource = SetupSources();
        if (newSource == null)
        {
            ToastHelper.ShowError("New Artist Source", "Artist has no new source options");
            CloseDialog();
            return;
        }

        ResultsVM = new ArtistResultsViewModel((SearchSource)newSource, existingArtist.Name);
    }

    public event EventHandler<bool> RefreshArtistCard;

    public SearchSource? SetupSources()
    {
        YtEnabled = currentArtist.Metadata.All(meta => meta.SourceId != SearchSource.YouTubeMusic);
        AmEnabled = currentArtist.Metadata.All(meta => meta.SourceId != SearchSource.AppleMusic);
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

    public void YouTubeChecked()
    {
        ResultsVM.SelectedSource = SearchSource.YouTubeMusic;
        ResultsVM.SearchResults.Clear();
    }

    public void AppleMusicChecked()
    {
        ResultsVM.SelectedSource = SearchSource.AppleMusic;
        ResultsVM.SearchResults.Clear();
    }

    public async void SaveSource()
    {
        var resultCard = ResultsVM.SelectedArtist.GetResult();
        var newMetadata = await ArtistMetadata.GetNewMetadata(resultCard, currentArtist);
        App.GetDBContext().ArtistMetadata.Add(newMetadata);
        await App.GetDBContext().SaveChangesAsync();
        RefreshArtistCard?.Invoke(this, true);
        CloseDialog();
    }

    public void CloseDialog()
    {
        App.GetVM().GetDialogManager().DismissDialog();
    }
}