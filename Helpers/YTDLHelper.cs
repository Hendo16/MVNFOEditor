using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVNFOEditor.DB;
using MVNFOEditor.Models;
using YoutubeDLSharp;

namespace MVNFOEditor.Helpers
{
    public class YTDLHelper
    {
        private YoutubeDL ytdl;
        public YTDLHelper()
        {
            ytdl = new YoutubeDL();
            MusicDbContext db = App.GetDBContext();
            SettingsData setData = db.SettingsData.SingleOrDefault();
            if (setData != null)
            {
                ytdl.FFmpegPath = setData.FFMPEGPath;
                ytdl.YoutubeDLPath = setData.YTDLPath;
                ytdl.OutputFolder = setData.OutputFolder;
            }
        }

        public async void DownloadVideo(string id)
        {
            var res = await ytdl.RunVideoDownload($"https://www.youtube.com/watch?v={id}");
            string path = res.Data;
        }

        public void UpdateSettings(SettingsData setData)
        {
            ytdl.FFmpegPath = setData.FFMPEGPath;
            ytdl.YoutubeDLPath = setData.YTDLPath;
            ytdl.OutputFolder = setData.OutputFolder;
        }
    }
}
