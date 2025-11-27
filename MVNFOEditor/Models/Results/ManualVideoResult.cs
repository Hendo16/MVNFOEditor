using System;
using Avalonia.Media.Imaging;
using YoutubeDLSharp.Metadata;

namespace MVNFOEditor.Models;

public class ManualVideoResult : Result
{
    public ManualVideoResult(string name, string year, string artUrl, string id, Artist currArt, Album? currAlb = null, VideoData? vidData = null) :
        base(name, artUrl, SearchSource.Manual, id)
    {
        Artist = currArt;
        Album = currAlb;
        Year = year;
        if (vidData != null)
        {
            Duration = vidData.Duration.ToString();
        }
    }

    public Artist Artist { get; set; }
    public Album? Album { get; set; }
    public string Year { get; set; }
    public VideoData? VidData { get; set; }
    public string? Duration { get; set; }
    public event EventHandler RemoveCallback;
}