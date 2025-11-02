using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MVNFOEditor.Models;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Factories;

public interface IResultFactory
{
    public Task<ObservableCollection<ArtistResultViewModel>> SearchArtists(string artistStr);
    public Task<ObservableCollection<AlbumResultViewModel>> GetAlbums(Artist artist);
}