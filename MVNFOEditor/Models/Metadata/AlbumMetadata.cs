using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using MVNFOEditor.Interface;

namespace MVNFOEditor.Models;

public class AlbumMetadata : IMetadata
{
    private SearchSource _sourceId;

    public AlbumMetadata()
    {
    } // Parameterless constructor required by EF Core

    public AlbumMetadata(AlbumResult resultCard, Album album)
    {
        Album = album;
        SourceId = resultCard.Source;
        BrowseId = resultCard.SourceId;
        ArtworkUrl = resultCard.ArtUrl;
        OriginalTitle = resultCard.Name;
        IsExplicit = resultCard.IsExplicit;
        if (int.TryParse(resultCard.Year, out var year)) Year = year;
    }

    public AlbumMetadata(YtMusicNet.Models.Album ytAlbum, Album album, string browseId)
    {
        SourceId = SearchSource.YouTubeMusic;
        BrowseId = browseId;
        Description = ytAlbum.Description;
        DurationTime = ytAlbum.DurationTime;
        Year = ytAlbum.Year;
        TrackCount = ytAlbum.TrackCount;
        OriginalTitle = ytAlbum.Title;
        IsExplicit = ytAlbum.IsExplicit;
        Album = album;
        if (ytAlbum.Thumbnails != null) ArtworkUrl = ytAlbum.Thumbnails.Last().URL;
    }

    public int Id { get; set; }
    public int AlbumId { get; set; }
    public Album Album { get; set; } = null!;
    public SearchSource SourceId { get; set; }

    [MaxLength(50)] public string BrowseId { get; set; }

    [MaxLength(255)] public string ArtworkUrl { get; set; }

    public string? Description { get; set; }

    public TimeSpan? DurationTime { get; set; }

    public int? Year { get; set; }

    public int? TrackCount { get; set; }

    [MaxLength(255)] public string? OriginalTitle { get; set; }

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
                case SearchSource.Manual:
                    return "./Assets/manual-48x48.png";
                default:
                    return "";
            }
        }
    }

    public void GetBrowseData()
    {
        throw new NotImplementedException();
    }

    public SearchSource GetSearchSource()
    {
        return _sourceId;
    }

    public string GetArtwork()
    {
        throw new NotImplementedException();
    }

    public static async Task<AlbumMetadata?> GetNewMetadata(AlbumResult card, Album album)
    {
        switch (card.Source)
        {
            case SearchSource.YouTubeMusic:
                var fullAlbumInfo = await App.GetYTMusicHelper().GetAlbum(card.SourceId);
                if (fullAlbumInfo == null) return null;
                return new AlbumMetadata(fullAlbumInfo, album, card.SourceId);
            case SearchSource.AppleMusic:
                return new AlbumMetadata(card, album);
        }

        return null;
    }
}