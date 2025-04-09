using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MVNFOEditor.Models;
using MVNFOEditor.ViewModels;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using MVNFOEditor.DB;
using SukiUI.Controls;
using SukiUI.Dialogs;
using SukiUI.Enums;
using SukiUI.Toasts;
using YoutubeDLSharp.Metadata;

namespace MVNFOEditor.Helpers
{
    public class YTMusicHelper
    {
        private dynamic _ytMusic;
        private MusicDbContext _dbContext;
        public YTMusicHelper()
        {
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();
            using (Py.GIL())
            {
                dynamic ytmusicapi = Py.Import("ytmusicapi");

                _ytMusic = ytmusicapi.YTMusic("./Assets/oauth.json");
            }
            _dbContext = App.GetDBContext();
        }

        public JArray search_Artists(string artist)
        {
            string result = "";
            dynamic search_results;
            using (Py.GIL())
            {
                // Call methods, access properties, etc.
                search_results = _ytMusic.search(artist,"artists");
            }
            string parsedResult = search_results.ToString()
                .Replace("None", "null")
                .Replace("True", "true")
                .Replace("False", "false")
                .Replace("\\xa0", " ");
            return JArray.Parse(parsedResult);
        }

        public string get_artistID(string artist)
        {
            string result = "";
            dynamic search_results = "";
            using (Py.GIL())
            {
                // Call methods, access properties, etc.
                search_results = _ytMusic.search(artist);
            }
            string parsedResult = search_results.ToString()
                .Replace("None","null")
                .Replace("True","true")
                .Replace("False","false");
            List<Dictionary<string, object>> jsonArray = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(parsedResult);

            for (int i = 0; i < jsonArray.Count; i++)
            {
                var curr_result = jsonArray[i];
                if (curr_result.ContainsKey("category"))
                {
                    if (curr_result["category"].ToString() == "Top result")
                    {
                        try
                        {
                            var artistObj = ((JArray)curr_result["artists"])[0];
                            result = artistObj["id"].ToString();
                            break;
                        }
                        catch (KeyNotFoundException e)
                        {
                            result = "null";
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public JArray? get_videos(string artistId)
        {
            string browseId;
            string parsedResult;
            dynamic search_results;

            using (Py.GIL())
            {
                search_results = _ytMusic.get_artist(artistId);
            }
            parsedResult = search_results.ToString()
                .Replace("None", "null")
                .Replace("True", "true")
                .Replace("False", "false")
                .Replace("\\xa0", " ");

            dynamic artistObj = JObject.Parse(parsedResult);

            dynamic initVideos = artistObj.videos;
            if (initVideos == null)
            {
                return null;
            }
            browseId = initVideos.browseId;
            if (browseId != null)
            {
                using (Py.GIL())
                {
                    try
                    {
                        search_results = _ytMusic.get_playlist(browseId);
                    }
                    catch (PythonException e)
                    {
                        App.GetVM().GetToastManager().CreateToast()
                            .WithTitle("Error")
                            .WithContent("Error fetching video list, please wait a few seconds and try again")
                            .OfType(NotificationType.Error)
                            .Queue();
                        return null;
                    }
                }
                parsedResult = search_results.ToString()
                    .Replace("None", "null")
                    .Replace("True", "true")
                    .Replace("False", "false")
                    .Replace("\\xa0", " ");

                dynamic playlistObj = JObject.Parse(parsedResult);

                return playlistObj.tracks;
            }
            else
            {
                return (JArray)initVideos.results;
            }
        }

        public JObject get_album(string albumId)
        {
            string parsedResult;
            dynamic search_results;

            using (Py.GIL())
            {
                search_results = _ytMusic.get_album(albumId);
            }
            parsedResult = search_results.ToString()
                .Replace("None", "null")
                .Replace("True", "true")
                .Replace("False", "false")
                .Replace("\\xa0", " ");

            return JObject.Parse(parsedResult);
        }

        public async Task<ObservableCollection<AlbumResultViewModel>> GenerateAlbumResultList(Artist artist)
        {
            ObservableCollection<AlbumResultViewModel> results = new ObservableCollection<AlbumResultViewModel>();
            ArtistMetadata artistMetadata = artist.GetArtistMetadata(SearchSource.YouTubeMusic);
            for (int i = 0; i < artistMetadata.AlbumResults.Count; i++)
            {
                var currAlbum = artistMetadata.AlbumResults[i];
                AlbumResult currResult = new AlbumResult();

                currResult.Title = currAlbum["title"].ToString();
                currResult.Year = currAlbum["year"] != null ? currAlbum["year"].ToString() : "";
                currResult.browseId = currAlbum["browseId"].ToString();
                currResult.thumbURL = GetHighQualityArt((JObject)currAlbum);
                currResult.isExplicit = Convert.ToBoolean(currAlbum["isExplicit"]);
                currResult.Artist = artist;
                AlbumResultViewModel newVM = new AlbumResultViewModel(currResult);
                await newVM.LoadThumbnail();
                results.Add(newVM);
            }

            return results;
        }

        public async Task<ObservableCollection<VideoResultViewModel>> GenerateVideoResultList(JArray vidResults, JObject albumDetails, List<MusicVideo>? songs, Album album)
        {
            ObservableCollection<VideoResultViewModel> results = new ObservableCollection<VideoResultViewModel>();
            //Grab videos that are only within this album
            List<string> albumTitles = ((JArray)albumDetails["tracks"]).Select(t => (Regex.Replace((string)t["title"], @"[^\w\s]", "")).ToLower()).ToList();
           // List<JToken> matchingVideos = vidResults.Where(vid => albumTitles.Contains(CleanYTName((string)vid["title"], album.Artist).ToLower())).ToList();
            List<JToken> matchingVideos = vidResults
                .Where(vid =>
                {
                    var title = CleanYTName((string)vid["title"], album.Artist).ToLower();
                    var cleanVidTitle = Regex.Replace(title, @"[^\w\s]", "");
                    return title != null && (albumTitles.Contains(cleanVidTitle) || albumTitles.Any(cleanVidTitle.Contains));
                })
                .ToList();

            //Create a list of VideoResultViewModel
            for (int i = 0; i < matchingVideos.Count; i++)
            {
                JToken vid = matchingVideos[i];
                VideoResult newResult = new VideoResult();
                var parsedTitle = CleanYTName(vid["title"].ToString(), album.Artist);

                //newResult.Artist = vid["artists"][0]["name"].ToString();
                newResult.Artist = album.Artist;
                newResult.Title = parsedTitle;
                newResult.VideoID = vid["videoId"].ToString();
                newResult.thumbURL = vid["thumbnails"][0]["url"].ToString();

                //'Duration' is null sometimes
                try { newResult.Duration = vid["duration"].ToString(); } catch (NullReferenceException e) { }
                
                //Until I find a faster way to get resolutions for videos, this is commented out for the time being :(
                /*
                //YTDL VideoData
                VideoData vidData = await App.GetYTDLHelper()
                    .GetVideoFormats(newResult.VideoID);
                if (vidData != null)
                {
                    var list = (vidData.Formats.Where(f => f.FormatNote != null && f.Resolution != "audio only")).OrderByDescending(f => f.AverageBitrate);
                    newResult.TopRes = list.FirstOrDefault().FormatNote == "Premium" ? "1080p (Premium)" : list.FirstOrDefault().FormatNote;
                }
                */
                VideoResultViewModel resultVM = new VideoResultViewModel(newResult, album);
                //Check if the song already exists in the DataBase
                if (songs!= null && songs.Exists(s => s.videoID == newResult.VideoID || s.title.ToLower() == parsedTitle.ToLower()))
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

        public async Task<ObservableCollection<VideoResultViewModel>> GenerateVideoResultList(JArray vidResults, Artist artist)
        {
            ObservableCollection<VideoResultViewModel> results = new ObservableCollection<VideoResultViewModel>();
            var songs = _dbContext.MusicVideos.ToList();
            for (int i = 0; i < vidResults.Count; i++)
            {
                var vid = vidResults[i];
                var parsedTitle = CleanYTName(vid["title"].ToString(), artist);
                VideoResult newResult = new VideoResult();

                newResult.Title = parsedTitle;
                newResult.VideoID = vid["videoId"].ToString();
                newResult.thumbURL = vid["thumbnails"][0]["url"].ToString();
                newResult.Artist = artist;
                //'Duration' is null sometimes
                try { newResult.Duration = vid["duration"].ToString(); } catch (NullReferenceException e) { }
                //Until I find a faster way to get resolutions for videos, this is commented out for the time being :(
                /*
                //YTDL VideoData
                VideoData vidData = await App.GetYTDLHelper()
                    .GetVideoFormats(newResult.VideoID);
                if (vidData != null)
                {
                    var list = (vidData.Formats.Where(f => f.FormatNote != null && f.Resolution != "audio only")).OrderByDescending(f => f.AverageBitrate);
                    newResult.TopRes = list.FirstOrDefault().FormatNote == "Premium" ? "1080p (Premium)" : list.FirstOrDefault().FormatNote;
                }
                */
                VideoResultViewModel resultVM = new VideoResultViewModel(newResult);
                //Check if the song already exists in the DataBase
                if (songs != null && songs.Exists(s => s.videoID == newResult.VideoID || s.title.ToLower() == parsedTitle.ToLower()))
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

        public void GetInfoFromVideo(MusicVideo mv)
        {

        }

        public string GetArtistBanner(string artistId, int width)
        {
            string result="";
            string parsedResult;
            dynamic search_results;

            using (Py.GIL())
            {
                search_results = _ytMusic.get_artist(artistId);
            }
            parsedResult = search_results.ToString()
                .Replace("None", "null")
                .Replace("True", "true")
                .Replace("False", "false")
                .Replace("\\xa0", " ");
            dynamic artistObj = JObject.Parse(parsedResult);
            JArray bannerList = (JArray)artistObj.thumbnails;
            for (int i = 0; i < bannerList.Count; i++)
            {
                JToken bannerObj = bannerList[i];
                if (int.Parse(bannerObj["width"].ToString()) >= width)
                {
                    result = bannerObj["url"].ToString();
                }
            }
            return result;
        }

        public JArray GetAlbums(string artistId)
        {
            string browseId;
            string searchParams;
            string parsedResult;
            dynamic search_results;
            JArray albumList;

            using (Py.GIL())
            {
                search_results = _ytMusic.get_artist(artistId);
            }
            parsedResult = search_results.ToString()
                .Replace("None", "null")
                .Replace("True", "true")
                .Replace("False", "false")
                .Replace("\\xa0", " ");

            dynamic artistObj = JObject.Parse(parsedResult);
            if (artistObj.albums == null) {return null;}
            JToken initAlbums = artistObj.albums;
            browseId = initAlbums["browseId"].ToString();
            if (browseId != "")
            {
                searchParams = initAlbums["params"].ToString();
                using (Py.GIL())
                {
                    search_results = _ytMusic.get_artist_albums(browseId, searchParams);
                }
                parsedResult = search_results.ToString()
                    .Replace("None", "null")
                    .Replace("True", "true")
                    .Replace("False", "false")
                    .Replace("\\xa0", " ");
                
                albumList = JArray.Parse(parsedResult);
            }
            else
            {
                albumList = (JArray)initAlbums["results"];
            }

            return albumList;
        }

        public JObject GetAlbumObj(string albumName, Artist artist)
        {
            ArtistMetadata artistMetadata = artist.GetArtistMetadata(SearchSource.YouTubeMusic);
            JArray albumList = artistMetadata.AlbumResults;
            for (int i = 0; i < albumList.Count; i++)
            {
                //Checking if album matches the object result
                if (albumList[i] is JObject obj &&
                    obj.TryGetValue("title", out var titleObj) &&
                    titleObj is JValue titleValue &&
                    (titleValue.Value.ToString().ToLower() == albumName.ToLower() ||
                     titleValue.Value.ToString().ToLower().Contains(albumName.ToLower())))
                {
                    return obj;
                }
            }
            //Album not in Artist album list, will have to perform a search
            string result = "";
            dynamic search_results;
            using (Py.GIL())
            {
                // Call methods, access properties, etc.
                search_results = _ytMusic.search(albumName);
            }
            string parsedResult = search_results.ToString()
                .Replace("None", "null")
                .Replace("True", "true")
                .Replace("False", "false")
                .Replace("\\xa0", " ");
            JArray albumSearchResult = JArray.Parse(parsedResult);
            for (int i = 0; i < albumSearchResult.Count; i++)
            {
                JObject currentResult = (JObject)albumSearchResult[i];
                if (currentResult["category"].ToString() == "Top result" &&
                    currentResult["resultType"].ToString() == "album" &&
                    ((JArray)currentResult["artists"]).Select(e => ((string)e["name"]).ToLower() == artist.Name.ToLower()).Count() > 0)
                {
                    return currentResult;
                }
            }
            return null;
        }

        public string GetHighQualityArt(JObject albumObj)
        {
            string artURL = "";
            int artWidth = 0;
            //Finding actual art
            JArray thumbnails = (JArray)albumObj["thumbnails"];
            for (int j = 0; j < thumbnails.Count; j++)
            {
                var currArt = thumbnails[j];
                var parsedWidth = int.Parse(currArt["width"].ToString());

                if (artURL == "")
                {
                    artURL = currArt["url"].ToString();
                    artWidth = parsedWidth;
                }
                else if (artWidth < parsedWidth)
                {
                    artURL = currArt["url"].ToString();
                    artWidth = parsedWidth;
                }
            }
            return artURL;
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