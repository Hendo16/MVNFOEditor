using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using YtMusicNet.Records;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Models
{
    public class ArtistResultCard
    {
        public string Name { get; set; }
        public string browseId { get; set; }
        public string artistLinkURL { get; set; }
        public string thumbURL { get; set; }
        
        public SearchSource Source { get; set; }

        private static HttpClient s_httpClient = new();

        public ArtistResultCard(JObject result, string artistURL)
        {
            Name = result.Value<string>("artistName");
            browseId = result.Value<int>("artistId").ToString();
            artistLinkURL = artistURL;
            thumbURL = App.GetiTunesHelper().GetArtistBannerLinks(artistURL)[0];
            Source = SearchSource.AppleMusic;
        }
        public ArtistResultCard(ArtistResult result)
        {
            Name = result.Title;
            browseId = result.Id;
            thumbURL = result.Thumbnails.Last().URL;
            Source = SearchSource.YouTubeMusic;
        }

        public async Task<Stream> LoadCoverBitmapAsync()
        {
            if (thumbURL != "")
            {
                var data = await s_httpClient.GetByteArrayAsync(thumbURL);
                return new MemoryStream(data);
            }
            else
            {
                return File.OpenRead("./Assets/tmbte-FULL.jpg");
            }
        }

        public Stream SaveThumbnailBitmapStream(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return File.OpenWrite(folderPath + $"/{Name}.jpg");
        }
    }
}
