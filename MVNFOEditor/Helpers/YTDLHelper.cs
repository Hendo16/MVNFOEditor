using System;
using System.Linq;
using System.Threading.Tasks;
using MVNFOEditor.Models;
using MVNFOEditor.Settings;
using MVNFOEditor.ViewModels;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace MVNFOEditor.Helpers;

public class YTDLHelper
{
    private static ISettings _settingsData;
    private readonly OptionSet _settings;
    private readonly YoutubeDL ytdl;

    private YTDLHelper()
    {
        ytdl = new YoutubeDL();
        _settings = new OptionSet();
        _settingsData = App.GetSettings();
        ytdl.FFmpegPath = $"{_settingsData.FFMPEGPath}\\ffmpeg.exe";
        ytdl.YoutubeDLPath = $"{_settingsData.YTDLPath}\\yt-dlp.exe";
    }

    public static YTDLHelper CreateHelper()
    {
        var newHelper = new YTDLHelper();
        return newHelper;
    }

    public async Task<RunResult<string>> DownloadVideo(VideoResultViewModel video,
        Progress<DownloadProgress>? progress = null)
    {
        string outputFolder = $"{App.GetSettings().RootFolder}\\{video.Artist.Name}";
        //Weird bug where if the last character is a '.', it will be replaced with a # and throw the whole system off. So we'll just remove it
        if (outputFolder.Last() == '.') outputFolder = outputFolder.Remove(outputFolder.Length - 1, 1);
        _settings.RestrictFilenames = true;
        _settings.Paths = outputFolder;
        _settings.Output = $"{video.Title}-{SearchSource.YouTubeMusic.ToString()}.%(ext)s";
        _settings.Format = GetFormat();

        return await ytdl.RunVideoDownload($"https://www.youtube.com/watch?v={video.GetResult().SourceId}", overrideOptions: _settings);
    }

    public string GetFormat()
    {
        var formatString = "";
        if (_settingsData.YTDLFormat == "")
        {
            switch (_settingsData.Resolution)
            {
                case Resolution.SD:
                    formatString = "bestvideo[width<=576]+bestaudio/best[width<=576]";
                    break;
                case Resolution.HD:
                    formatString = "bestvideo[width<=1280]+bestaudio/best[width<=1280]";
                    break;
                case Resolution.FHD:
                    formatString = "bestvideo[width<=1920]+bestaudio/best[width<=1920]";
                    break;
                case Resolution.WQHD:
                    formatString = "bestvideo[width<=2560]+bestaudio/best[width<=2560]";
                    break;
                case Resolution.UHD:
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
        if (result.Success) return result.Data;

        return null;
    }
}