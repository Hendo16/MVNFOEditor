using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using MVNFOEditor.Interface;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MVNFOEditor.Models;

public class AlbumMetadata : IMetadata
{
    private SearchSource _sourceId;
    public AlbumMetadata() { } // Parameterless constructor required by EF Core

    public AlbumMetadata(AlbumResult resultCard, Album _album)
    {
        SourceId = SearchSource.AppleMusic;
        BrowseId = resultCard.browseId;
        ArtworkUrl = resultCard.thumbURL;
        OriginalTitle = resultCard.Title;
        Album = _album;
    }
    public AlbumMetadata(YtMusicNet.Models.Album ytAlbum, Album _album, string browseId)
    {
        SourceId = SearchSource.YouTubeMusic;
        ArtworkUrl = ytAlbum.Thumbnails.Last().URL;
        BrowseId = browseId;
        Description = ytAlbum.Description;
        DurationTime =  ytAlbum.DurationTime;
        Year =  ytAlbum.Year;
        TrackCount = ytAlbum.TrackCount;
        OriginalTitle = ytAlbum.Title;
        IsExplicit = ytAlbum.IsExplicit;
        Album = _album;
    }
    
    public int Id { get; set; }
    public int AlbumId { get; set; }
    public Album Album { get; set; } = null!;
    public SearchSource SourceId { get; set; }
    [MaxLength(50)]
    public string BrowseId { get; set; }
    [MaxLength(255)]
    public string ArtworkUrl { get; set; }
    public string? Description { get; set; }

    public TimeSpan? DurationTime { get; set; }

    public int? Year { get; set; }

    public int? TrackCount { get; set; }
    [MaxLength(255)]

    public string? OriginalTitle { get; set; }

    public bool? IsExplicit { get; set; }

    [NotMapped]
    public string SourceIconPath
    {
        get
        {
            switch (SourceId)
            {
                case SearchSource.AppleMusic:
                    return "./Assets/am-48x48.png";
                case SearchSource.YouTubeMusic:
                    return "./Assets/ytm-48x48.png";
                default:
                    return "";
            }
        }
        
    }
    public void GetBrowseData()
    {
        throw new System.NotImplementedException();
    }

    public SearchSource GetSearchSource()
    {
        return _sourceId;
    }

    public string GetArtwork()
    {
        throw new System.NotImplementedException();
    }
}