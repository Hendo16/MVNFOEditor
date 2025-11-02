using Config.Net;
using Resolution = MVNFOEditor.Models.Resolution;

namespace MVNFOEditor.Settings;

public interface ISettings
{
    [Option(Alias = "VideoFolder", DefaultValue = "n/a")]
    public string RootFolder { get; set; }

    [Option(Alias = "Resolution", DefaultValue = "n/a")]
    public Resolution Resolution { get; set; }

    #region YouTubeMusic

    [Option(Alias = "YTM.AuthPath", DefaultValue = "N/A")]
    public string YTM_AuthHeaders { get; set; }

    #endregion

    #region FFMPEG

    [Option(Alias = "FFMPEG.Path", DefaultValue = "./Assets")]
    public string FFMPEGPath { get; set; }

    #endregion

    #region AppleMusic

    [Option(Alias = "AM.AccessToken", DefaultValue = "n/a")]
    public string AM_AccessToken { get; set; }

    [Option(Alias = "AM.UserToken", DefaultValue = "n/a")]
    public string AM_UserToken { get; set; }

    [Option(Alias = "AM.Region", DefaultValue = "n/a")]
    public string AM_Storefront { get; set; }

    [Option(Alias = "AM.Language", DefaultValue = "n/a")]
    public string AM_Language { get; set; }

    [Option(Alias = "AM.DeviceId", DefaultValue = "./Assets/device_client_id_blob")]
    public string AM_DeviceId { get; set; }

    [Option(Alias = "AM.DeviceKey", DefaultValue = "./Assets/device_private_key")]
    public string AM_DeviceKey { get; set; }

    #endregion

    #region YTDL

    [Option(Alias = "YTDL.Format", DefaultValue = "n/a")]
    public string YTDLFormat { get; set; }

    [Option(Alias = "YTDL.Path", DefaultValue = "./Assets")]
    public string YTDLPath { get; set; }

    #endregion

    #region FFPROBE

    [Option(Alias = "FFPROBE.ScreenshotSeconds", DefaultValue = "20")]
    public int ScreenshotSecond { get; set; }

    [Option(Alias = "FFPROBE.Path", DefaultValue = "./Assets")]
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