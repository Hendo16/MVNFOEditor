using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.EntityFrameworkCore;
using MVNFOEditor.DB;
using MVNFOEditor.Models;
using MVNFOEditor.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Helpers;
public class iTunesAPIHelper
{
    private MusicDbContext _dbContext;
    private static string baseURL = "https://itunes.apple.com/";
    private static string localCountry = "au";
    private static int resultLimit = 200;
    private readonly IFlurlClient _flurlClient;

    private iTunesAPIHelper()
    {
        _flurlClient = new FlurlClient();
        _dbContext = App.GetDBContext();
    }

    public static iTunesAPIHelper CreateHelper()
    {
        iTunesAPIHelper newHelper = new iTunesAPIHelper();
        return newHelper;
    }

    public async Task<JObject> ArtistSearch(string artistName)
    {
        string result = await baseURL
            .AppendPathSegment("search")
            .SetQueryParams(new
            {
                term = artistName,
                country = localCountry,
                entity = "musicArtist",
                attribute = "artistTerm",
                limit = resultLimit
            })
            .GetStringAsync();
        return JObject.Parse(result);
    }

    public async Task<JArray> GetAlbumsByArtistID(string artistId)
    {
        string result = await baseURL
            .AppendPathSegment("lookup")
            .SetQueryParams(new
            {
                id = artistId,
                country = localCountry,
                entity = "album",
                limit = resultLimit
            })
            .GetStringAsync();
        JObject albumResults = JsonConvert.DeserializeObject<JObject> ( result, new JsonSerializerSettings() { DateParseHandling = DateParseHandling.None });
        if (albumResults.Value<int>("resultCount") == 0) {return null;}
        JArray arrResults = albumResults.Value<JArray>("results");
        JToken artist_duplicate = arrResults.First();
        arrResults.Remove(artist_duplicate);
        return arrResults;
    }

    public async Task<List<AlbumResult>?> GetAlbums(string artistId, Artist artist)
    {
        JArray albums = await GetAlbumsByArtistID(artistId);
        return albums.Select(x =>  albObjToResult(x, artist)).ToList();
    }

    public async Task<JArray> GetTrackListByAlbumID(string albumId)
    {
        string result = await baseURL
            .AppendPathSegment("lookup")
            .SetQueryParams(new
            {
                id = albumId,
                country = localCountry,
                entity = "song",
                limit = resultLimit
            })
            .GetStringAsync();
        JObject albumResults = JsonConvert.DeserializeObject<JObject> ( result, new JsonSerializerSettings() { DateParseHandling = DateParseHandling.None });
        if (albumResults.Value<int>("resultCount") == 0) {return null;}
        JArray arrResults = albumResults.Value<JArray>("results");
        JToken album_duplicate = arrResults.First();
        arrResults.Remove(album_duplicate);
        return arrResults;
    }

    public async Task<JArray> GetVideosByArtistName(string artistName)
    {
        string result = await baseURL
            .AppendPathSegment("search")
            .SetQueryParams(new
            {
                term = artistName,
                country = localCountry,
                entity = "musicVideo",
                attribute = "artistTerm",
                limit = resultLimit
            })
            .GetStringAsync();
        JObject videoResults = JsonConvert.DeserializeObject<JObject> ( result, new JsonSerializerSettings() { DateParseHandling = DateParseHandling.None });
        return videoResults.Value<JArray>("results");
    }

    public async Task<JArray> GetVideosByArtistId(string artistId)
    {
        string result = await baseURL
            .AppendPathSegment("lookup")
            .SetQueryParams(new
            {
                id = artistId,
                entity = "musicVideo",
                country = localCountry,
                limit = resultLimit
            })
            .GetStringAsync();
        JObject videoResults = JsonConvert.DeserializeObject<JObject> ( result, new JsonSerializerSettings() { DateParseHandling = DateParseHandling.None });
        if (videoResults.Value<int>("resultCount") == 0) {return null;}
        JArray arrResults = videoResults.Value<JArray>("results");
        JToken artist_duplicate = arrResults.First();
        arrResults.Remove(artist_duplicate);
        //Remove 'feature-movie' objects
        JArray filteredResults = JArray.FromObject(arrResults.Where(v => (string)v["kind"] != "feature-movie"));
        return filteredResults;
    }

