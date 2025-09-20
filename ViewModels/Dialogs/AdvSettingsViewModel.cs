using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using M3U8Parser.Attributes;
using Resolution = MVNFOEditor.Models.Resolution;

namespace MVNFOEditor.ViewModels;

public partial class AdvSettingsViewModel : ObservableObject
{
    [ObservableProperty] [property: Category("Download"), DisplayName("Desired Resolution")]
    private Resolution _resolution;
    
    [ObservableProperty] [property: Category("Apple Music"), DisplayName("Apple Music Token")]
    private string? _appleMusicToken;
    
    [ObservableProperty] [property: Category("Apple Music"), DisplayName("Private Device ID")]
    private string? _deviceId;
    
    [ObservableProperty] [property: Category("Apple Music"), DisplayName("Private Device Key")]
    private string? _deviceKey;
    
    [ObservableProperty] [property: Category("YouTube Music"), DisplayName("YTDL Format")]
    private string? _ytdlFormat;
    
    [ObservableProperty] [property: Category("Local"), DisplayName("Screenshot Timestamp")]
    private int _screenshotSecond = 20;
    
    [ObservableProperty] [property: Category("YouTube Music"), DisplayName("YT-DLP")]
    private string? _ytDLPath;
    
    [ObservableProperty] [property: Category("Local"), DisplayName("FFMPEG Path")]
    private string? _ffmpegPath;
}