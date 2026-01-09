using System.Text.RegularExpressions;

namespace MVNFOEditor;

public static class RegexHandler
{
    public const string FilePathChecker =
        "^(?:[a-zA-Z]\\:|\\\\\\\\[\\w\\.]+\\\\[\\w.$]+)\\\\(?:[\\w]+\\\\)*\\w([\\w.])+$";
    public static bool IsLocalFile(string value)
    {
        Regex reg = new Regex(FilePathChecker);
        return reg.IsMatch(value);
    }
}