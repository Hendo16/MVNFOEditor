using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Models;

public class AlbumResult : Result
{
    public AlbumResult(YtMusicNet.Models.Album ytAlbum, Artist artist) :
        base(ytAlbum.Title, ytAlbum.Thumbnails.Last().URL, SearchSource.YouTubeMusic, ytAlbum.Id)
    {
        Artist = artist;
        Year = ytAlbum.Year.ToString();
        IsExplicit = ytAlbum.IsExplicit;
    }

    public AlbumResult(JToken album, Artist artist, string artUrl) :
        base(album["collectionName"].ToString(), artUrl, SearchSource.AppleMusic, album["collectionId"].ToString())
    {
        Artist = artist;
        Year = DateTime.Parse(album["releaseDate"].ToString()).Year.ToString();
        IsExplicit = album["collectionExplicitness"].ToString() == "Explicit";
    }

    public string? Year { get; set; }
    public bool? IsExplicit { get; set; }
    public Artist Artist { get; set; }
}