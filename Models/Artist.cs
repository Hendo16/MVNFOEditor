using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace MVNFOEditor.Models;

public class Artist
{
    public int Id { get; set; }

    [MaxLength(255)] public string Name { get; set; }

    public string? Description { get; set; }
    public string? CardBannerURL { get; set; }
    public string? LargeBannerURL { get; set; }
    public ICollection<ArtistMetadata> Metadata { get; set; }

    private string CachePath => $"./Cache/{Name}";
    // Parameterless constructor required by EF Core

    public void AddMetadata(ArtistMetadata metadata)
    {
        if (Metadata == null)
        {
            Metadata = new List<ArtistMetadata>();
            //First metadata should be used to set the default values for the Artist
            Description = metadata.Description;
            Name = metadata.OriginalTitle;
            CardBannerURL = LargeBannerURL = metadata.ArtworkUrl;
        }

        Metadata.Add(metadata);
    }


    public static async Task<Artist> CreateArtist(ArtistResult resultInfo)
    {
        var newArtist = new Artist();
        switch (resultInfo.Source)
        {
            case SearchSource.AppleMusic:
                /*
                string[] banners = App.GetiTunesHelper().GetArtistBannerLinks(resultInfo.artistLinkURL);
                if (banners[0] != "")
                {
                    newArtist.LargeBannerURL = banners[0];
                    newArtist.CardBannerURL = banners[1];
                }
                */
                newArtist.Name = resultInfo.Name;
                newArtist.AddMetadata(new ArtistMetadata(resultInfo, newArtist));
                break;
            case SearchSource.YouTubeMusic:
                var fullArtistInfo = await App.GetYTMusicHelper().GetArtist(resultInfo.SourceId);
                if (fullArtistInfo == null) return null;
                newArtist.AddMetadata(new ArtistMetadata(fullArtistInfo, newArtist));
                break;
        }
        App.GetDBContext().Artist.Add(newArtist);
        await App.GetDBContext().SaveChangesAsync();
        return newArtist;
    }

    public ArtistMetadata GetArtistMetadata(SearchSource? source = null)
    {
        return source != null ? Metadata.First(am => am.SourceId == source) : Metadata.First();
    }

    public List<string> GetBannerURLs()
    {
        return Metadata.Select(am => am.ArtworkUrl).ToList();
    }

    public List<SearchSource> GetSources()
    {
        return Metadata.Select(am => am.SourceId).ToList();
    }

    public async Task<List<AlbumResult>?> GetAlbums(SearchSource? source = null)
    {
        var matchingMetadata = source != null ? Metadata.First(am => am.SourceId == source) : Metadata.First();
        switch (source)
        {
            case SearchSource.YouTubeMusic:
                return await App.GetYTMusicHelper().GetAlbums(matchingMetadata.BrowseId, this);
            case SearchSource.AppleMusic:
                return await App.GetiTunesHelper().GetAlbums(matchingMetadata.BrowseId, this);
            default:
                return null;
        }
    }

    public async Task<List<VideoResult>?> GetVideos(SearchSource? source = null)
    {
        var matchingMetadata = source != null ? Metadata.First(am => am.SourceId == source) : Metadata.First();
        switch (source)
        {
            case SearchSource.YouTubeMusic:
                return await App.GetYTMusicHelper()
                    .GetVideosFromArtistId(matchingMetadata.YtVideoId, matchingMetadata.BrowseId, this);
            case SearchSource.AppleMusic:
                return await App.GetiTunesHelper().GetVideosFromArtistId(matchingMetadata.BrowseId, this);
            default:
                return null;
        }
    }

    public bool IsCardSaved()
    {
        return File.Exists(CachePath + "/cardBanner.jpg");
    }

    public bool IsBannerSaved()
    {
        return File.Exists(CachePath + "/largeBanner.jpg");
    }

