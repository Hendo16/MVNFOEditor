using System.ComponentModel.DataAnnotations;
using MVNFOEditor.Interface;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Models;

public class AlbumMetadata : IMetadata
{
    private SearchSource _sourceId;
    public AlbumMetadata() { } // Parameterless constructor required by EF Core

    public AlbumMetadata(SearchSource source, string browse)
    {
        _sourceId = source;
        BrowseId = browse;
    }
    
    public int Id { get; set; }
    public int AlbumId { get; set; }
    public Album Album { get; set; } = null!;
    public SearchSource SourceId { get; set; }
    [MaxLength(50)]
    public string BrowseId { get; set; }
    [MaxLength(255)]
    public string ArtworkUrl { get; set; }

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