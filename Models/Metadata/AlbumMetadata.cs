using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
        Album = _album;
        SourceId = SearchSource.AppleMusic;
        BrowseId = resultCard.browseId;
        ArtworkUrl = resultCard.thumbURL;
        OriginalTitle = resultCard.Title;
        IsExplicit = resultCard.isExplicit;
        if (int.TryParse(resultCard.Year, out var year))
        {
            Year = year;
        }
    }
    public AlbumMetadata(YtMusicNet.Models.Album ytAlbum, Album album, string browseId)
    {
        SourceId = SearchSource.YouTubeMusic;
        if (ytAlbum.Thumbnails != null)
        {
            ArtworkUrl = ytAlbum.Thumbnails.Last().URL;
        }
        BrowseId = browseId;
        Description = ytAlbum.Description;
        DurationTime =  ytAlbum.DurationTime;
        Year =  ytAlbum.Year;
        TrackCount = ytAlbum.TrackCount;
        OriginalTitle = ytAlbum.Title;
        IsExplicit = ytAlbum.IsExplicit;
        Album = album;
    }
    public static async Task<AlbumMetadata?> GetNewMetadata(AlbumResult card, Album album)
    {
        switch (card.SearchSource)
        {
            case SearchSource.YouTubeMusic:
                YtMusicNet.Models.Album? fullAlbumInfo = await App.GetYTMusicHelper().GetAlbum(card.browseId);
                if (fullAlbumInfo == null)
                {
                    return null;
                }
                return new AlbumMetadata(fullAlbumInfo, album, card.browseId);
            case SearchSource.AppleMusic:
                //string[] banners = App.GetiTunesHelper().GetArtistBannerLinks(card.artistLinkURL);
                return new AlbumMetadata(card, album);
        }
        return null;
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