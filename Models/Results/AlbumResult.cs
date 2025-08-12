using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Models
{
    public class 
        AlbumResult
    {
        public string Title { get; set; }
        public string? Year { get; set; }
        public string browseId { get; set; }
        public string thumbURL { get; set; }
        public bool? isExplicit { get; set; }
        public Artist Artist { get; set; }

        private static HttpClient s_httpClient = new();

        public AlbumResult(YtMusicNet.Models.Album ytAlbum, Artist artist)
        {
            Artist = artist;
            Title = ytAlbum.Title;
            Year = ytAlbum.Year.ToString();
            browseId = ytAlbum.Id;
            thumbURL = ytAlbum.Thumbnails.Last().URL;
            isExplicit = ytAlbum.IsExplicit;
        }
        public AlbumResult() { }

        public static List<AlbumResult> GetAlbumResults<T>(List<T> albums)
        {
            List<AlbumResult> results = new List<AlbumResult>();
            switch (albums)
            {
                case List<YtMusicNet.Models.Album> ytList:
                    for (int i = 0; i < ytList.Count; i++)
                    {
                        YtMusicNet.Models.Album currAlbum = ytList[i];
                        AlbumResult newResult = new AlbumResult();
                        
                        newResult.Title = currAlbum.Title;
                        newResult.Year = currAlbum.Year.ToString();
                        newResult.browseId = currAlbum.Id;
                        newResult.thumbURL = currAlbum.Thumbnails.Last().URL;
                        newResult.isExplicit = currAlbum.IsExplicit;
                        
                        results.Add(newResult);
                    }
                    break;
            }

            return results;
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
            return File.OpenWrite(folderPath + $"/{Title}.jpg");
        }
    }
}
