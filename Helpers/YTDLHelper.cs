using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using MVNFOEditor.DB;
using MVNFOEditor.Models;
using MVNFOEditor.Settings;
using Newtonsoft.Json;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace MVNFOEditor.Helpers
{
    public class YTDLHelper
    {
        private YoutubeDL ytdl;
        private OptionSet _settings;
        private static ISettings _settingsData;
        private YTDLHelper()
        {
            ytdl = new YoutubeDL();
            _settings = new OptionSet();
            _settingsData = App.GetSettings();
            ytdl.FFmpegPath = _settingsData.FFMPEGPath;
            ytdl.YoutubeDLPath = _settingsData.YTDLPath;
        }

        public static YTDLHelper CreateHelper()
        {
            YTDLHelper newHelper = new YTDLHelper();
            return newHelper;
        }

        public async Task<RunResult<string>> DownloadVideo(string id, string outputFolder, string outputName, Progress<DownloadProgress> progress)
        {
            //Weird bug where if the last character is a '.', it will be replaced with a # and throw the whole system off. So we'll just remove it
            if(outputFolder.Last() == '.')
            {
                outputFolder = outputFolder.Remove(outputFolder.Length - 1, 1);
            }
            _settings.RestrictFilenames = true;
            _settings.Paths = outputFolder;
            _settings.Output = outputName + ".%(ext)s";
            _settings.Format = GetFormat();

            return await ytdl.RunVideoDownload($"https://www.youtube.com/watch?v={id}", overrideOptions: _settings, progress:progress);
        }

        public string GetFormat()
        {
            var formatString = "";
            if (_settingsData.YTDLFormat == "")
            {
                switch (_settingsData.YTDLResolution)
                {
                    case "720p":
                        formatString = "bestvideo[width<=1280]+bestaudio/best[width<=1280]";
                        break;
                    case "1080p":
                        formatString = "bestvideo[width<=1920]+bestaudio/best[width<=1920]";
                        break;
                    case "1440p":
                        formatString = "bestvideo[width<=2560]+bestaudio/best[width<=2560]";
                        break;
                    case "4k":
                        formatString = "bestvideo[width<=3840]+bestaudio/best[width<=3840]";
                        break;
                }
                return formatString;
            }
            return _settingsData.YTDLFormat;
        }

        public async Task<VideoData> GetVideoInfo(string URL)
        {
            var result = await ytdl.RunVideoDataFetch(URL);
            return result.Data;
        }

        public async Task<VideoData> GetVideoFormats(string vidID)
        {        
            // Set up options to fetch format information only
            var options = new OptionSet();
            options.DumpSingleJson = true;
            var result = await ytdl.RunVideoDataFetch($"https://www.youtube.com/watch?v={vidID}", overrideOptions: options);
            if (result.Success)
            {
                return result.Data;
            }
            else
            {
                return null;
            }
        }
    }
}