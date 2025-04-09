using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Models
{
    public class Album
    {
        public int Id { get; set; }
        [MaxLength(255)]
        public string Title { get; set; }
        [MaxLength(4)]
        public string Year { get; set; }
        [MaxLength(255)]
        public string? ArtURL { get; set; }
        [MaxLength(255)]
        public string? AlbumBrowseID { get; set; }
        
        public AlbumMetadata Metadata { get; set; }
        public Artist Artist { get; set; }
        public Album() { } // Parameterless constructor required by EF Core

        public Album(AlbumResult result)
        {
            Title = result.Title;
            Year = result.Year;
            ArtURL = result.thumbURL;
            AlbumBrowseID = result.browseId;
            Artist = result.Artist;
        }
        private static HttpClient s_httpClient = new();
        private string CachePath => $"./Cache/{Artist.Name}";

        public bool IsArtSaved() { return File.Exists(CachePath + $"/{CleansedTitle()}.jpg"); }
        
        public async Task<Stream> LoadCoverBitmapAsync()
        {
            if (File.Exists(CachePath + $"/{CleansedTitle()}.jpg"))
            {
                return File.OpenRead(CachePath + $"/{CleansedTitle()}.jpg");
            }
            else if (ArtURL != null)
            {
                var data = await s_httpClient.GetByteArrayAsync(ArtURL);
                return new MemoryStream(data);
            }
            else
            {
                return File.OpenRead("./Assets/tmbte-FULL.jpg");
            }
        }

        public void SaveManualCover(string path)
        {
            if (!Directory.Exists(CachePath))
            {
                Directory.CreateDirectory(CachePath);
            }
            if(File.Exists(CachePath + $"/{CleansedTitle()}.jpg"))
            {
                File.Delete(CachePath + $"/{CleansedTitle()}.jpg");
            }
            File.Copy(path, CachePath + $"/{CleansedTitle()}.jpg");
        }

        public Stream SaveCoverBitmapStream()
        {
            if (!Directory.Exists(CachePath))
            {
                Directory.CreateDirectory(CachePath);
            }
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
            if (!Directory.Exists("./Cache"))
            {
                Directory.CreateDirectory("./Cache");
            }

            var results = new List<Album>();

            foreach (var file in Directory.EnumerateFiles("./Cache"))
            {
                if (!string.IsNullOrWhiteSpace(new DirectoryInfo(file).Extension)) continue;

                await using var fs = File.OpenRead(file);
                results.Add(await Album.LoadFromStream(fs).ConfigureAwait(false));
            }

            return results;
        }

        private string CleansedTitle()
        {
            return Title
                .Replace(":","-")
                .Replace("/","-")
                .Replace("?", "");
        }
    }
}