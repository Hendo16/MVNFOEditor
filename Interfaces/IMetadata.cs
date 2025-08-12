using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Models;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Interface;

public interface IMetadata
{
    void GetBrowseData();
    SearchSource GetSearchSource();
    string GetArtwork();
}