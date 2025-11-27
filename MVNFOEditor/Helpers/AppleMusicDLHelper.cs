using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Enums;
using Flurl;
using Flurl.Http;
using HtmlAgilityPack;
using log4net;
using M3U8Parser;
using M3U8Parser.Attributes.ValueType;
using MVNFOEditor.DB;
using MVNFOEditor.Models;
using MVNFOEditor.Settings;
using MVNFOEditor.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WVCore;

namespace MVNFOEditor.Helpers;

public enum AppleMusicDownloadResponse
{
    Success,
    Failure,
    InvalidUserToken,
    InvalidDeviceFiles
}

public class AppleMusicStreamList
{
    public string? Key { get; set; }
    public List<string>? Uris { get; set; }
    public string? BaseUri { get; set; }
    public decimal? AverageBitrate { get; set; }
    public string? Codec { get; set; }
    public string? Type { get; set; }
    public ResolutionType? Resolution { get; set; }
}

public class AppleMusicDataPayload
{
    [JsonPropertyName("user-initiated")] public bool UserInitiated { get; set; }

    [JsonPropertyName("key-system")] public string? KeySystem { get; set; }

    [JsonPropertyName("adamId")] public string? AdamId { get; set; }

    [JsonPropertyName("challenge")] public string? Challenge { get; set; }

    [JsonPropertyName("isLibrary")] public bool IsLibrary { get; set; }

    [JsonPropertyName("uri")] public string? Uri { get; set; }
}

public class AppleMusicDLHelper
{
    private const string WebplaybackUrl = "https://play.itunes.apple.com/WebObjects/MZPlay.woa/wa/webPlayback";

    private const string LicenseUrl =
        "https://play.itunes.apple.com/WebObjects/MZPlay.woa/wa/acquireWebPlaybackLicense";

    private static readonly ILog Log = LogManager.GetLogger(typeof(AppleMusicDLHelper));
    private static ISettings _settings;

    private static readonly object headers = new
    {
        Accept = "Application/json",
        Accept_Encoding = "application/json",
        Connection = "keep-alive",
        Content_Type = "application/json;charset=utf-8",
        Origin = "https://music.apple.com",
        Referer = "https://music.apple.com/",
        User_Agent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36"
    };

    private readonly MusicDbContext _db;
    private bool _isValidUserToken;

    private AppleMusicDLHelper()
    {
        _db = App.GetDBContext();
        _settings = App.GetSettings();
        GlobalFFOptions.Configure(new FFOptions
            { BinaryFolder = "./Assets", TemporaryFilesFolder = "/Cache/FFMPEG/tmp" });
    }

    private string BaseApiUrl()
    {
        return $"https://amp-api.music.apple.com/v1/catalog/{_settings.AM_Storefront}/music-videos/";
    }

    public static async Task<AppleMusicDLHelper> CreateHelper()
    {
        var initHelper = new AppleMusicDLHelper();
        initHelper.ValidAccessToken();
        //TODO: Need a way to validate this in settings, will only validate on init
        initHelper._isValidUserToken = await ValidUserToken();
        return initHelper;
    }

    public bool IsValidToken()
    {
        return _isValidUserToken;
    }

    public async Task<bool> UpdateUserToken(string newToken)
    {
        _settings.AM_UserToken = newToken;
        return await ValidUserToken();
    }

    private string GetAccessToken()
    {
        using (var client = new WebClient())
        {
            var mainPage = client.DownloadString($"https://music.apple.com/{_settings.AM_Storefront}/browse");

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(mainPage);

            var scriptNode =
                htmlDoc.DocumentNode.SelectSingleNode("//script[@type='module' and @crossorigin and @src]");
            if (scriptNode != null)
            {
                var scriptName = scriptNode.GetAttributeValue("src", "");

                var scriptUrl = $"https://music.apple.com{scriptName}";
                var scriptContent = client.DownloadString(scriptUrl);

                var match = Regex.Match(scriptContent, @"(?=eyJh)(.*?)(?="")");
                if (match.Success) return match.Groups[1].Value;

                Log.Error("Error in AppleMusicDLHelper->GetAccessToken: Couldn't find access token");
                Console.WriteLine("No matching token found.");
                return "";
            }

            Log.Error("Error in AppleMusicDLHelper->GetAccessToken: Script tag with required attributes not found");
            return "";
        }
    }

