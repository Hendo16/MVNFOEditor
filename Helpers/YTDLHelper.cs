using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using MVNFOEditor.DB;
using MVNFOEditor.Models;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace MVNFOEditor.Helpers
{
    public class YTDLHelper
    {
        private YoutubeDL ytdl;
        private OptionSet _settings;
        public YTDLHelper()
        {
            ytdl = new YoutubeDL();
            _settings = new OptionSet();
            MusicDbContext db = App.GetDBContext();
            SettingsData setData = db.SettingsData.SingleOrDefault();
            if (setData != null)
            {
                ytdl.FFmpegPath = setData.FFMPEGPath;
                ytdl.YoutubeDLPath = setData.YTDLPath;
                _settings.Format = setData.YTDLFormat;
            }
        }

        public async Task<RunResult<string>> DownloadVideo(string id, string outputFolder, string outputName, Progress<DownloadProgress> progress)
        {
            _settings.Paths = outputFolder;
            _settings.Output = outputName + "-video.%(ext)s";
            _settings.RestrictFilenames = true;

            return await ytdl.RunVideoDownload($"https://www.youtube.com/watch?v={id}", overrideOptions: _settings, progress:progress);
        }

        public async Task<VideoData> GetVideoInfo(string URL)
        {
            var result = await ytdl.RunVideoDataFetch(URL);
            return result.Data;
        }
        
    }
}