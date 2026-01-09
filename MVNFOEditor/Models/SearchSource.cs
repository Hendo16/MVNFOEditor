namespace MVNFOEditor.Models;

public enum SearchSource
{
    YouTubeMusic,
    AppleMusic,
    Manual
}

public static class SearchSourceExt
{
    public static string GetSourceIconPath(this SearchSource source)
    {
        switch (source)
        {
            case SearchSource.Manual:
                return "./Assets/manual-48x48.png";
            case SearchSource.AppleMusic:
                return "./Assets/am-48x48.png";
            case SearchSource.YouTubeMusic:
                return "./Assets/ytm-48x48.png";
        }
        return "";
    }
}