    public async void ValidAccessToken()
    {
        //First we make sure we have a valid region set prior to proceeding
        if (_settings.AM_Storefront == "n/a") _settings.AM_Storefront = "us";
        //Check if token has been set before
        if (_settings.AM_AccessToken == "n/a") _settings.AM_AccessToken = GetAccessToken();
        //Check if we can make a valid request with the token
        try
        {
            await BaseApiUrl()
                .AppendPathSegment("281899913")
                .WithHeaders(headers)
                .WithOAuthBearerToken(_settings.AM_AccessToken)
                .GetStringAsync();
        }
        catch (FlurlHttpException e)
        {
            //Invalid/Expired media access token
            _settings.AM_AccessToken = GetAccessToken();
        }
    }

    public static async Task<bool> ValidUserToken()
    {
        try
        {
            var response = await "https://amp-api.music.apple.com/v1/me/storefront"
                .WithHeaders(headers)
                .WithHeader("media-user-token", _settings.AM_UserToken)
                .WithOAuthBearerToken(_settings.AM_AccessToken)
                .GetStringAsync();

            //Get account region information
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            var attributes = root.GetProperty("data")[0].GetProperty("attributes");

            //string language = GetLanguage(attributes, _settings.AM_Language);
            _settings.AM_Language = attributes.GetProperty("defaultLanguageTag").GetString();
            _settings.AM_Storefront = root.GetProperty("data")[0].GetProperty("id").GetString();

            return true;
        }
        catch (FlurlHttpException e)
        {
            //Invalid user token
            Console.WriteLine("Error: Invalid AM token provided.");
            Log.Error("Invalid AM Token Provided");
            Log.Error(e);
            return false;
        }
    }

    public async Task<AppleMusicDownloadResponse> DownloadVideo(VideoResultViewModel videoResult,
        WaveProgressViewModel wavevm)
    {
        //Validate tokens
        wavevm.HeaderText = "Validating Apple Music Account Information...";
        if (!_isValidUserToken)
        {
            Log.Error("Error in AppleMusicDLHelper->DownloadVideo: Invalid user token");
            return AppleMusicDownloadResponse.InvalidUserToken;
        }

        if (!File.Exists(_settings.AM_DeviceId) ||
            !File.Exists(_settings.AM_DeviceKey))
            return AppleMusicDownloadResponse.InvalidDeviceFiles;

        var id = int.Parse(videoResult.VideoURL.Split('/')[^1]);
        //Get Video Metadata
        var data = _db.AppleMusicVideoMetadata.FirstOrDefault(am => am.id == id);
        wavevm.HeaderText = "Getting Video Metadata...";
        if (data == null)
        {
            data = await get_api(id, videoResult.Artist.Id);
            Log.InfoFormat("Storing keys for {0} in DB...", id);
            //Store video metadata for future use
            await _db.AppleMusicVideoMetadata.AddAsync(data);
            await _db.SaveChangesAsync();
        }

        wavevm.HeaderText = "Getting Encryption Keys...";
        //Get all available streams and encryption keys
        var (streams, keys) = await get_content(data);
        wavevm.HeaderText = "Selecting the highest quality...";
        //Select streams
        IEnumerable<AppleMusicStreamList> bestVideoStreams =
            streams.Where(s => s.Type == "video").OrderByDescending(s => s.AverageBitrate);
        IEnumerable<AppleMusicStreamList> bestAudioStreams =
            streams.Where(s => s.Type == "audio").OrderByDescending(s => s.AverageBitrate);
        Console.WriteLine("Available Resolutions:");
        Console.WriteLine("--------------------------------------------------------");
        for (var i = 0; i < bestVideoStreams.Count(); i++)
        {
            var vidStream = bestVideoStreams.ElementAt(i);
            var megabyte = (decimal)(vidStream.AverageBitrate / 1000000);
            var kilobyte = (decimal)(vidStream.AverageBitrate / 1000);
            var bitrateStr = megabyte > 0 ? $"{Math.Round(megabyte, 2)} Mb/s" : $"{Math.Round(kilobyte, 2)} Kb/s";
            Console.WriteLine(
                $"{i}: {vidStream.Resolution.Width}x{vidStream.Resolution.Height} at {bitrateStr} in {vidStream.Codec}");
        }

        Console.WriteLine("\n");
        for (var i = 0; i < bestAudioStreams.Count(); i++)
        {
            var audStream = bestAudioStreams.ElementAt(i);
            Console.WriteLine($"{i}: {audStream.AverageBitrate} Kb/s in {audStream.Codec}");
        }

        return await DownloadVideo(bestVideoStreams.First(), bestAudioStreams.First(), keys, videoResult, wavevm);
    }