    public async Task<List<VideoResult>?> GetVideosFromArtistId(string browseId, Artist artist)
    {
        JArray videos = await GetVideosByArtistId(browseId);
        return videos.Select(x =>  vidObjToResult(x, artist)).ToList();
    }
    
    private static AlbumResult albObjToResult(JToken video, Artist artist)
    {
        AlbumResult newResult = new AlbumResult(video, artist);
        newResult.thumbURL = GetHighQualityArt(video);
        return newResult;
    }
    
    private static VideoResult vidObjToResult(JToken video, Artist artist)
    {
        VideoResult newResult = new VideoResult(video, artist);
        newResult.thumbURL = GetHighQualityArt(video);
        return newResult;
    }
    
    public async Task<ObservableCollection<AlbumResultViewModel>> GenerateAlbumResultList(Artist artist)
    {            
        ArtistMetadata artistMetadata = artist.GetArtistMetadata(SearchSource.AppleMusic);
        //Process Album Results
        //JArray AlbumList = artistMetadata.AlbumResults;
        JArray AlbumList = new JArray();
        ObservableCollection<AlbumResultViewModel> results = new ObservableCollection<AlbumResultViewModel>();
        
        //Build up the album cards
        for (int i = 0; i < AlbumList.Count; i++)
        {
            var currAlbum = AlbumList[i];
            AlbumResult currResult = new AlbumResult();

            currResult.Artist = artist;
            currResult.Title = currAlbum["collectionName"].ToString();
            currResult.browseId = currAlbum["collectionId"].ToString();
            currResult.isExplicit = currAlbum["collectionExplicitness"].ToString() == "Explicit";
            currResult.thumbURL = GetHighQualityArt((JObject)currAlbum);
            currResult.Year = DateTime.Parse(currAlbum["releaseDate"].ToString()).Year.ToString();
            AlbumResultViewModel newVM = new AlbumResultViewModel(currResult);
            await newVM.LoadThumbnail();
            results.Add(newVM);
        }

        return results;
    }

    public async Task<ObservableCollection<VideoResultViewModel>> GenerateVideoResultList(JArray vidResults,
        Artist artist)
    {
        ObservableCollection<VideoResultViewModel> results = new ObservableCollection<VideoResultViewModel>();
        var songs = _dbContext.MusicVideos.Include(musicVideo => musicVideo.artist)
            .ThenInclude(art => art.Metadata).ToList();
        for (int i = 0; i < vidResults.Count; i++)
        {
            JToken video = vidResults[i];
            //Create the VideoResult object
            VideoResult newVideo = new VideoResult();
            newVideo.Artist = artist;
            //Use trackCensoredName instead of trackName, because only trackCensoredName seems to contain the ambiguations [(Live), (Directors Cut), etc.]
            newVideo.Title = (string)video["trackCensoredName"];
            newVideo.VideoID = (string)video["trackId"];
            newVideo.VideoURL = ((string)video["trackViewUrl"]).Split('?')[0];
            newVideo.Duration = TimeSpan.FromMilliseconds((double)video["trackTimeMillis"])
                .ToString(@"mm\:ss");
            newVideo.Explicit = (string)video["trackExplicitness"] == "explicit";
            newVideo.Year = DateTime.Parse((string)video["releaseDate"]).Year.ToString();
            newVideo.thumbURL = GetHighQualityArt((JObject)video);
                    
            //Create the VM for the result
            VideoResultViewModel resultVM = new VideoResultViewModel(newVideo);
                    
            //Check if the song already exists in the DataBase
            if (songs != null && songs.Exists(s => s.artist.Metadata.Any(m => m.SourceId == SearchSource.AppleMusic) && s.videoID == newVideo.VideoID))
            {
                resultVM.BorderColor = "Green";
                resultVM.DownloadEnabled = false;
                resultVM.DownloadBtnText = "Downloaded";
            }
            else
            {
                resultVM.BorderColor = "Black";
                resultVM.DownloadEnabled = true;
                resultVM.DownloadBtnText = "Download";
            }
            //Download and cache the thumbnail
            await resultVM.LoadThumbnail();
            results.Add(resultVM);
        }
        return results;
    }

