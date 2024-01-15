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

namespace MVNFOEditor.Helpers
{
    public class YTMusicHelper
    {
        public YTMusicHelper()
        {
            PythonEngine.Initialize();
        }

        public string get_artist(string artist)
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
                        Debug.WriteLine(curr_result["title"]);

                    }
                }
            }

            return result;
        }

        public string get_videos(string artistId)
        {
            string result = "";
            using (Py.GIL())
            {
                dynamic ytmusicapi = Py.Import("ytmusicapi");

                // Now, you can use ytmusicapi as if you were writing Python code
                dynamic ytmusic = ytmusicapi.YTMusic();

                // Call methods, access properties, etc.
                dynamic search_results = ytmusic.get_artist(artistId);

                // Process the search results using .NET code
                result = search_results.ToString();
            }
            return result;
        }
    }
}
