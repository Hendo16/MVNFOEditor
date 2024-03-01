using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Models
{
    public class Artist
    {
        public int Id { get; set; }
        public string? YTMusicId { get; set; }
        public string? CardBannerURL { get; set; }
        public string? LargeBannerURL { get; set; }
        public string Name { get; set; }
        public JArray? YTMusicAlbumResults { get; set; }
        public Artist() { } // Parameterless constructor required by EF Core
        private static HttpClient s_httpClient = new();
        private string CachePath => $"./Cache/{Name}";

        public bool IsCardSaved(){return File.Exists(CachePath + "/cardBanner.jpg");}
        public bool IsBannerSaved(){return File.Exists(CachePath + "/largeBanner.jpg");}
        public async Task<Stream> LoadCardBannerBitmapAsync()
        {
            if (File.Exists(CachePath + "/cardBanner.jpg"))
            {
                return File.OpenRead(CachePath + "/cardBanner.jpg");
            }
            else if (CardBannerURL != "")
            {
                var data = await s_httpClient.GetByteArrayAsync(CardBannerURL);
                return new MemoryStream(data);
            }
            else
            {
                return File.OpenRead("./Assets/defaultBanner.jpg");
            }
        }
        public async Task<Stream> LoadLocalCardBannerBitmapAsync()
        {
            if (File.Exists(CachePath + "/cardBanner.jpg"))
            {
                return File.OpenRead(CachePath + "/cardBanner.jpg");
            }
            else
            {
                return File.OpenRead("./Assets/defaultBanner.jpg");
            }
        }

        public async Task<Stream> LoadLargeBannerBitmapAsync()
        {
            if (File.Exists(CachePath + "/largeBanner.jpg"))
            {
                return File.OpenRead(CachePath + "/largeBanner.jpg");
            }
            else
            {
                if (LargeBannerURL != "")
                {
                    var data = await s_httpClient.GetByteArrayAsync(LargeBannerURL);
                    return new MemoryStream(data);
                }
                //Default to smaller image if Large doesn't exist
                else if (CardBannerURL != "")
                {
                    var data = await s_httpClient.GetByteArrayAsync(CardBannerURL);
                    return new MemoryStream(data);
                }
                //If no banner is found, return default banner
                else
                {
                    return File.OpenRead("./Assets/defaultLargeBanner.jpg");
                }
            }
        }

        public async Task<Stream> LoadLocalLargeBannerBitmapAsync()
        {
            if (File.Exists(CachePath + "/largeBanner.jpg"))
            {
                return File.OpenRead(CachePath + "/largeBanner.jpg");
            }
            else if (File.Exists(CachePath + "/cardBanner.jpg"))
            {
                return File.OpenRead(CachePath + "/cardBanner.jpg");
            }
            else
            {
                return File.OpenRead("./Assets/defaultLargeBanner.jpg");
            }
        }

        public async Task SaveAsync()
        {
            if (!Directory.Exists("./Cache"))
            {
                Directory.CreateDirectory("./Cache");
            }

            using (var fs = File.OpenWrite(CachePath))
            {
                await SaveToStreamAsync(this, fs);
            }
        }

        public void SaveManualBanner(string path)
        {
            if (!Directory.Exists(CachePath))
            {
                Directory.CreateDirectory(CachePath);
            }
            File.Copy(path, CachePath + "/cardBanner.jpg");
        }

        public Stream SaveCardBannerBitmapStream()
        {
            if (!Directory.Exists(CachePath))
            {
                Directory.CreateDirectory(CachePath);
            }
            return File.OpenWrite(CachePath + "/cardBanner.jpg");
        }

        public Stream SaveLargeBannerBitmapStream()
        {
            if (!Directory.Exists(CachePath))
            {
                Directory.CreateDirectory(CachePath);
            }
            return File.OpenWrite(CachePath + "/largeBanner.jpg");
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
            if (!Directory.Exists("./Cache"))
            {
                Directory.CreateDirectory("./Cache");
            }

            var results = new List<Artist>();

            foreach (var file in Directory.EnumerateFiles("./Cache"))
            {
                if (!string.IsNullOrWhiteSpace(new DirectoryInfo(file).Extension)) continue;

                await using var fs = File.OpenRead(file);
                results.Add(await Artist.LoadFromStream(fs).ConfigureAwait(false));
            }

            return results;
        }
    }
}
