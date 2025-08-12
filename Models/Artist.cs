using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Models
{
    public class Artist
    {
        public Artist() { } // Parameterless constructor required by EF Core

        public Artist(ArtistMetadata metadata, YtMusicNet.Models.Artist ytArtist)
        {
            Description = ytArtist.Description;
            Name = ytArtist.Name;
            CardBannerURL = LargeBannerURL = ytArtist.Thumbnails.Last().URL;
            Metadata = new List<ArtistMetadata>() { metadata };
        }
        
        public int Id { get; set; }
        [MaxLength(255)]
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? CardBannerURL { get; set; }
        public string? LargeBannerURL { get; set; }
        public ICollection<ArtistMetadata> Metadata { get; set; }
        private string CachePath => $"./Cache/{Name}";
        
        private static HttpClient s_httpClient = new();

        public static async Task<Artist?> CreateArtist(ArtistResultCard resultCardInfo, SearchSource source)
        {
            Artist newArtist = new Artist();
            switch (source)
            {
                case SearchSource.AppleMusic:
                    string[] banners = App.GetiTunesHelper().GetArtistBannerLinks(resultCardInfo.artistLinkURL);
                    if (banners[0] != "")
                    {
                        newArtist.LargeBannerURL = banners[0];
                        newArtist.CardBannerURL = banners[1];
                    }
                    newArtist.Name = resultCardInfo.Name;
                    newArtist.Metadata = new List<ArtistMetadata>() { new (source, resultCardInfo.browseId) };
                    break;
                case SearchSource.YouTubeMusic:
                    YtMusicNet.Models.Artist? fullArtistInfo = await App.GetYTMusicHelper().GetArtist(resultCardInfo.browseId);
                    if (fullArtistInfo == null)
                    {
                        return null;
                    }
                    newArtist = new Artist(new ArtistMetadata(fullArtistInfo), fullArtistInfo);
                    break;
            }
            return newArtist;
        }

        public ArtistMetadata GetArtistMetadata(SearchSource? source = null)
        {
            return source != null ? Metadata.First(am => am.SourceId == source) : Metadata.First();
        }

        public async Task<List<AlbumResult>?> GetAlbums(SearchSource? source = null)
        {
            ArtistMetadata matchingMetadata = source != null ? Metadata.First(am => am.SourceId == source) : Metadata.First();
            switch (source)
            {
                case SearchSource.YouTubeMusic:
                    return await App.GetYTMusicHelper().GetAlbums(matchingMetadata.BrowseId, this);
                default:
                    return null;
            }
        }

        public async Task<List<VideoResult>?> GetVideos(SearchSource? source = null)
        {
            ArtistMetadata matchingMetadata = source != null ? Metadata.First(am => am.SourceId == source) : Metadata.First();
            switch (source)
            {
                case SearchSource.YouTubeMusic:
                    return await App.GetYTMusicHelper().GetVideosFromArtistId(matchingMetadata.YTVideoId, this);
                default:
                    return null;
            }
        }
        
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
                return File.OpenRead("./Assets/defaultBanner.png");
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
                return File.OpenRead("./Assets/defaultBanner.png");
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
                    return File.OpenRead("./Assets/defaultBanner.png");
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
                return File.OpenRead("./Assets/defaultBanner.png");
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
            //Sometimes banner already exists on disk - usually when single album was deleted and the artist was re-added
            if (File.Exists(CachePath + "/cardBanner.jpg"))
            {
                File.Delete(CachePath + "/cardBanner.jpg");
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
