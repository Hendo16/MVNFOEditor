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
}