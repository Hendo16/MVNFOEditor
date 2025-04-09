using Avalonia.Styling;
using Config.Net;
using M3U8Parser.Attributes;
using SukiUI.Models;

namespace MVNFOEditor.Settings;

public interface ISettings
{
    [Option(Alias = "VideoFolder", DefaultValue = "n/a")]
    public string RootFolder { get; set; }
    
    #region AppleMusic
    [Option(Alias = "AM.AccessToken", DefaultValue = "n/a")]
    public string AM_MediaBearerToken { get; set; }
    [Option(Alias = "AM.UserToken", DefaultValue = "n/a")]
    public string AM_UserToken { get; set; }
    [Option(Alias = "AM.Region", DefaultValue = "n/a")]
    public string AM_Storefront { get; set; }
    [Option(Alias = "AM.Language", DefaultValue = "n/a")]
    public string AM_Language { get; set; }
    #endregion

    #region YTDL
        [Option(Alias = "YTDL.Format", DefaultValue = "n/a")]
        public string YTDLFormat { get; set; }
        [Option(Alias = "YTDL.Path", DefaultValue = "n/a")]
        public string YTDLPath { get; set; }
        [Option(Alias = "YTDL.Res", DefaultValue = "n/a")]
        public string YTDLResolution { get; set; }
    #endregion
    
    #region FFMPEG
    [Option(Alias = "FFMPEG.Path", DefaultValue = "n/a")]
    public string FFMPEGPath { get; set; }
    #endregion
    
    #region FFPROBE
    [Option(Alias = "FFPROBE.ScreenshotSeconds", DefaultValue = "20")]
    public int ScreenshotSecond { get; set; }
    [Option(Alias = "FFPROBE.Path", DefaultValue = "n/a")]
    public string FFPROBEPath { get; set; }
    #endregion

    #region SukiUI
        [Option(Alias = "SukiUI.Animated", DefaultValue = "true")]
        public bool AnimatedBackground { get; set; }
        [Option(Alias = "SukiUI.Transitions", DefaultValue = "true")]
        public bool BackgroundTransitions { get; set; }
        [Option(Alias = "SukiUI.LightMode", DefaultValue = "true")]
        public bool LightMode { get; set; }
    #endregion
}