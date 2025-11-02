using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using YtMusicNet.Models;

namespace MVNFOEditor.Models;

public class VideoResult : Result
{
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
    public string? Year { get; set; }
    public bool? IsExplicit { get; set; }
    public string? Duration { get; set; }
    public string? TopRes { get; set; }
    public string VideoURL { get; set; }
}