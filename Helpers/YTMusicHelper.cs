using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MVNFOEditor.Models;

namespace MVNFOEditor.Helpers
{
    public class YTMusicHelper
    {
        public YTMusicHelper()
        {
            PythonEngine.Initialize();
        }

        public string get_artistID(string artist)
        {
            string result = "";
            dynamic search_results;
            using (Py.GIL())
            {
                dynamic ytmusicapi = Py.Import("ytmusicapi");

                // Now, you can use ytmusicapi as if you were writing Python code
                dynamic ytmusic = ytmusicapi.YTMusic();

                // Call methods, access properties, etc.
                search_results = ytmusic.search(artist);
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
                        var artistObj = ((JArray)curr_result["artists"])[0];
                        result = artistObj["id"].ToString();
                        break;
                    }
                }
            }

            return result;
        }

        public JArray get_videos(string artistId)
        {
            JArray result;
            string browseId;
            string parsedResult;
            dynamic search_results;

            dynamic ytmusicapi;
            dynamic ytmusic;

            using (Py.GIL())
            {
                ytmusicapi = Py.Import("ytmusicapi");

                ytmusic = ytmusicapi.YTMusic();

                search_results = ytmusic.get_artist(artistId);
            }
            parsedResult = search_results.ToString()
                .Replace("None", "null")
                .Replace("True", "true")
                .Replace("False", "false");

            dynamic artistObj = JObject.Parse(parsedResult);

            dynamic initVideos = artistObj.videos;
            browseId = initVideos.browseId;
            if (browseId != null)
            {
                using (Py.GIL())
                {
                    search_results = ytmusic.get_playlist(browseId);
                }
                parsedResult = search_results.ToString()
                    .Replace("None", "null")
                    .Replace("True", "true")
                    .Replace("False", "false");

                dynamic playlistObj = JObject.Parse(parsedResult);

                return playlistObj.tracks;
            }
            else
            {
                return (JArray)initVideos.results;
            }
        }

        public void GetInfoFromVideo(MusicVideo mv)
        {

        }

        public JArray get_album_artists(string artistId)
        {
            JArray result;
            string browseId;
            string paramsId;
            string parsedResult;
            dynamic search_results;

            dynamic ytmusicapi;
            dynamic ytmusic;

            using (Py.GIL())
            {
                ytmusicapi = Py.Import("ytmusicapi");

                ytmusic = ytmusicapi.YTMusic();

                search_results = ytmusic.get_artist(artistId);
            }
            parsedResult = search_results.ToString()
                .Replace("None", "null")
                .Replace("True", "true")
                .Replace("False", "false");

            dynamic artistObj = JObject.Parse(parsedResult);

            dynamic initAlbums = artistObj.albums;
            browseId = initAlbums.browseId;
            paramsId = initAlbums.params;
            if (browseId != null)
            {
                using (Py.GIL())
                {
                    search_results = ytmusic.get_playlist(browseId);
                }
                parsedResult = search_results.ToString()
                    .Replace("None", "null")
                    .Replace("True", "true")
                    .Replace("False", "false");

                dynamic playlistObj = JObject.Parse(parsedResult);

                return playlistObj.tracks;
            }
            else
            {
                return (JArray)initVideos.results;
            }
        }
    }
}
