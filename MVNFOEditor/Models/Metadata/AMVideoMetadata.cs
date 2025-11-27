using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Models;

public class AMVideoMetadata
{
    public AMVideoMetadata()
    {
    }

    public AMVideoMetadata(JToken videoData, int artId)
    {
        var attributes = (JObject)videoData["attributes"];
        var artwork = (JObject)attributes["artwork"];
        id = (int)videoData["id"];
        href = (string)videoData["href"];
        genre = (string)(JValue)attributes["genreNames"][0];
        duration = (int)attributes["durationInMillis"];
        releaseDate = (string)attributes["releaseDate"];
        isrc = (string)attributes["isrc"];
        has4K = (string)attributes["has4K"];
        hasHDR = (string)attributes["hasHDR"];
        name = (string)attributes["name"];
        playlistURL = "";
        artistId = artId;

        artworkObj = artwork.ToString(Formatting.None);
        previewObj = ((JObject)((JArray)attributes["previews"])[0]).ToString(Formatting.None);
        relationshipObj = ((JObject)videoData["relationships"]).ToString(Formatting.None);

        //Inferred values
        coverURL = ((string)artwork["url"]).Replace("{w}x{h}",
            $"{(string)artwork["width"]}x{(string)artwork["height"]}");
        coverBGColor = (string)artwork["bgColor"];
        coverTextColors =
        [
            (string)artwork["textColor1"], (string)artwork["textColor2"], (string)artwork["textColor3"],
            (string)artwork["textColor4"]
        ];
    }

    public int id { get; set; }
    public string href { get; set; }
    public string genre { get; set; }
    public int duration { get; set; }
    public string releaseDate { get; set; }
    public string isrc { get; set; }
    public string artworkObj { get; set; }
    public string has4K { get; set; }
    public string hasHDR { get; set; }
    public string name { get; set; }
    public string previewObj { get; set; }
    public string relationshipObj { get; set; }
    public string playlistURL { get; set; }
    public int artistId { get; set; }


    [NotMapped] public string coverURL { get; set; }

    [NotMapped] public string coverBGColor { get; set; }

    [NotMapped] public string[] coverTextColors { get; set; }
}