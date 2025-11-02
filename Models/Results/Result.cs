using System.IO;
using System.Threading.Tasks;

namespace MVNFOEditor.Models;

public abstract class Result(string name, string url, SearchSource source, string sourceId)
{
    public string Name { get; set; } = name;
    public string ArtUrl { get; set; } = url;
    public SearchSource Source { get; set; } = source;

    public string SourceId { get; set; } = sourceId;

    public async Task<Stream> LoadCoverBitmapAsync()
    {
        if (ArtUrl != "")
        {
            var data = await App.GetHttpClient().GetByteArrayAsync(ArtUrl);
            return new MemoryStream(data);
        }

        return File.OpenRead("./Assets/sddefault.jpg");
    }

    public Stream SaveThumbnailBitmapStream(string folderPath)
    {
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        return File.OpenWrite(folderPath + $"/{Name}.jpg");
    }
}