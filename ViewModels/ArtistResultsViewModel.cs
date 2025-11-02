using System;
using System.Collections.ObjectModel;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using Flurl.Http;
using log4net;
using MVNFOEditor.Exceptions;
using MVNFOEditor.Factories;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using SukiUI.Toasts;

namespace MVNFOEditor.ViewModels;

public partial class ArtistResultsViewModel : ObservableObject
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ArtistResultsViewModel));
    [ObservableProperty] private string _busyText;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _searching;
    [ObservableProperty] private string _searchInput;
    [ObservableProperty] private ObservableCollection<ArtistResultViewModel> _searchResults;
    [ObservableProperty] private ArtistResultViewModel _selectedArtist;
    public SearchSource SelectedSource;

    public ArtistResultsViewModel(SearchSource source = SearchSource.YouTubeMusic, string searchInput = "")
    {
        SearchResults = new ObservableCollection<ArtistResultViewModel>();
        Searching = false;
        SelectedSource = source;
        SearchInput = searchInput;
    }
    public event EventHandler<bool> ValidSearch;
    public event EventHandler<bool> HideError;
    public event EventHandler<string> DisplayError;

    public void SearchArtist()
    {
        HideError?.Invoke(this, false);
        if (SearchInput == "")
        {
            DisplayError?.Invoke(this, "Please enter search text");
            return;
        }
        Searching = true;
        //ValidSearch(null, true);
        SearchResults.Clear();
        switch (SelectedSource)
        {
            case SearchSource.YouTubeMusic:
                YouTubeSearch();
                return;
            case SearchSource.AppleMusic:
                iTunesSearch();
                return;
        }
    }

    private async void YouTubeSearch()
    {
        try
        {
            Searching = true;
            YouTubeResultFactory newFactory =  new YouTubeResultFactory();
            SearchResults = await newFactory.SearchArtists(SearchInput);
            Searching = false;
        }
        catch (SearchExceptions.ResultsEmptyException ex)
        {
            ToastHelper.ShowError("Artist search", $"No results found for {SearchInput}");
            Log.Error(ex.Message);
            Searching = false;
        }
        catch (FlurlHttpException ex)
        {
            ToastHelper.ShowError("Artist search", "Couldn't connect to server. Check logs.");
            Log.Error(ex.Message);
            Searching = false;
        }
    }

    private async void iTunesSearch()
    {
        try
        {
            Searching = true;
            AppleResultFactory newFactory =  new AppleResultFactory();
            SearchResults = await newFactory.SearchArtists(SearchInput);
            Searching = false;
        }
        catch (SearchExceptions.ResultsEmptyException ex)
        {
            ToastHelper.ShowError("Artist search", $"No results found for {SearchInput}");
            Log.Error(ex.Message);
            Searching = false;
        }
        catch (FlurlHttpException ex)
        {
            ToastHelper.ShowError("Artist search", "Couldn't connect to server. Check logs.");
            Log.Error(ex.Message);
            Searching = false;
        }
    }
}