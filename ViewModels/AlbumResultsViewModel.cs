using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using MVNFOEditor.Models;

namespace MVNFOEditor.ViewModels
{
    public partial class AlbumResultsViewModel : ObservableObject
    {
        private ObservableCollection<AlbumResultViewModel> _results;
        private ObservableCollection<AlbumResultViewModel> _fullResults;
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

        public ObservableCollection<AlbumResultViewModel> SearchResults
        {
            get { return _results; }
            set
            {
                _results = value;
                OnPropertyChanged(nameof(SearchResults));
            }
        }

        public AlbumResultsViewModel(ObservableCollection<AlbumResultViewModel> results, SearchSource source)
        {
            _results = results;
            _fullResults = results;
            selectedSource = source;
        }

        public async void LoadCovers()
        {
            foreach (var result in _results)
            {
                await result.LoadThumbnail();
            }
        }

        public void FilterResults()
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                SearchResults = _fullResults;
                return;
            }
            SearchResults = new ObservableCollection<AlbumResultViewModel>(
                _fullResults.Where(item => item.Title?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
            );
        }

        public void HandleSelection()
        {

        }
    }
}
