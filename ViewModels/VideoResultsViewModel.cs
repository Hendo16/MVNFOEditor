using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MVNFOEditor.ViewModels
{
    public class SelectedItem
    {
        public VideoResultViewModel viewModel { get; set; }
        public bool IsSelected { get; set; }
    }
    public partial class VideoResultsViewModel : ObservableObject
    {
        private ObservableCollection<VideoResultViewModel> _results;
        private ObservableCollection<VideoResultViewModel> _fullResults;
        private ObservableCollection<SelectedItem> _fullSelectedItems;

        [ObservableProperty] private ObservableCollection<VideoResultViewModel> _selectedVideos;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _busyText;
        [ObservableProperty] private string _searchText;

        public ObservableCollection<VideoResultViewModel> SearchResults
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
            
            SearchResults = new ObservableCollection<VideoResultViewModel>(
                _fullResults.Where(item => item.Title?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        public VideoResultsViewModel(ObservableCollection<VideoResultViewModel> results)
        {
            _results = results;
            _fullResults = results;
            _fullSelectedItems = new ObservableCollection<SelectedItem>();
            SelectedVideos = new ObservableCollection<VideoResultViewModel>();
        }

        [RelayCommand]
        public void CloseDialog() =>
            App.GetVM().GetDialogManager().DismissDialog();
    }
}
