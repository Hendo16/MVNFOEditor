using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MVNFOEditor.Models;

namespace MVNFOEditor.ViewModels
{
    public partial class AlbumResultsViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<AlbumResultViewModel> _results;
        [ObservableProperty] private ObservableCollection<AlbumResultViewModel> _fullResults;
        [ObservableProperty] private ObservableCollection<AlbumResultViewModel> _searchResults;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _busyText;
        [ObservableProperty] private AlbumResultViewModel _selectedAlbum;
        
        public SearchSource selectedSource;

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                FilterResults();
            }
        }

        public AlbumResultsViewModel(SearchSource source)
        {
            //Results = results;
            //FullResults = results;
            selectedSource = source;
        }

        public async void LoadCovers()
        {
            foreach (var result in Results)
            {
                await result.LoadThumbnail();
            }
        }

        public void FilterResults()
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                SearchResults = FullResults;
                return;
            }
            SearchResults = new ObservableCollection<AlbumResultViewModel>(
                FullResults.Where(item => item.Title?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
            );
        }        
        public void GenerateNewResults(List<AlbumResult> albumResults)
        {
            ObservableCollection<AlbumResultViewModel> resultCards = new ObservableCollection<AlbumResultViewModel>(albumResults.ConvertAll(AlbumResultToVM));
            /*
            for (int i = 0; i < resultCards.Count; i++)
            {
                var albumVM = resultCards[i];
                albumVM.NextPage += async (a, ar) => { await parentRef.NextStep(a, ar); };
            }
            */
            Results = FullResults = resultCards;
            LoadCovers();
        }
        
        private AlbumResultViewModel AlbumResultToVM(AlbumResult result)
        {
            return new AlbumResultViewModel(result);
        }
    }
}
