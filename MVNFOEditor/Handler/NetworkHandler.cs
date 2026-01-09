using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using log4net;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor;

public static class NetworkHandler
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(NetworkHandler));
    public static async Task<byte[]?> GetFileData(string url)
    {
        try
        {
            return await App.GetHttpClient().GetByteArrayAsync(url);
        }
        catch (HttpRequestException ex)
        {
            Log.ErrorFormat("NetworkError:\n StatusCode:{0}\n ErrorMessage: {1}", ex.StatusCode, ex.Message);
            return null;
        }
    }
    
    public static async Task<long> GetTotalFileSize(List<string> uris, string baseUrl)
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
    
    public static async Task DownloadWithProgressAsync(List<string>? urls, string baseUrl, string filename,
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
                var speed = downloaded / (stopwatch.Elapsed.TotalSeconds + 1);
                var speedStr = speed >= 1_048_576
                    ? $"{speed / 1_048_576:0.0} MB/s"
                    : $"{speed / 1024:0.0} KB/s";
                Console.Write($"\rProgress: {percent:0.0}% | Speed: {speedStr}  ");
                wavevm.UpdateProgress(percent);
                wavevm.UpdateDownloadSpeed(speedStr);
            }
        }
    }
}