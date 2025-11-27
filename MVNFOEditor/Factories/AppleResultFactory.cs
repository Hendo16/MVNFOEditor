using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using log4net;
using MVNFOEditor.Exceptions;
using MVNFOEditor.Models;
using MVNFOEditor.ViewModels;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Factories;

public class AppleResultFactory : IResultFactory
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(AppleResultFactory));
    public async Task<ObservableCollection<ArtistResultViewModel>> SearchArtists(string artistStr)
    {
        ObservableCollection<ArtistResultViewModel> artistViewModels = new();
        JObject apiResponse = await App.GetiTunesHelper().ArtistSearch(artistStr);
        JArray? results = apiResponse.Value<JArray>("results");
        if (results == null)
        {
            throw new SearchExceptions.ResultsEmptyException($"Empty search results for {artistStr} on Apple Music");
        }
        //TODO: Handle search results greater than the first 10 artists
        var limit = results.Count < 10 ? results.Count : 10;
        for (var i = 0; i < limit; i++)
        {
            var result = results[i].ToObject<JObject>();
            if (result == null) {continue;}
            //Some results return outdated iTunes based links - so we need to fix these
            var artistUrl = result.Value<string>("artistLinkUrl")?.Replace("itunes.apple.com", "music.apple.com");
            var artUrl = artistUrl != null ? App.GetiTunesHelper().GetArtistBannerLinks(artistUrl)[0] : "";
            var arrResult = new ArtistResult(result, artUrl);
            var newVm = await ArtistResultViewModel.CreateViewModel(arrResult);
            artistViewModels.Add(newVm);
        }
        return artistViewModels;
    }

    public async Task<ObservableCollection<AlbumResultViewModel>> GetAlbums(Artist artist)
    {
        ObservableCollection<AlbumResultViewModel> albumViewModels = new();
        List<AlbumResult>? albumList = await artist.GetAlbums(SearchSource.AppleMusic);
        if (albumList is null)
        {
            throw new SearchExceptions.ResultsEmptyException($"No albums found for {artist.Name} on Apple Music");
        }
        foreach (var album in albumList)
        {
            var albResult = await AlbumResultViewModel.CreateAsync(album);
            albumViewModels.Add(albResult);
        }
        return albumViewModels;
    }
}