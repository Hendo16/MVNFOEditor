using System;
using System.Linq;
using Avalonia.Media.Imaging;
using Newtonsoft.Json.Linq;
using YoutubeDLSharp.Metadata;
using YtMusicNet.Models;

namespace MVNFOEditor.Models;

public class VideoResult : Result
{
    
    public VideoResult(string name, string year, string artUrl, string id, string vidPath, Artist currArt, Album? currAlb = null, VideoData? vidData = null, SearchSource source = SearchSource.Manual) :
        base(name, artUrl, source, id)
    {
        Artist = currArt;
        Album = currAlb;
        Year = year;
        VidPath = vidPath;
        if (vidData != null)
        {
            Duration = vidData.Duration.ToString();
        }
    }
    
    public VideoResult(Track track, Artist artist) :
        base(track.Title, track.Thumbnails.Last().URL, SearchSource.YouTubeMusic, track.VideoId)
    {
        Artist = artist;
        Year = track.Year;
        IsExplicit = track.IsExplicit;
        Duration = track.Duration.ToString();
    }

    //Apple Music
    //Use trackCensoredName instead of trackName, because only trackCensoredName seems to contain the ambiguations [(Live), (Directors Cut), etc.]
    public VideoResult(JToken track, Artist artist, string artUrl) :
        base((string)track["trackCensoredName"], artUrl, SearchSource.AppleMusic, (string)track["trackId"])
    {
        Artist = artist;
        Year = DateTime.Parse((string)track["releaseDate"]).Year.ToString();
        IsExplicit = (string)track["trackExplicitness"] == "explicit";
        if (track["trackTimeMillis"] != null)
        {
            Duration = TimeSpan.FromMilliseconds((double)track["trackTimeMillis"])
                .ToString(@"mm\:ss");
        }
    }

    public Artist Artist { get; set; }
    public Album? Album { get; set; }
    public string VidPath { get; set; }
    public string? Year { get; set; }
    public bool? IsExplicit { get; set; }
    public string? Duration { get; set; }
    public string? TopRes { get; set; }
    public string VideoURL { get; set; }
    public event EventHandler RemoveCallback;
}