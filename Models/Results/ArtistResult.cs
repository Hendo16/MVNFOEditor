using System.Linq;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Models;

public class ArtistResult : Result
{
    public ArtistResult(YtMusicNet.Records.ArtistResult result) :
        base(result.Title, result.Thumbnails.Last().URL, SearchSource.YouTubeMusic, result.Id)
    {
    }
    public ArtistResult(JObject result, string artUrl) :
        base(result["artistName"].ToString(), artUrl, SearchSource.AppleMusic, result["artistId"].ToString())
    {
    }
}