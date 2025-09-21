using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using log4net;
using MVNFOEditor.Models;
using MVNFOEditor.ViewModels;
using MVNFOEditor.DB;
using MVNFOEditor.Settings;
using YtMusicNet;
using YtMusicNet.Constants;
using YtMusicNet.Handlers;
using YtMusicNet.Records;
using AlbumResult = MVNFOEditor.Models.AlbumResult;
using VideoResult = MVNFOEditor.Models.VideoResult;

namespace MVNFOEditor.Helpers
{
    public class YTMusicHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(YTMusicHelper));
        private static ISettings settings;
        private static YtMusicClient api;
        
        private dynamic _ytMusic;
        private MusicDbContext _dbContext;
        public YTMusicHelper()
        {
            _dbContext = App.GetDBContext();
            settings = App.GetSettings();
        }
        
        public static async Task<YTMusicHelper> CreateHelper(string? authFile = null)
        {
            YTMusicHelper newHelper = new YTMusicHelper();
            if (File.Exists("./Assets/browser.json"))
            {
                authFile = "./Assets/browser.json";
            }
            await newHelper.InitClient(authFile);
            return newHelper;
        }

        private async Task InitClient(string? authFile = null)
        {
            api = await YtMusicClient.CreateAsync(authFilePath:authFile);
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
            List<Result?>? results = await api.Searching.Search(artistStr, FilterType.Artists);
            if (results != null && results.Count > 0)
            {
                return results.ConvertAll(r => new ArtistResult(r.Id, r.Title, r.Thumbnails));
            }
            return null;
        }

        public async Task<ObservableCollection<VideoResultViewModel>> GenerateVideoResultList(List<VideoResult> vidResults, YtMusicNet.Models.Album albumDetails, List<MusicVideo>? songs, Album album)
        {
            ObservableCollection<VideoResultViewModel> results = new ObservableCollection<VideoResultViewModel>();
            //Grab videos that are only within this album
            List<string> albumTitles = albumDetails.Tracks
                .Select(t => (Regex.Replace(t.Title, @"[^\w\s]", "")).ToLower()).ToList();
            //List<string> albumTitles = ((JArray)albumDetails["tracks"]).Select(t => (Regex.Replace((string)t["title"], @"[^\w\s]", "")).ToLower()).ToList();
           // List<JToken> matchingVideos = vidResults.Where(vid => albumTitles.Contains(CleanYTName((string)vid["title"], album.Artist).ToLower())).ToList();
           List<VideoResult> matchingVideos = vidResults
               .Where(vid =>
               {
                   var title = CleanYTName(vid.Title, album.Artist).ToLower();
                   var cleanVidTitle = Regex.Replace(title, @"[^\w\s]", "");
                   return (albumTitles.Contains(cleanVidTitle) || albumTitles.Any(cleanVidTitle.Contains));
               })
               .ToList();

            //Create a list of VideoResultViewModel
            for (int i = 0; i < matchingVideos.Count; i++)
            {
                VideoResult vid = matchingVideos[i];
                var parsedTitle = CleanYTName(vid.Title, album.Artist);
                var vidMetadata = await api.Browse.GetSong(vid.VideoID);
                if (vidMetadata != null)
                {
                    var topRes = vidMetadata.StreamingData.AdaptiveFormats.OrderByDescending(af => af.Bitrate).First();
                    vid.TopRes = topRes.QualityLabel;
                }
                VideoResultViewModel resultVM = new VideoResultViewModel(vid, album);
                //Check if the song already exists in the DataBase
                if (songs!= null && songs.Exists(s => s.videoID == vid.VideoID || s.title.ToLower() == parsedTitle.ToLower()))
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

        public async Task<ObservableCollection<VideoResultViewModel>> GenerateVideoResultList(List<VideoResult> vidResults, Artist artist)
        {
            ObservableCollection<VideoResultViewModel> results = new ObservableCollection<VideoResultViewModel>();
            var songs = _dbContext.MusicVideos.ToList();
            for (int i = 0; i < vidResults.Count; i++)
            {
                VideoResult result = vidResults[i];
                var parsedTitle = CleanYTName(result.Title, artist);
                /*
                var vidMetadata = await api.Browse.GetSong(result.VideoID);
                if (vidMetadata != null)
                {
                    var topRes = vidMetadata.StreamingData.AdaptiveFormats.OrderByDescending(af => af.Bitrate).First();
                    result.TopRes = topRes.QualityLabel;
                }
                */
                VideoResultViewModel resultVM = new VideoResultViewModel(result);
                //Check if the song already exists in the DataBase
                if (songs != null && songs.Exists(s => s.videoID == result.VideoID || s.title.ToLower() == parsedTitle.ToLower()))
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

        public async Task<List<YtMusicNet.Models.Album>> GetAlbums(YtMusicNet.Models.Artist artist, ArtistOrderType sorting = ArtistOrderType.Default)
        {
            return await api.Browse.GetArtistAlbums(artist.Albums.BrowseId, artist.Albums.Params, sorting);
        }

        public async Task<YtMusicNet.Models.Album?> GetAlbum(string albumId)
        {
            return await api.Browse.GetAlbum(albumId);
        }
        
        public async Task<List<AlbumResult>?> GetAlbums(string artistId, Artist artist, ArtistOrderType sorting = ArtistOrderType.Default)
        {
            YtMusicNet.Models.Artist? selectedArtist = await api.Browse.GetArtist(artistId);
            if (selectedArtist == null)
            {
                return null;
            }
            List<YtMusicNet.Models.Album>? ytAlbums = selectedArtist?.Albums?.Results;
            //If we have a browseID then the results won't be the full list
            if (selectedArtist.Albums.BrowseId != null)
            {
                ytAlbums = await api.Browse.GetArtistAlbums(selectedArtist.Albums.BrowseId, selectedArtist.Albums.Params, sorting);
            }
            return ytAlbums.ConvertAll(a => AlbumToResult(a, artist));
        }

        public async Task<List<VideoResult>?> GetVideosFromArtistId(string browseId, Artist artist)
        {            
            YtMusicNet.Models.Playlist videoPlaylist = await api.Playlists.GetPlaylist(browseId);
            return videoPlaylist?.Tracks?.ConvertAll(v => TrackToResult(v, artist));
        }

        private static VideoResult TrackToResult(YtMusicNet.Models.Track track, Artist artist)
        {
            return new VideoResult(track, artist);
        }

        private static AlbumResult AlbumToResult(YtMusicNet.Models.Album album, Artist artist)
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
                .Replace(artist.Name,"", StringComparison.OrdinalIgnoreCase)
                .Replace("\"","", StringComparison.OrdinalIgnoreCase);
        }
    }
}