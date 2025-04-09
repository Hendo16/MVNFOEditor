using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Models;

public class ArtistMetadata
{
    public ArtistMetadata() { } // Parameterless constructor required by EF Core

    public ArtistMetadata(SearchSource source, string browse, JArray albums)
    {
        SourceId = source;
        BrowseId = browse;
        AlbumResults = albums;
    }
    public int Id { get; set; }
    public int ArtistId { get; set; }
    public Artist Artist { get; set; }
    public SearchSource SourceId { get; set; }
    [MaxLength(50)]
    public string BrowseId { get; set; }
    
    [MaxLength(255)]
    public string ArtworkUrl { get; set; }
    public JArray AlbumResults { get; set; }
}