using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;

namespace MVNFOEditor.ViewModels;

public abstract class SelectedItem
{
    public VideoResultViewModel viewModel { get; set; }
    public bool IsSelected { get; set; }
}

public partial class VideoResultsViewModel : ObservableObject
{
    private ObservableCollection<VideoResultViewModel> _fullResults;
    private ObservableCollection<VideoResultViewModel> _results;
    
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private ObservableCollection<VideoResultViewModel> _selectedVideos;
    
    
    public Artist CurrentArtist;
    public Album? CurrentAlbum;


    internal VideoResultsViewModel(Artist currArtist, Album? currAlbum = null)
    {
        CurrentArtist = currArtist;
        CurrentAlbum = currAlbum;
        SelectedVideos = new ObservableCollection<VideoResultViewModel>();
    }

    public static async Task<VideoResultsViewModel?> CreateViewModel(SearchSource source, Artist artist, Album? album = null)
    {
        List<SearchSource> checkedSources = new List<SearchSource>() { source };
        while (true)
        {
            //First, we need to get a full list of videos from the artist from the source
            var videos = await artist.GetVideos(source);
            if (videos == null)
            {
                ToastHelper.ShowError("Error getting videos", "Couldn't find videos for artist, please check logs");
                return null;
            }

            var newVm = new VideoResultsViewModel(artist, album);
            //Now we map the results to view models, as well as filter out videos not within the album (if it exists)
            var results = await newVm.ResultsToVm(videos, source);
            if (results == null)
            {
                ToastHelper.ShowError("Error generating results", "Couldn't generate results for the artist/album, please check logs");
                return null;
            }

            if (results.Count == 0)
            {
                //Check if we can go to another source
                SearchSource? nextSource = artist.GetNextSource(checkedSources);
                if (nextSource == null)
                {
                    ToastHelper.ShowError("No Videos", "No Videos Available for the Artist/Album");
                    return null;
                }
                ToastHelper.ShowError($"No Videos on {source.ToString()}", $"{source.ToString()} has no videos, checking other sources...", NotificationType.Information, 3);
                source = (SearchSource)nextSource;
                checkedSources.Add(source);
                continue;
            }

            newVm._results = newVm._fullResults = results;
            return newVm;
        }
    }

    public async Task<bool> RefreshList(SearchSource source)
    {
        var videos = await CurrentArtist.GetVideos(source);
        if (videos == null)
        {
            ToastHelper.ShowError("Error getting videos", "Couldn't find videos for artist, please check logs");
            return false;
        }
        var results = await ResultsToVm(videos, source);
        if (results == null)
        {
            ToastHelper.ShowError("Error generating results", "Couldn't generate results for the artist/album, please check logs");
            return false;
        } 
        if (results.Count == 0)
        {
            ToastHelper.ShowError("No Videos", "No Videos Available for the Artist/Album");
            return false;
        }
        
        _results = _fullResults = results;
        return true;
    }

    private async Task<ObservableCollection<VideoResultViewModel>?> ResultsToVm(List<VideoResult> results, SearchSource source)
    {
        switch (source)
        {
            case SearchSource.YouTubeMusic:
                if (CurrentAlbum?.AlbumBrowseID == null)
                {
                    return await App.GetYTMusicHelper().GenerateVideoResultList(results, CurrentArtist);
                }
                //If an album is provided, we need to filter through
                var fullAlbum = await App.GetYTMusicHelper().GetAlbum(CurrentAlbum.AlbumBrowseID);
                //If we can't get the full album details, just returned the artist's full list anyway
                if (fullAlbum == null)
                {
                    return await App.GetYTMusicHelper().GenerateVideoResultList(results, CurrentArtist);
                }
                return await App.GetYTMusicHelper().GenerateVideoResultList(results, fullAlbum, CurrentAlbum);
                break;
            case SearchSource.AppleMusic:
                //Get videos just for the given album
                if (CurrentAlbum != null) return await App.GetiTunesHelper().GenerateVideoResultList(CurrentAlbum);
                //Get all videos for Artist
                var artistMetadata = CurrentArtist.GetArtistMetadata(SearchSource.AppleMusic);
                var fullVideos = await App.GetiTunesHelper().GetVideosByArtistId(artistMetadata.BrowseId);
                return await App.GetiTunesHelper().GenerateVideoResultList(fullVideos, CurrentArtist);
                break;
            default:
                return null;
        }
    }

    public ObservableCollection<VideoResultViewModel> SearchResults
    {
        get => _results;
        set
        {
            _results = value;
            OnPropertyChanged();
        }
    }

    public void FilterResults()
    {
        if (string.IsNullOrEmpty(SearchText))
        {
            SearchResults = _fullResults;
            return;
        }

        SearchResults = new ObservableCollection<VideoResultViewModel>(
            _fullResults.Where(item => item.Title?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0));
    }

    [RelayCommand]
    public void CloseDialog()
    {
        App.GetVM().GetDialogManager().DismissDialog();
    }
}