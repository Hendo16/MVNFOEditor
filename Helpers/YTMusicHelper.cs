using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using log4net;
using MVNFOEditor.DB;
using MVNFOEditor.Models;
using MVNFOEditor.Settings;
using MVNFOEditor.ViewModels;
using YtMusicNet;
using YtMusicNet.Constants;
using YtMusicNet.Handlers;
using YtMusicNet.Models;
using Album = YtMusicNet.Models.Album;
using AlbumResult = MVNFOEditor.Models.AlbumResult;
using Artist = MVNFOEditor.Models.Artist;
using ArtistResult = YtMusicNet.Records.ArtistResult;
using VideoResult = MVNFOEditor.Models.VideoResult;

namespace MVNFOEditor.Helpers;

public class YTMusicHelper
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(YTMusicHelper));
    private static YtMusicClient api;
    private readonly MusicDbContext _dbContext;

    private dynamic _ytMusic;

    public YTMusicHelper()
    {
        _dbContext = App.GetDBContext();
    }

    public static async Task<YTMusicHelper> CreateHelper(string? authFile = null)
    {
        var newHelper = new YTMusicHelper();
        if (File.Exists("./Assets/browser.json")) authFile = "./Assets/browser.json";
        await newHelper.InitClient(authFile);
        return newHelper;
    }

    private async Task InitClient(string? authFile = null)
    {
        api = await YtMusicClient.CreateAsync(authFile);
    }

    public bool SetupBrowserHeaders(string headers)
    {
        try
        {
            var json = NetworkHandler.BrowserSetup(headers);
            if (json != "{}")
            {
                File.WriteAllText("./Assets/browser.json", json);
                return File.Exists("./Assets/browser.json");
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error in YTMusicHelper->SetupBrowserHeaders: Exception in Setting Up Browser.json");
            Log.Error(ex);
        }

        return false;
    }

    public async Task<List<ArtistResult?>?> searchArtists(string artistStr)
    {
        try
        {
            var results = await api.Searching.Search(artistStr, FilterType.Artists);
            if (results != null && results.Count > 0)
                return results.ConvertAll(r => new ArtistResult(r.Id, r.Title, r.Thumbnails));
            return null;
        }
        catch (HttpRequestException e)
        {
            Log.ErrorFormat("Error: YoutubeMusic returned {0} when searching for {1}", e.StatusCode, artistStr);
            Log.Error(e);
            return null;
        }
    }

    public async Task<ObservableCollection<VideoResultViewModel>> GenerateVideoResultList(List<VideoResult> vidResults,
        Album albumDetails, Models.Album dbAlbum)
    {
        var results = new ObservableCollection<VideoResultViewModel>();
        //Grab videos that are only within this album
        var albumTitles = albumDetails.Tracks
            .Select(t => Regex.Replace(t.Title, @"[^\w\s]", "").ToLower()).ToList();
        //List<string> albumTitles = ((JArray)albumDetails["tracks"]).Select(t => (Regex.Replace((string)t["title"], @"[^\w\s]", "")).ToLower()).ToList();
        // List<JToken> matchingVideos = vidResults.Where(vid => albumTitles.Contains(CleanYTName((string)vid["title"], album.Artist).ToLower())).ToList();
        var matchingVideos = vidResults
            .Where(vid =>
            {
                var title = CleanYTName(vid.Name, dbAlbum.Artist).ToLower();
                var cleanVidTitle = Regex.Replace(title, @"[^\w\s]", "");
                return albumTitles.Contains(cleanVidTitle) || albumTitles.Any(cleanVidTitle.Contains);
            })
            .ToList();

        //Create a list of VideoResultViewModel
        for (var i = 0; i < matchingVideos.Count; i++)
        {
            var vid = matchingVideos[i];
            var parsedTitle = CleanYTName(vid.Name, dbAlbum.Artist);
            var vidMetadata = await api.Browse.GetSong(vid.SourceId);
            if (vidMetadata != null)
            {
                var topRes = vidMetadata.StreamingData.AdaptiveFormats.OrderByDescending(af => af.Bitrate).First();
                vid.TopRes = topRes.QualityLabel;
            }

            var resultVM = new VideoResultViewModel(vid, dbAlbum);
            var songs = App.GetDBContext().MusicVideos.Where(mv => mv.album.Id == dbAlbum.Id).ToList();
            //Check if the song already exists in the DataBase
            if (songs != null &&
                songs.Exists(s => s.videoID == vid.SourceId || s.title.ToLower() == parsedTitle.ToLower()))
            {
                resultVM.BorderColor = "Green";
                resultVM.DownloadEnabled = false;
                resultVM.DownloadBtnText = "Downloaded";
            }
            else if (songs != null && songs.Exists(s => s.title.ToLower().Contains(parsedTitle.ToLower())))
            {
                resultVM.BorderColor = "Orange";
                resultVM.DownloadEnabled = false;
                resultVM.DownloadBtnText = "Downloaded";
            }
            else
            {
                resultVM.BorderColor = "Black";
                resultVM.DownloadEnabled = true;
                resultVM.DownloadBtnText = "Download";
            }

            await resultVM.LoadThumbnail();
            results.Add(resultVM);
        }

        return results;
    }

    public async Task<ObservableCollection<VideoResultViewModel>> GenerateVideoResultList(List<VideoResult> vidResults,
        Artist artist)
    {
        var results = new ObservableCollection<VideoResultViewModel>();
        var songs = _dbContext.MusicVideos.ToList();
        for (var i = 0; i < vidResults.Count; i++)
        {
            var result = vidResults[i];
            var parsedTitle = CleanYTName(result.Name, artist);
            /*
            var vidMetadata = await api.Browse.GetSong(result.VideoID);
            if (vidMetadata != null)
            {
                var topRes = vidMetadata.StreamingData.AdaptiveFormats.OrderByDescending(af => af.Bitrate).First();
                result.TopRes = topRes.QualityLabel;
            }
            */
            var resultVM = new VideoResultViewModel(result);
            //Check if the song already exists in the DataBase
            if (songs != null &&
                songs.Exists(s => s.videoID == result.SourceId || s.title.ToLower() == parsedTitle.ToLower()))
            {
                resultVM.BorderColor = "Green";
                resultVM.DownloadEnabled = false;
                resultVM.DownloadBtnText = "Downloaded";
            }
            else if (songs != null && songs.Exists(s => s.title.ToLower().Contains(parsedTitle.ToLower())))
            {
                resultVM.BorderColor = "Orange";
                resultVM.DownloadEnabled = false;
                resultVM.DownloadBtnText = "Downloaded";
            }
            else
            {
                resultVM.BorderColor = "Black";
                resultVM.DownloadEnabled = true;
                resultVM.DownloadBtnText = "Download";
            }

            await resultVM.LoadThumbnail();
            results.Add(resultVM);
        }

        return results;
    }

    public async Task<YtMusicNet.Models.Artist?> GetArtist(string artistId)
    {
        return await api.Browse.GetArtist(artistId);
    }

    public async Task<List<Album>> GetAlbums(YtMusicNet.Models.Artist artist,
        ArtistOrderType sorting = ArtistOrderType.Default)
    {
        return await api.Browse.GetArtistAlbums(artist.Albums.BrowseId, artist.Albums.Params, sorting);
    }

    public async Task<Album?> GetAlbum(string albumId)
    {
        return await api.Browse.GetAlbum(albumId);
    }

    public async Task<List<AlbumResult>?> GetAlbums(string artistId, Artist artist,
        ArtistOrderType sorting = ArtistOrderType.Default)
    {
        var selectedArtist = await api.Browse.GetArtist(artistId);
        if (selectedArtist == null) return null;
        var ytAlbums = selectedArtist.Albums?.Results;
        if (ytAlbums == null) return null;
        //If we have a browseID then the results won't be the full list
        if (selectedArtist.Albums?.BrowseId != null)
            ytAlbums = await api.Browse.GetArtistAlbums(selectedArtist.Albums.BrowseId, selectedArtist.Albums.Params,
                sorting);
        return ytAlbums.ConvertAll(a => AlbumToResult(a, artist));
    }

    public async Task<List<VideoResult>?> GetVideosFromArtistId(string? vidBrowseId, string? channelId, Artist artist)
    {
        //Check that we have a valid browse Id for the video playlist
        if (vidBrowseId != null)
        {
            var videoPlaylist = await api.Playlists.GetPlaylist(vidBrowseId);
            return videoPlaylist?.Tracks?.ConvertAll(v => TrackToResult(v, artist));
        }

        //If not, we need the original list of videos attached to the artist
        //Re-fetch the artist object that has all available videos loaded up
        var artistObj = await api.Browse.GetArtist(channelId);
        return artistObj?.Videos?.Results.ConvertAll(v => TrackToResult(v, artist));
    }

    private static VideoResult TrackToResult(Track track, Artist artist)
    {
        return new VideoResult(track, artist);
    }

    private static AlbumResult AlbumToResult(Album album, Artist artist)
    {
        return new AlbumResult(album, artist);
    }

    public string CleanYTName(string name, Artist artist)
    {
        return name.Replace(" (Official Video)", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" (official music video)", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" (official hd video)", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" [official video]", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" [official music video]", "", StringComparison.OrdinalIgnoreCase)
            .Replace($"{artist.Name} - ", "", StringComparison.OrdinalIgnoreCase)
            .Replace(artist.Name, "", StringComparison.OrdinalIgnoreCase)
            .Replace("\"", "", StringComparison.OrdinalIgnoreCase);
    }
}