using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeDLSharp;

namespace MVNFOEditor.Helpers
{
    public class YTDLHelper
    {
        private YoutubeDL ytdl;
        public YTDLHelper()
        {
            ytdl = new YoutubeDL();
        }

        public async void DownloadVideo(string id)
        {
            var res = await ytdl.RunVideoDownload($"https://www.youtube.com/watch?v={id}");
            string path = res.Data;
        }
    }
}
