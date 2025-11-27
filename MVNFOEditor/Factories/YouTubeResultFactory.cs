using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using log4net;
using MVNFOEditor.Exceptions;
using MVNFOEditor.Models;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Factories;

public class YouTubeResultFactory : IResultFactory
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(YouTubeResultFactory));
    public async Task<ObservableCollection<ArtistResultViewModel>> SearchArtists(string artistStr)
    {
        ObservableCollection<ArtistResultViewModel> artistViewModels = new();
        List<YtMusicNet.Records.ArtistResult?>? results = await App.GetYTMusicHelper().searchArtists(artistStr);
        if (results == null)
        {
            throw new SearchExceptions.ResultsEmptyException($"Empty search results for {artistStr} on YouTube Music");
        }
        for (var i = 0; i < results.Count; i++)
        {
            if (results[i] == null) {continue;}
            var arrResult = new ArtistResult(results[i]);
            var newVm = await ArtistResultViewModel.CreateViewModel(arrResult);
            artistViewModels.Add(newVm);
        }
        return artistViewModels;
    }

    public async Task<ObservableCollection<AlbumResultViewModel>> GetAlbums(Artist artist)
    {
        ObservableCollection<AlbumResultViewModel> albumViewModels = new();
        List<AlbumResult>? albumList = await artist.GetAlbums(SearchSource.YouTubeMusic);
        if (albumList is null)
        {
            throw new SearchExceptions.ResultsEmptyException($"No albums found for {artist.Name} on YouTube Music");
        }
        foreach (var album in albumList)
        {
            var albResult = await AlbumResultViewModel.CreateAsync(album);
            albumViewModels.Add(albResult);
        }
        return albumViewModels;
    }
}