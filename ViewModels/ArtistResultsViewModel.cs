using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVNFOEditor.Models;
using Newtonsoft.Json.Linq;
using Avalonia.Input;

namespace MVNFOEditor.ViewModels
{
    public partial class ArtistResultsViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<ArtistResultViewModel> _searchResults;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _busyText;
        [ObservableProperty] private string _searchInput;
        [ObservableProperty] private ArtistResultViewModel _selectedArtist;
        private YTMusicHelper ytMusicHelper;

        public event EventHandler<ArtistResult> NextPage;
        public event EventHandler<bool> ValidSearch;

        public ArtistResultsViewModel()
        {
            SearchResults = new ObservableCollection<ArtistResultViewModel>();
            ytMusicHelper = App.GetYTMusicHelper();
        }

        public async void SearchArtist()
        {
            ValidSearch(null, true);
            JArray results = ytMusicHelper.search_Artists(_searchInput);
            for (int i = 0; i < results.Count; i++)
            {
                JObject result = results[i].ToObject<JObject>();
                ArtistResult arrResult = new ArtistResult();

                arrResult.Name = result["artist"].ToString();
                arrResult.browseId = result["browseId"].ToString();
                arrResult.thumbURL = ytMusicHelper.GetHighQualityArt(result);

                ArtistResultViewModel newVM = new ArtistResultViewModel(arrResult);
                newVM.NextPage += NextPage;
                await newVM.LoadThumbnail();
                SearchResults.Add(newVM);
            }
        }
    }
}