using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MVNFOEditor.Models;
using MVNFOEditor.ViewModels;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Interface;

public interface ISearchHelper
{
    Task<ObservableCollection<AlbumResultViewModel>> GenerateAlbumResultList(Artist artist);
    Task<ObservableCollection<VideoResultViewModel>> GenerateVideoResultList(JArray vidResults, Artist artist);
}