    private async Task<AMVideoMetadata> get_api(int id, int artistId)
    {
        var result = await BaseApiUrl()
            .AppendPathSegment(id.ToString())
            .SetQueryParams(new
            {
                l = _settings.AM_Language
            })
            .WithHeaders(headers)
            .WithHeader("media-user-token", _settings.AM_UserToken)
            .WithOAuthBearerToken(_settings.AM_AccessToken)
            .GetStringAsync();
        var objResult = JsonConvert.DeserializeObject<JObject>(result,
            new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
        var loaded = new AMVideoMetadata(objResult.Value<JArray>("data")[0], artistId);
        return loaded;
    }

    private async Task<(List<AppleMusicStreamList>, List<PsshKey>)> get_content(AMVideoMetadata data)
    {
        var assetUrl = "";
        if (data.playlistURL == "")
        {
            var webplaybackitems = await get_webplayback(data.id);
            assetUrl = (string)webplaybackitems["hls-playlist-url"];
            //update playlist url
            data.playlistURL = assetUrl;
            _db.AppleMusicVideoMetadata.Update(data);
            await _db.SaveChangesAsync();
        }
        else
        {
            assetUrl = data.playlistURL;
        }

        var playlist = parseMusicVideoUri(assetUrl);
        var (streams, psshs) = await parseMVPlaylist(playlist);

        //Check pssh keys in DB
        var keys = new List<PsshKey>();
        if (_db.PsshKeys.Any(p => psshs.Contains(p.pssh)))
        {
            //get keys from db
            keys = _db.PsshKeys.Where(p => psshs.Contains(p.pssh)).ToList();
        }
        else
        {
            //if not, get new ones
            keys = await getMVKeys(psshs, data.id.ToString());
            //Save Keys in DB
            for (var i = 0; i < keys.Count; i++) await _db.PsshKeys.AddAsync(keys[i]);
            await _db.SaveChangesAsync();
        }

        return (streams, keys);
    }

    private async Task<List<PsshKey>> getMVKeys(HashSet<string> psshs, string id)
    {
        var keys = new List<PsshKey>();
        foreach (var pssh in psshs)
        {
            var decKey = await getMVKeys(pssh, id);
            var newKey = new PsshKey(pssh, decKey);
            keys.Add(newKey);
        }

        return keys;
    }

    private async Task<(List<AppleMusicStreamList>, HashSet<string>)> parseMVPlaylist(MasterPlaylist playlist)
    {
        var streams = new List<AppleMusicStreamList>();
        var psshs = new HashSet<string>();
        //Video Streams
        for (var i = 0; i < playlist.Streams.Count; i++)
        {
            var info = playlist.Streams[i];
            var encContent = await getMediaMetadata(info.Uri);
            encContent.Codec = info.Codecs.Contains("avc") ? "AVC" : "HEVC";
            encContent.AverageBitrate = info.AverageBandwidth;
            encContent.Resolution = info.Resolution;
            encContent.Type = "video";
            psshs.Add(encContent.Key);
            streams.Add(encContent);
        }

        //Audio Streams
        for (var i = 0; i < playlist.Medias.Count; i++)
        {
            var info = playlist.Medias[i];
            //For some reason a type comparison no longer works so string comparison is needed
            if (info.Type.ToString() != "AUDIO") continue;
            var encContent = await getMediaMetadata(info.Uri);
            encContent.Codec = info.GroupId.Contains("HE") ? "HE-AAC" : "AAC";
            encContent.AverageBitrate = decimal.Parse(info.GroupId.Split("-").Last());
            encContent.Type = "audio";
            streams.Add(encContent);
            psshs.Add(encContent.Key);
        }

        return (streams, psshs);
    }

    private async Task<long> GetTotalFileSize(List<string> uris, string baseUrl)
    {
        using var client = new HttpClient();
        long totalSize = 0;

        // Get total size
        foreach (var url in uris)
        {
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"{baseUrl}/{url}"));
            if (response.Content.Headers.ContentLength.HasValue)
                totalSize += response.Content.Headers.ContentLength.Value;
        }

