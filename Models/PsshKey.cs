namespace MVNFOEditor.Models;

public class PsshKey
{
    public PsshKey()
    {
    }

    public PsshKey(string _pssh, string _key)
    {
        pssh = _pssh;
        key = _key;
    }

    public int id { get; set; }
    public string pssh { get; set; }
    public string key { get; set; }
}