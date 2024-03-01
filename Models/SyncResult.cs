using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MVNFOEditor.Models
{
    public class SyncResult
    {
        public string Title { get; set; }
        public Artist Artist { get; set; }
        public string? Duration { get; set; }
        public string vidID { get; set; }
        public string thumbURL { get; set; }
        private static HttpClient s_httpClient = new();
        public SyncResult() { }

        public async Task<Stream> LoadCoverBitmapAsync()
        {
            if (thumbURL != "")
            {
                var data = await s_httpClient.GetByteArrayAsync(thumbURL);
                return new MemoryStream(data);
            }
            else
            {
                return File.OpenRead("./Assets/sddefault.jpg");
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
