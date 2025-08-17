using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Interface;
using MVNFOEditor.ViewModels;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Models;

public class ArtistMetadata : IMetadata
{
    public ArtistMetadata() { } // Parameterless constructor required by EF Core

    public ArtistMetadata(SearchSource source, string browse, string? artworkUrl)
    {
        SourceId = source;
        BrowseId = browse;
        ArtworkUrl = artworkUrl;
    }
    public ArtistMetadata(YtMusicNet.Models.Artist ytArtist)
    {
        ArtworkUrl = ytArtist.Thumbnails.Last().URL;
        BrowseId = ytArtist.ChannelId;
        SourceId = SearchSource.YouTubeMusic;
        YTAlbumBrowseId = ytArtist.Albums.BrowseId;
        YTAlbumParams = ytArtist.Albums.Params;
        YTVideoId = ytArtist.Videos.BrowseId;
    }

    public int Id { get; set; }
    public int ArtistId { get; set; }
    public Artist Artist { get; set; }
    public SearchSource SourceId { get; set; }
    
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
    [MaxLength(50)]
    public string BrowseId { get; set; }
    
    [MaxLength(255)]
    public string? ArtworkUrl { get; set; }
    
    [MaxLength(100)]
    public string? YTVideoId { get; set; }
    
    [MaxLength(100)]
    public string? YTAlbumBrowseId { get; set; }
    
    [MaxLength(100)]
    public string? YTAlbumParams { get; set; }

    public void GetBrowseData()
    {
        throw new System.NotImplementedException();
    }

    public SearchSource GetSearchSource()
    {
        return SourceId;
    }

    public string GetArtwork()
    {
        throw new System.NotImplementedException();
    }
}