    public async Task<ObservableCollection<VideoResultViewModel>> GenerateVideoResultList(Album album, List<MusicVideo>? songs)
    {
            ObservableCollection<VideoResultViewModel> results = new ObservableCollection<VideoResultViewModel>();
            ArtistMetadata artistMetadata = album.Artist.GetArtistMetadata(SearchSource.AppleMusic);
            //Get a list of music videos from the artist available
            JArray currentVideos = await GetVideosByArtistId(artistMetadata.BrowseId);
            //Get a list of songs from the album
            JArray albumTracks = await GetTrackListByAlbumID(album.AlbumBrowseID);
            //Match videos with the song tracks
            for (int i = 0; i < albumTracks.Count; i++)
            {
                JToken track = albumTracks[i];
                JToken? matchedVideo = currentVideos.FirstOrDefault(v => ((string)v["trackName"]).ToLower() == ((string)track["trackName"]).ToLower());
                if (matchedVideo != null)
                {
                    //Create the VideoResult object
                    VideoResult newVideo = new VideoResult();
                    newVideo.Artist = album.Artist;
                    //Use trackCensoredName instead of trackName, because only trackCensoredName seems to contain the ambiguations [(Live), (Directors Cut), etc.]
                    newVideo.Title = (string)matchedVideo["trackCensoredName"];
                    newVideo.VideoID = (string)matchedVideo["trackId"];
                    newVideo.VideoURL = ((string)matchedVideo["trackViewUrl"]).Split('?')[0];
                    newVideo.Duration = TimeSpan.FromMilliseconds((double)matchedVideo["trackTimeMillis"])
                        .ToString(@"mm\:ss");
                    newVideo.Explicit = (string)matchedVideo["trackExplicitness"] == "explicit";
                    newVideo.Year = DateTime.Parse((string)matchedVideo["releaseDate"]).Year.ToString();
                    newVideo.thumbURL = GetHighQualityArt((JObject)matchedVideo);
                    
                    //Create the VM for the result
                    VideoResultViewModel resultVM = new VideoResultViewModel(newVideo, album);
                    
                    //Check if the song already exists in the DataBase
                    if (songs != null && songs.Exists(s => s.artist.Metadata.Any(m => m.SourceId == SearchSource.AppleMusic) && s.videoID == newVideo.VideoID))
                    {
                        resultVM.BorderColor = "Green";
                        resultVM.DownloadEnabled = false;
                        resultVM.DownloadBtnText = "Downloaded";
                    }
                    else
                    {
                        resultVM.BorderColor = "Black";
                        resultVM.DownloadEnabled = true;
                        resultVM.DownloadBtnText = "Download";
                    }
                    //Download and cache the thumbnail
                    await resultVM.LoadThumbnail();
                    results.Add(resultVM);
                }
            }
            return results;
    }
    
    public static string GetHighQualityArt(JToken albumObj)
    {
        string artURL = albumObj["artworkUrl100"].ToString();
        return artURL.Replace("100x100", "400x400");
    }

    public string[] GetArtistBannerLinks(string url)
    {
        using (var wc = new WebClient()) // "using" keyword automatically closes WebClient stream on download completed
        {
            string[] urls = new string[2];
            string source = wc.DownloadString(url);
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(source);
            
            var element = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'artist-header')]");
            var sources = doc.DocumentNode.SelectNodes("//source");
            string srcset = sources[1].GetAttributeValue("srcset", string.Empty);
            if (!string.IsNullOrEmpty(srcset))
            {
                urls[0] = srcset.Split(',').Select(url => url.Split(' ')[0]).ElementAt(1);
                urls[1] = srcset.Split(',').Select(url => url.Split(' ')[0]).ElementAt(0);
            }
            if (element != null)
            {
                string style = element.GetAttributeValue("style", "");
                if (!string.IsNullOrEmpty(style))
                {
                    var matches = Regex.Matches(style, @"--background-image(?:-xs)?:\s*url\((.*?)\);");
                    urls[0] = matches[0].Groups[1].Value.Trim('\'', '"');
                    urls[1] = matches[1].Groups[1].Value.Trim('\'', '"');
                }
            }
            return urls;
        }
    }

    public string GetArtistThumb(string url)
    {
        //Some artist urls will be dead itunes links - skip these
        if (url.Contains("itunes.apple.com")) {return "";}
        using (var wc = new WebClient()) // "using" keyword automatically closes WebClient stream on download completed
        {
            string source = wc.DownloadString(url);
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(source);
            
            var metaTag = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");
            string result = metaTag?.GetAttributeValue("content", null);
            return result != "https://music.apple.com/assets/meta/apple-music.png" ? result.Replace(Path.GetFileName(result),"100x100.jpg") : result;
        }
    }
}