using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MVNFOEditor.Models
{
    public class AlbumResult
    {
        public string Title { get; set; }
        public string? Year { get; set; }
        public string browseId { get; set; }
        public string thumbURL { get; set; }
        public bool? isExplicit { get; set; }
        public Artist Artist { get; set; }

        private static HttpClient s_httpClient = new();
        public AlbumResult() { }

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
            return File.OpenWrite(folderPath + $"/{Title}-video.jpg");
        }
    }
}
