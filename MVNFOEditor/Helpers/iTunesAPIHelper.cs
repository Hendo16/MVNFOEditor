using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using MVNFOEditor.DB;
using MVNFOEditor.Models;
using MVNFOEditor.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Helpers;

public class iTunesAPIHelper
{
    private static readonly string baseURL = "https://itunes.apple.com/";
    private static readonly string localCountry = "au";
    private static readonly int resultLimit = 200;
    private readonly MusicDbContext _dbContext;
    private readonly IFlurlClient _flurlClient;

    private iTunesAPIHelper()
    {
        _flurlClient = new FlurlClient();
        _dbContext = App.GetDBContext();
    }

    public static iTunesAPIHelper CreateHelper()
    {
        var newHelper = new iTunesAPIHelper();
        return newHelper;
    }

    public async Task<JObject> ArtistSearch(string artistName)
    {
        var result = await baseURL
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
        var result = await baseURL
            .AppendPathSegment("lookup")
            .SetQueryParams(new
            {
                id = artistId,
                country = localCountry,
                entity = "album",
                limit = resultLimit
            })
            .GetStringAsync();
        var albumResults = JsonConvert.DeserializeObject<JObject>(result,
            new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
        if (albumResults.Value<int>("resultCount") == 0) return null;
        var arrResults = albumResults.Value<JArray>("results");
        var artist_duplicate = arrResults.First();
        arrResults.Remove(artist_duplicate);
        return arrResults;
    }

    public async Task<List<AlbumResult>?> GetAlbums(string artistId, Artist artist)
    {
        var albums = await GetAlbumsByArtistID(artistId);
        return albums.Select(x => albObjToResult(x, artist)).ToList();
    }

    public async Task<JArray> GetTrackListByAlbumID(string albumId)
    {
        var result = await baseURL
            .AppendPathSegment("lookup")
            .SetQueryParams(new
            {
                id = albumId,
                country = localCountry,
                entity = "song",
                limit = resultLimit
            })
            .GetStringAsync();
        var albumResults = JsonConvert.DeserializeObject<JObject>(result,
            new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
        if (albumResults.Value<int>("resultCount") == 0) return null;
        var arrResults = albumResults.Value<JArray>("results");
        var album_duplicate = arrResults.First();
        arrResults.Remove(album_duplicate);
        return arrResults;
    }

    public async Task<JArray> GetVideosByArtistName(string artistName)
    {
        var result = await baseURL
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
        var videoResults = JsonConvert.DeserializeObject<JObject>(result,
            new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
        return videoResults.Value<JArray>("results");
    }

    public async Task<JArray> GetVideosByArtistId(string artistId)
    {
        var result = await baseURL
            .AppendPathSegment("lookup")
            .SetQueryParams(new
            {
                id = artistId,
                entity = "musicVideo",
                country = localCountry,
                limit = resultLimit
            })
            .GetStringAsync();
        var videoResults = JsonConvert.DeserializeObject<JObject>(result,
            new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
        if (videoResults.Value<int>("resultCount") == 0) return null;
        var arrResults = videoResults.Value<JArray>("results");
        var artist_duplicate = arrResults.First();
        arrResults.Remove(artist_duplicate);
        //Remove 'feature-movie' objects
        var filteredResults = JArray.FromObject(arrResults.Where(v => (string)v["kind"] != "feature-movie"));
        return filteredResults;
    }

    public async Task<List<VideoResult>?> GetVideosFromArtistId(string browseId, Artist artist)
    {
        var videos = await GetVideosByArtistId(browseId);
        return videos.Select(x => vidObjToResult(x, artist)).ToList();
    }

    private static AlbumResult albObjToResult(JToken video, Artist artist)
    {
        var artUrl = GetHighQualityArt(video);
        return new AlbumResult(video, artist, artUrl);
    }

    private static VideoResult vidObjToResult(JToken video, Artist artist)
    {
        var artUrl = GetHighQualityArt(video);
        return new VideoResult(video, artist, artUrl);
    }

    public async Task<ObservableCollection<VideoResultViewModel>> GenerateVideoResultList(JArray vidResults,
        Artist artist)
    {
        var results = new ObservableCollection<VideoResultViewModel>();
        var songs = _dbContext.MusicVideos.Include(musicVideo => musicVideo.artist)
            .ThenInclude(art => art.Metadata).ToList();
        for (var i = 0; i < vidResults.Count; i++)
        {
            var video = vidResults[i];
            //Create the VideoResult object
            var artUrl = GetHighQualityArt(video);
            var newVideo = new VideoResult(video, artist, artUrl);

            //Create the VM for the result
            var resultVM = await VideoResultViewModel.CreateViewModel(newVideo);

            //Check if the song already exists in the DataBase
            if (songs != null && songs.Exists(s =>
                    s.artist.Metadata.Any(m => m.SourceId == SearchSource.AppleMusic) &&
                    s.videoID == newVideo.SourceId))
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

    public async Task<ObservableCollection<VideoResultViewModel>> GenerateVideoResultList(Album album)
    {
        var results = new ObservableCollection<VideoResultViewModel>();
        var artistMetadata = album.Artist.GetArtistMetadata(SearchSource.AppleMusic);
        var albumMetadata = album.GetAlbumMetadata(SearchSource.AppleMusic);
        //Get a list of music videos from the artist available
        var currentVideos = await GetVideosByArtistId(artistMetadata.BrowseId);
        //Get a list of songs from the album
        var albumTracks = await GetTrackListByAlbumID(albumMetadata.BrowseId);
        //Match videos with the song tracks
        for (var i = 0; i < albumTracks.Count; i++)
        {
            var track = albumTracks[i];
            //Try for a direct match
            var matchedVideo = currentVideos.FirstOrDefault(v =>
                ((string)v["trackName"]).ToLower() == ((string)track["trackName"]).ToLower());
            //Failing that, try for a contains (useful when subtitles are appended like '4k Upgrade' or 'Official Music Video')
            if (matchedVideo == null)
            {matchedVideo = currentVideos.FirstOrDefault(v =>
                ((string)v["trackName"]).ToLower().Contains(((string)track["trackName"]).ToLower()));
            }
            if (matchedVideo != null)
            {
                //Create the VideoResult object
                var artUrl = GetHighQualityArt(matchedVideo);
                var newVideo = new VideoResult(matchedVideo, album.Artist, artUrl);
                //Create the VM for the result
                var resultVM = await VideoResultViewModel.CreateViewModel(newVideo, album);
                var songs = App.GetDBContext().MusicVideos.Where(mv => mv.album.Id == album.Id).ToList();

                //Check if the song already exists in the DataBase
                if (songs != null && songs.Exists(s =>
                        s.artist.Metadata.Any(m => m.SourceId == SearchSource.AppleMusic) &&
                        s.videoID == newVideo.SourceId))
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
        var artURL = albumObj["artworkUrl100"].ToString();
        return artURL.Replace("100x100", "400x400");
    }

    public string[] GetArtistBannerLinks(string url)
    {
        using (var wc = new WebClient()) // "using" keyword automatically closes WebClient stream on download completed
        {
            var urls = new string[2];
            var source = wc.DownloadString(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(source);

            var element = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'artist-header')]");
            var sources = doc.DocumentNode.SelectNodes("//source");
            var srcset = sources[1].GetAttributeValue("srcset", string.Empty);
            if (!string.IsNullOrEmpty(srcset))
            {
                //urls[0] = srcset.Split(',').Select(url => url.Split(' ')[0]).ElementAt(1);
                //urls[1] = srcset.Split(',').Select(url => url.Split(' ')[0]).ElementAt(0);
                //Regex to ensure that the height matches the width of the returned image, some of them return a cropped image as opposed to the original 1:1
                urls[0] = Regex.Replace(srcset.Split(',').Select(url => url.Split(' ')[0]).ElementAt(1),
                    @"(?<width>\d+)x(?<height>\d+)", match =>
                    {
                        var height = match.Groups["height"].Value;
                        return $"{height}x{height}";
                    });
                urls[1] = Regex.Replace(srcset.Split(',').Select(url => url.Split(' ')[0]).ElementAt(0),
                    @"(?<width>\d+)x(?<height>\d+)", match =>
                    {
                        var height = match.Groups["height"].Value;
                        return $"{height}x{height}";
                    });
            }

            if (element != null)
            {
                var style = element.GetAttributeValue("style", "");
                if (!string.IsNullOrEmpty(style))
                {
                    var matches = Regex.Matches(style, @"--background-image(?:-xs)?:\s*url\((.*?)\);");
                    //urls[0] = matches[0].Groups[1].Value.Trim('\'', '"');
                    //urls[1] = matches[1].Groups[1].Value.Trim('\'', '"');
                    //Regex to ensure that the height matches the width of the returned image, some of them return a cropped image as opposed to the original 1:1
                    urls[0] = Regex.Replace(matches[0].Groups[1].Value.Trim('\'', '"'), @"(?<width>\d+)x(?<height>\d+)",
                        match =>
                        {
                            var height = match.Groups["height"].Value;
                            return $"{height}x{height}";
                        });
                    urls[1] = Regex.Replace(matches[1].Groups[1].Value.Trim('\'', '"'), @"(?<width>\d+)x(?<height>\d+)",
                        match =>
                        {
                            var height = match.Groups["height"].Value;
                            return $"{height}x{height}";
                        });
                }
            }

            return urls;
        }
    }

    public string GetArtistThumb(string url)
    {
        //Some artist urls will be dead itunes links - skip these
        if (url.Contains("itunes.apple.com")) return "";
        using (var wc = new WebClient()) // "using" keyword automatically closes WebClient stream on download completed
        {
            var source = wc.DownloadString(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(source);

            var metaTag = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");
            var result = metaTag?.GetAttributeValue("content", null);
            return result != "https://music.apple.com/assets/meta/apple-music.png"
                ? result.Replace(Path.GetFileName(result), "100x100.jpg")
                : result;
        }
    }
}