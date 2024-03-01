using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Controls;

namespace MVNFOEditor.ViewModels
{
    public partial class SyncDialogViewModel : ObservableObject
    {
        private ObservableCollection<SyncResultViewModel> _results;
        private ObservableCollection<SyncResultViewModel> _fullResults;
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

        public ObservableCollection<SyncResultViewModel> SearchResults
        {
            get { return _results; }
            set
            {
                _results = value;
                OnPropertyChanged(nameof(SearchResults));
            }
        }

        public void FilterResults()
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                SearchResults = _fullResults;
                return;
            }
            SearchResults = new ObservableCollection<SyncResultViewModel>(
                SearchResults.Where(item => item.Title?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
            );
        }

        public SyncDialogViewModel(ObservableCollection<SyncResultViewModel> results)
        {
            _results = results;
            _fullResults = results;
        }

        [RelayCommand]
        public void CloseDialog() => SukiHost.CloseDialog();
    }
}
