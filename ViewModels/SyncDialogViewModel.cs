using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using SukiUI.Controls;

namespace MVNFOEditor.ViewModels
{
    public class SelectedItem
    {
        public SyncResultViewModel viewModel { get; set; }
        public bool IsSelected { get; set; }
    }
    public partial class SyncDialogViewModel : ObservableObject
    {
        private ObservableCollection<SyncResultViewModel> _results;
        private ObservableCollection<SyncResultViewModel> _fullResults;
        private ObservableCollection<SelectedItem> _fullSelectedItems;

        [ObservableProperty] private ObservableCollection<SyncResultViewModel> _selectedVideos;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _busyText;
        [ObservableProperty] private string _searchText;

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
                _fullResults.Where(item => item.Title?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        public SyncDialogViewModel(ObservableCollection<SyncResultViewModel> results)
        {
            _results = results;
            _fullResults = results;
            _fullSelectedItems = new ObservableCollection<SelectedItem>();
            SelectedVideos = new ObservableCollection<SyncResultViewModel>();
        }

        [RelayCommand]
        public void CloseDialog() => SukiHost.CloseDialog();
    }
}
