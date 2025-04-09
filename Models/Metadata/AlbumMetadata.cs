using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Models;

public class AlbumMetadata
{
    public AlbumMetadata() { } // Parameterless constructor required by EF Core

    public AlbumMetadata(SearchSource source, string browse)
    {
        SourceId = source;
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
}