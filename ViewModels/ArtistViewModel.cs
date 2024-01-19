using MVNFOEditor.DB;
using MVNFOEditor.Models;
using System.Collections.ObjectModel;
using System.Linq;
using MVNFOEditor.Views;
using SimpleInjector.Advanced;

namespace MVNFOEditor.ViewModels
{
    public class ArtistViewModel : ViewModelBase
    {
        public MusicDbContext MVDBContext { get; set; }
        private ObservableCollection<CardViewModel> _artistCards;
        public ObservableCollection<CardViewModel> ArtistCards { get; set; }

        public ArtistViewModel()
        {
            MVDBContext = App.GetDBContext();
            if (MVDBContext.MusicVideos.Count() != 0)
            {
                var newList = new ObservableCollection<CardViewModel>();
                var ArtistList = MVDBContext.MusicVideos.Select(e => e.artist).Distinct();
                foreach (string artist in ArtistList)
                {
                    CardViewModel newCard = new CardViewModel();
                    newCard.ArtistName = artist;
                    newList.Add(newCard);
                }
                ArtistCards = newList;
            }
        }
    }
}