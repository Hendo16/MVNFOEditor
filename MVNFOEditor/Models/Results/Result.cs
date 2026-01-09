using System.IO;
using System.Threading.Tasks;
using MVNFOEditor.Helpers;

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
            var data = await NetworkHandler.GetFileData(ArtUrl);
            if (data != null)
            {
                return new MemoryStream(data);
            }
            ToastHelper.ShowError("Cover Error", "Couldn't fetch album artwork, please check logs");
        }

        return File.OpenRead("./Assets/defaultBanner.png");
    }

    public Stream SaveThumbnailBitmapStream(string folderPath, string source)
    {
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        return File.OpenWrite(folderPath + $"/{Name}-{source}.jpg");
    }
}