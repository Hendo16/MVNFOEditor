using MVNFOEditor.Models;

namespace MVNFOEditor.Interface;

public interface IMetadata
{
    void GetBrowseData();
    SearchSource GetSearchSource();
    string GetArtwork();
}