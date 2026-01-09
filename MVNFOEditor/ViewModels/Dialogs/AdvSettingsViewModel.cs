using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Resolution = MVNFOEditor.Models.Resolution;

#pragma warning disable CS0657 // Not a valid attribute location for this declaration
namespace MVNFOEditor.ViewModels;

public partial class AdvSettingsViewModel : ObservableObject
{
    [ObservableProperty] [property: Category("Apple Music")] [property: DisplayName("Apple Music Token")]
    private string? _appleMusicToken;

    [ObservableProperty] [property: Category("Apple Music")] [property: DisplayName("Private Device ID")]
    private string? _deviceId;

    [ObservableProperty] [property: Category("Apple Music")] [property: DisplayName("Private Device Key")]
    private string? _deviceKey;

    [ObservableProperty] [property: Category("Local")] [property: DisplayName("FFMPEG Path")]
    private string? _ffmpegPath;

    [ObservableProperty] [property: Category("Download")] [property: DisplayName("Desired Resolution")]
    private Resolution _resolution;

    [ObservableProperty] [property: Category("Local")] [property: DisplayName("Screenshot Timestamp")]
    private int _screenshotSecond = 20;

    [ObservableProperty] [property: Category("YouTube Music")] [property: DisplayName("YTDL Format")]
    private string? _ytdlFormat;

    [ObservableProperty] [property: Category("YouTube Music")] [property: DisplayName("YT-DLP")]
    private string? _ytDLPath;

    [ObservableProperty] [property: Category("YouTube Music")] [property: DisplayName("Browser Headers File")]
    private string? _ytMusicAuthFile;
}