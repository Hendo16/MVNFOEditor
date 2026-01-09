using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Enums;
using log4net;

namespace MVNFOEditor;

public static class VideoHandler
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(VideoHandler));
    public static string decryptPath = Path.GetFullPath("./Assets/mp4decrypt.exe");
    public static string decryptWorkingDir = Path.GetDirectoryName(decryptPath);
    
    public static Task DecryptStream(string key, string encPath, string decPath)
    {
        try
        {
            var arg = "--key 1:" + key + " \"" + encPath + "\" \"" + decPath + "\"";

            var decryptv = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Arguments = arg,
                    FileName = decryptPath,
                    WorkingDirectory = decryptWorkingDir
                }
            };

            decryptv.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            decryptv.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);
            decryptv.Start();
            decryptv.BeginOutputReadLine();
            decryptv.BeginErrorReadLine();
            return decryptv.WaitForExitAsync();
        }
        catch (Exception e)
        {
            Log.ErrorFormat("DecryptStream Exception:\n {0}", e.Message);
            return null;
        }
    }

    public static async Task<bool> AddAudioToVideo(string vidInp, string audInp, string outInp)
    {
        try
        {
            return await FFMpegArguments
                .FromFileInput(vidInp)
                .AddFileInput(audInp)
                .OutputToFile(outInp, true, options => options
                    .CopyChannel(Channel.Video)
                    .CopyChannel(Channel.Audio)
                )
                .ProcessAsynchronously();
        }
        catch (Exception e)
        {
            Log.ErrorFormat("AddAudioToVideo Exception:\n {0}", e.Message);
            return false;
        }
    }
}