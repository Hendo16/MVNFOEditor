using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MVNFOEditor.Models;

public class Album
{
    public Album()
    {
    } // Parameterless constructor required by EF Core

    public Album(Artist _artist)
    {
        Artist = _artist;
    }

    public int Id { get; set; }

    [MaxLength(255)] public string Title { get; set; }

    [MaxLength(4)] public string Year { get; set; }

    [MaxLength(255)] public string? ArtURL { get; set; }

    [MaxLength(255)] public string? AlbumBrowseID { get; set; }

    public List<AlbumMetadata> Metadata { get; set; }
    public bool? IsExplicit { get; set; }
    public Artist Artist { get; set; }
    private string CachePath => $"./Cache/{Artist.Name}";

    public void AddMetadata(AlbumMetadata metadata)
    {
        if (Metadata == null)
        {
            Metadata = new List<AlbumMetadata>();
            Title = metadata.OriginalTitle;
            Year = metadata.Year.ToString();
            ArtURL = metadata.ArtworkUrl;
            AlbumBrowseID = metadata.BrowseId;
        }

        Metadata.Add(metadata);
    }

    public List<SearchSource> GetSources()
    {
        return Metadata.Select(am => am.SourceId).ToList();
    }

    public AlbumMetadata GetAlbumMetadata(SearchSource? source = null)
    {
        return source != null ? Metadata.First(am => am.SourceId == source) : Metadata.First();
    }

    public static async Task<Album?> CreateAlbum(AlbumResult resultCardInfo, SearchSource source = SearchSource.YouTubeMusic)
    {
        var newAlbum = new Album(resultCardInfo.Artist);
        switch (source)
        {
            case SearchSource.AppleMusic:
                newAlbum.AddMetadata(new AlbumMetadata(resultCardInfo, newAlbum));
                break;
            case SearchSource.YouTubeMusic:
                var fullAlbumInfo = await App.GetYTMusicHelper().GetAlbum(resultCardInfo.SourceId);
                if (fullAlbumInfo == null) return null;
                newAlbum.AddMetadata(new AlbumMetadata(fullAlbumInfo, newAlbum, resultCardInfo.SourceId));
                break;
        }
        App.GetDBContext().Album.Add(newAlbum);
        await App.GetDBContext().SaveChangesAsync();
        return newAlbum;
    }

    public bool IsArtSaved()
    {
        return File.Exists(CachePath + $"/{CleansedTitle()}.jpg");
    }

    public async Task<Stream> LoadCoverBitmapAsync()
    {
        if (File.Exists(CachePath + $"/{CleansedTitle()}.jpg"))
            return File.OpenRead(CachePath + $"/{CleansedTitle()}.jpg");

        if (ArtURL != null)
        {
            var data = await App.GetHttpClient().GetByteArrayAsync(ArtURL);
            return new MemoryStream(data);
        }

        return File.OpenRead("./Assets/tmbte-FULL.jpg");
    }

    public void SaveManualCover(string path)
    {
        if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
        if (File.Exists(CachePath + $"/{CleansedTitle()}.jpg")) File.Delete(CachePath + $"/{CleansedTitle()}.jpg");
        File.Copy(path, CachePath + $"/{CleansedTitle()}.jpg");
    }

    public Stream SaveCoverBitmapStream()
    {
        if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
        return File.OpenWrite(CachePath + $"/{CleansedTitle()}.jpg");
    }

    private static async Task SaveToStreamAsync(Album data, Stream stream)
    {
        await JsonSerializer.SerializeAsync(stream, data).ConfigureAwait(false);
    }

    public static async Task<Album> LoadFromStream(Stream stream)
    {
        return (await JsonSerializer.DeserializeAsync<Album>(stream).ConfigureAwait(false))!;
    }

    public static async Task<IEnumerable<Album>> LoadCachedAsync()
    {
        if (!Directory.Exists("./Cache")) Directory.CreateDirectory("./Cache");

        var results = new List<Album>();

        foreach (var file in Directory.EnumerateFiles("./Cache"))
        {
            if (!string.IsNullOrWhiteSpace(new DirectoryInfo(file).Extension)) continue;

            await using var fs = File.OpenRead(file);
            results.Add(await LoadFromStream(fs).ConfigureAwait(false));
        }

        return results;
    }

    private string CleansedTitle()
    {
        return Title
            .Replace(":", "-")
            .Replace("/", "-")
            .Replace("?", "");
    }
}