        return totalSize;
    }

    private async Task<AppleMusicDownloadResponse> DownloadVideo(AppleMusicStreamList vidStream,
        AppleMusicStreamList audStream, List<PsshKey> keys, VideoResultViewModel videoResult,
        WaveProgressViewModel wavevm)
    {
        var enc_v = Path.GetFullPath("./Cache/enc_v.mp4");
        var dec_v = Path.GetFullPath("./Cache/dec_v.mp4");
        var enc_a = Path.GetFullPath("./Cache/enc_a.mp4");
        var dec_a = Path.GetFullPath("./Cache/dec_a.mp4");

        //Clear out cached files
        if (File.Exists(enc_v)) File.Delete(enc_v);
        if (File.Exists(dec_v)) File.Delete(dec_v);
        if (File.Exists(enc_a)) File.Delete(enc_a);
        if (File.Exists(dec_a)) File.Delete(dec_a);

        //Calculate file size
        var vidSize = await GetTotalFileSize(vidStream.Uris, vidStream.BaseUri);
        var audSize = await GetTotalFileSize(audStream.Uris, audStream.BaseUri);

        Console.WriteLine($"Video Size: {vidSize / 1024 / 1024} MB");
        Console.WriteLine($"Audio Size: {audSize / 1024 / 1024} MB");

        var totalSize = vidSize + audSize;
        Console.WriteLine($"Total Size: {totalSize / 1024 / 1024} MB");

        var decryptToolFileName = Path.GetFullPath("./Assets/mp4decrypt.exe");
        var decryptWorking = Path.GetDirectoryName(decryptToolFileName);
        wavevm.HeaderText = $"Downloading {videoResult.Title} {totalSize / 1024 / 1024} MB - ";
        await DownloadWithProgressAsync(vidStream.Uris, vidStream.BaseUri, enc_v, wavevm, totalSize);

        var decKey_v = keys.Where(vk => vk.pssh == vidStream.Key).First().key;
        var arg_v = "--key 1:" + decKey_v + " \"" + enc_v + "\" \"" + dec_v + "\"";

        var decryptv = new Process
        {
            StartInfo =
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Arguments = arg_v,
                FileName = decryptToolFileName,
                WorkingDirectory = decryptWorking
            }
        };

        decryptv.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
        decryptv.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);
        wavevm.HeaderText = $"Decrypting {videoResult.Title}...";
        decryptv.Start();
        decryptv.BeginOutputReadLine();
        decryptv.BeginErrorReadLine();
        decryptv.WaitForExit();

        Console.WriteLine($"Decrypt with this {decKey_v}");
        Console.WriteLine("-----------------------------------------------------------------");
        await DownloadWithProgressAsync(audStream.Uris, audStream.BaseUri, enc_a, wavevm, totalSize, vidSize);

        var decKey_a = keys.Where(vk => vk.pssh == audStream.Key).First().key;

        var decrypta = new Process
        {
            StartInfo =
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Arguments = "--key 1:" + decKey_a + " \"" + enc_a + "\" \"" + dec_a + "\"",
                FileName = decryptToolFileName,
                WorkingDirectory = decryptWorking
            }
        };

        decrypta.OutputDataReceived += (sender, e) =>
        {
            Console.WriteLine(e.Data);
            wavevm.UpdateProgress(0.5);
        };
        decrypta.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);
        decrypta.Start();
        decrypta.BeginOutputReadLine();
        decrypta.BeginErrorReadLine();
        decrypta.WaitForExit();
        Console.WriteLine($"Decrypt with this {decKey_a}");

        var muxed = Path.GetFullPath($"{_settings.RootFolder}/{videoResult.Artist.Name}/{videoResult.Title}.mp4");
        await FFMpegArguments
            .FromFileInput(dec_v)
            .AddFileInput(dec_a)
            .OutputToFile(muxed, true, options => options
                .CopyChannel(Channel.Video)
                .CopyChannel(Channel.Audio)
            )
            .ProcessAsynchronously();

        wavevm.HeaderText = "Creating final output...";

        //Clear out cached files
        File.Delete(enc_v);
        File.Delete(dec_v);
        File.Delete(enc_a);
        File.Delete(dec_a);
        return AppleMusicDownloadResponse.Success;
    }

    private static async Task DownloadWithProgressAsync(List<string>? urls, string baseUrl, string filename,
        WaveProgressViewModel wavevm, long totalSize, long alreadyDownloadedBytes = 0)
    {
        using var client = new HttpClient();

        using var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
        var downloaded = alreadyDownloadedBytes != 0 ? alreadyDownloadedBytes : 0;
        var stopwatch = Stopwatch.StartNew();

        foreach (var url in urls)
        {
            using var response = await client.GetAsync($"{baseUrl}/{url}", HttpCompletionOption.ResponseHeadersRead);
            using var stream = await response.Content.ReadAsStreamAsync();

            var buffer = new byte[32768];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                fileStream.Write(buffer, 0, bytesRead);
                downloaded += bytesRead;

                // Progress update
                var percent = (double)downloaded / totalSize * 100;
                var speed = downloaded / (stopwatch.Elapsed.TotalSeconds + 1); // Bytes per second
                var speedStr = speed >= 1_048_576 // 1MB = 1,048,576 bytes
                    ? $"{speed / 1_048_576:0.0} MB/s"
                    : $"{speed / 1024:0.0} KB/s";
                Console.Write($"\rProgress: {percent:0.0}% | Speed: {speedStr}  ");
                wavevm.UpdateProgress(percent);
                wavevm.UpdateDownloadSpeed(speedStr);
            }
        }
    }

    private async Task<string> getMVKeys(string pssh, string id)
    {
        var base64_key = $"data:text/plain;base64,{pssh}";
        var ClientIdFile = new FileInfo(_settings.AM_DeviceId);
        var PrivateKeyFile = new FileInfo(_settings.AM_DeviceKey);

        var resp1 = await LicenseUrl
            .WithHeaders(headers)
            .WithHeader("media-user-token", _settings.AM_UserToken)
            .WithOAuthBearerToken(_settings.AM_AccessToken)
            .PostJsonAsync(new AppleMusicDataPayload
            {
                AdamId = id, Challenge = "CAQ=", IsLibrary = false, KeySystem = "com.widevine.alpha", Uri = base64_key,
                UserInitiated = true
            })
            .ReceiveString();

        var certDataB64 = JObject.Parse(resp1).Value<string>("license");
        var cdm = new WVApi(ClientIdFile, PrivateKeyFile);
        var challenge = cdm.GetChallenge(pssh, certDataB64);
        var challengeB64 = Convert.ToBase64String(challenge);
        var resp2 = await LicenseUrl
            .WithHeaders(headers)
            .WithHeader("media-user-token", _settings.AM_UserToken)
            .WithOAuthBearerToken(_settings.AM_AccessToken)
            .PostJsonAsync(new AppleMusicDataPayload
            {
                AdamId = id, Challenge = challengeB64, IsLibrary = false, KeySystem = "com.widevine.alpha",
                Uri = base64_key, UserInitiated = true
            })
            .ReceiveString();

        var licenseB64 = JObject.Parse(resp2).Value<string>("license");
        cdm.ProvideLicense(licenseB64);
        return cdm.GetKeys()[0].ToString().Split(":")[1];
    }

    private async Task<AppleMusicStreamList> getMediaMetadata(string url)
    {
        var output = new AppleMusicStreamList();
        var splitInd = url.LastIndexOf('/');
        Url baseURL = new Uri(url.Substring(0, splitInd));
        output.BaseUri = baseURL;
        using (var client = new WebClient())
        {
            var playlistRaw = client.DownloadString(url);
            var isKey = false;
            var keyUri = "";
            var uris = new List<string>();

            var splitTest = playlistRaw.Split('\n');
            for (var i = 0; i < splitTest.Length - 1; i++)
            {
                if (splitTest[i].Contains("#EXT-X-KEY") &&
                    splitTest[i].Contains("urn:uuid:edef8ba9-79d6-4ace-a3c8-27dcd51d21ed"))
                {
                    isKey = true;
                    keyUri = splitTest[i].Split('"')[1].Replace("data:text/plain;base64,", "");
                }

                if (isKey)
                {
                    if (splitTest[i].Contains("#EXT-X-MAP:URI"))
                        //Append filepath, stripping out m3u8 metadata and quotation characters
                        uris.Add($"{splitTest[i].Replace("#EXT-X-MAP:URI=", "").Replace("\"", "")}");
                    else if (!splitTest[i].StartsWith('#'))
                        //Take Directly
                        uris.Add(splitTest[i]);
                }
            }

            output.Uris = uris;
            output.Key = keyUri;
            return output;
        }
    }

    private MasterPlaylist parseMusicVideoUri(string assetUrl)
    {
        using (var client = new WebClient())
        {
            var playlistRaw = client.DownloadString(assetUrl);
            return MasterPlaylist.LoadFromText(playlistRaw);
        }
    }

    private async Task<JToken> get_webplayback(int id)
    {
        var result = await WebplaybackUrl
            .WithHeaders(headers)
            .WithHeader("media-user-token", _settings.AM_UserToken)
            .WithOAuthBearerToken(_settings.AM_AccessToken)
            .PostJsonAsync(new { salableAdamId = id.ToString() })
            .ReceiveString();
        var output = JsonConvert.DeserializeObject<JObject>(result,
            new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
        return output.Value<JArray>("songList")[0];
    }
}