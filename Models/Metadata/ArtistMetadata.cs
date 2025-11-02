using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using MVNFOEditor.Interface;

namespace MVNFOEditor.Models;

public class ArtistMetadata : IMetadata
{
    public ArtistMetadata()
    {
    } // Parameterless constructor required by EF Core

    //Apple Music
    public ArtistMetadata(ArtistResult result, Artist artist)
    {
        SourceId = SearchSource.AppleMusic;
        BrowseId = result.SourceId;
        OriginalTitle = result.Name;
        ArtworkUrl = result.ArtUrl;
        Artist = artist;
    }

    //YouTube Music
    //Separate constructor with full 'Artist' model due to additional parameters needed above the ArtistResult card
    public ArtistMetadata(YtMusicNet.Models.Artist ytArtist, Artist artist)
    {
        SourceId = SearchSource.YouTubeMusic;
        ArtworkUrl = ytArtist.Thumbnails?.Last().URL;
        BrowseId = ytArtist.ChannelId ?? throw new InvalidOperationException();
        YtAlbumBrowseId = ytArtist.Albums?.BrowseId;
        YtVideoId = ytArtist.Videos?.BrowseId;
        YtAlbumParams = ytArtist.Albums?.Params;
        OriginalTitle = ytArtist.Name;
        Artist = artist;
    }

    public int Id { get; init; }
    public int ArtistId { get; init; }
    public Artist Artist { get; init; }
    public SearchSource SourceId { get; init; }

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

    [MaxLength(50)] public string BrowseId { get; init; }

    [MaxLength(255)] public string? ArtworkUrl { get; init; }

    [MaxLength(100)] public string? OriginalTitle { get; init; }

    public string? Description { get; init; }

    [MaxLength(100)] public string? YtVideoId { get; init; }

    [MaxLength(100)] public string? YtAlbumBrowseId { get; set; }

    [MaxLength(100)] public string? YtAlbumParams { get; set; }

    public void GetBrowseData()
    {
        throw new NotImplementedException();
    }

    public SearchSource GetSearchSource()
    {
        return SourceId;
    }

    public string GetArtwork()
    {
        throw new NotImplementedException();
    }

    public static async Task<ArtistMetadata?> GetNewMetadata(ArtistResult card, Artist artist)
    {
        switch (card.Source)
        {
            case SearchSource.YouTubeMusic:
                var fullArtistInfo = await App.GetYTMusicHelper().GetArtist(card.SourceId);
                if (fullArtistInfo == null) return null;
                return new ArtistMetadata(fullArtistInfo, artist);
            case SearchSource.AppleMusic:
                return new ArtistMetadata(card, artist);
        }

        return null;
    }
}