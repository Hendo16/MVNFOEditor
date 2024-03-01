using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVNFOEditor.ViewModels
{
    public partial class AlbumResultsViewModel : ObservableObject
    {
        private ObservableCollection<AlbumResultViewModel> _results;
        private ObservableCollection<AlbumResultViewModel> _fullResults;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _busyText;

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

        public AlbumResultsViewModel(ObservableCollection<AlbumResultViewModel> results)
        {
            _results = results;
            _fullResults = results;
        }

        public void FilterResults()
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                SearchResults = _fullResults;
                return;
            }
            SearchResults = new ObservableCollection<AlbumResultViewModel>(
                SearchResults.Where(item => item.Title?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
            );
        }
    }
}