    public async Task<Stream> LoadCardBannerBitmapAsync()
    {
        if (File.Exists(CachePath + "/cardBanner.jpg")) return File.OpenRead(CachePath + "/cardBanner.jpg");
        if (CardBannerURL == "") return File.OpenRead("./Assets/defaultBanner.png");

        var data = await App.GetHttpClient().GetByteArrayAsync(CardBannerURL);
        Directory.CreateDirectory(CachePath);
        var openStream = File.OpenWrite(CachePath + "/cardBanner.jpg");
        openStream.Write(data, 0, data.Length);
        openStream.Close();
        return new MemoryStream(data);
    }

    public async Task<Stream> LoadLocalCardBannerBitmapAsync()
    {
        if (File.Exists(CachePath + "/cardBanner.jpg")) return File.OpenRead(CachePath + "/cardBanner.jpg");
        return File.OpenRead("./Assets/defaultBanner.png");
    }

    public async Task<Stream> LoadLargeBannerBitmapAsync()
    {
        if (File.Exists(CachePath + "/largeBanner.jpg")) return File.OpenRead(CachePath + "/largeBanner.jpg");

        if (LargeBannerURL != "")
        {
            var data = await App.GetHttpClient().GetByteArrayAsync(LargeBannerURL);
            var openStream = File.OpenWrite(CachePath + "/largeBanner.jpg");
            openStream.Write(data, 0, data.Length);
            openStream.Close();
            return new MemoryStream(data);
        }

        //Default to smaller image if Large doesn't exist
        if (CardBannerURL != "")
        {
            var data = await App.GetHttpClient().GetByteArrayAsync(CardBannerURL);
            return new MemoryStream(data);
        }

        //If no banner is found, return default banner
        return File.OpenRead("./Assets/defaultBanner.png");
    }

    public async Task<Stream> LoadLocalLargeBannerBitmapAsync()
    {
        if (File.Exists(CachePath + "/largeBanner.jpg")) return File.OpenRead(CachePath + "/largeBanner.jpg");

        if (File.Exists(CachePath + "/cardBanner.jpg")) return File.OpenRead(CachePath + "/cardBanner.jpg");

        return File.OpenRead("./Assets/defaultBanner.png");
    }

    public async Task SaveAsync()
    {
        if (!Directory.Exists("./Cache")) Directory.CreateDirectory("./Cache");

        using (var fs = File.OpenWrite(CachePath))
        {
            await SaveToStreamAsync(this, fs);
        }
    }

    public void SaveManualBanner(string path)
    {
        if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
        //Sometimes banner already exists on disk - usually when single album was deleted and the artist was re-added
        if (File.Exists(CachePath + "/cardBanner.jpg")) File.Delete(CachePath + "/cardBanner.jpg");
        File.Copy(path, CachePath + "/cardBanner.jpg");
    }

    public Stream SaveCardBannerBitmapStream()
    {
        if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
        return File.OpenWrite(CachePath + "/cardBanner.jpg");
    }

    public Stream SaveLargeBannerBitmapStream()
    {
        if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
        return File.OpenWrite(CachePath + "/largeBanner.jpg");
    }

    public void UpdateArtistBanner(Bitmap newBanner)
    {
        using var largeBannerStream = SaveLargeBannerBitmapStream();
        using var cardBannerStream = SaveCardBannerBitmapStream();
        newBanner.Save(cardBannerStream);
        newBanner.Save(largeBannerStream);
    }

    private static async Task SaveToStreamAsync(Artist data, Stream stream)
    {
        await JsonSerializer.SerializeAsync(stream, data).ConfigureAwait(false);
    }

    public static async Task<Artist> LoadFromStream(Stream stream)
    {
        return (await JsonSerializer.DeserializeAsync<Artist>(stream).ConfigureAwait(false))!;
    }

    public static async Task<IEnumerable<Artist>> LoadCachedAsync()
    {
        if (!Directory.Exists("./Cache")) Directory.CreateDirectory("./Cache");

        var results = new List<Artist>();

        foreach (var file in Directory.EnumerateFiles("./Cache"))
        {
            if (!string.IsNullOrWhiteSpace(new DirectoryInfo(file).Extension)) continue;

            await using var fs = File.OpenRead(file);
            results.Add(await LoadFromStream(fs).ConfigureAwait(false));
        }

        return results;
    }
}