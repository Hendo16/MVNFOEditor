using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Factories;
using MVNFOEditor.Models;

namespace MVNFOEditor.ViewModels;

public partial class AlbumResultsViewModel : ObservableObject
{
    [ObservableProperty] private string _busyText;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private ObservableCollection<AlbumResultViewModel> _fullResults;
    [ObservableProperty] private ObservableCollection<AlbumResultViewModel> _searchResults;
    public Artist CurrentArtist;
    private string _searchText;
    [ObservableProperty] private AlbumResultViewModel _selectedAlbum;
    public string ArtistBrowseId { get; set; }
    public event EventHandler<bool> ShowNav;

    internal AlbumResultsViewModel() {}

    public async static Task<AlbumResultsViewModel> CreateViewModel(Artist artist, SearchSource source = SearchSource.YouTubeMusic, string searchText = "")
    {
        AlbumResultsViewModel newVm = new AlbumResultsViewModel();
        newVm.CurrentArtist = artist;
        await newVm.GenerateNewResults(source);
        newVm.SearchText = searchText;
        return newVm;
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            FilterResults();
        }
    }

    private void FilterResults()
    {
        if (string.IsNullOrEmpty(SearchText))
        {
            SearchResults = FullResults;
            return;
        }
        SearchResults = new ObservableCollection<AlbumResultViewModel>(
            FullResults.Where(item => item.Title?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
        );
    }

    public async Task<bool> GenerateNewResults(SearchSource source)
    {
        //IsBusy = true;
        ShowNav?.Invoke(this, false);
        SearchResults?.Clear();
        FullResults?.Clear();
        switch (source)
        {
            case SearchSource.YouTubeMusic:
                var ytResults = await App.GetYouTubeFactory().GetAlbums(CurrentArtist);
                if (ytResults.Count == 0) {return false;}
                SearchResults = FullResults = ytResults;
                break;
            case SearchSource.AppleMusic:
                var amResults = await App.GetAppleFactory().GetAlbums(CurrentArtist);
                if (amResults.Count == 0) {return false;}
                SearchResults = FullResults = amResults;
                break;
        }

        //IsBusy = false;
        ShowNav?.Invoke(this, true);
        return true;
    }
}