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

        public AlbumResultsViewModel(SearchSource source, string searchText = "")
        {
            selectedSource = source;
            SearchText = searchText;
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
            if (string.IsNullOrEmpty(SearchText) || SearchResults == null)
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
            Results = FullResults = resultCards;
            LoadCovers();
            FilterResults();
        }
        
        private AlbumResultViewModel AlbumResultToVM(AlbumResult result)
        {
            return new AlbumResultViewModel(result);
        }
    }
}
