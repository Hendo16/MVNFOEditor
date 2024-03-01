using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVNFOEditor.Models;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.ViewModels
{
    public partial class ArtistResultsViewModel : ObservableObject
    {
        private ObservableCollection<ArtistResultViewModel> _results;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _busyText;
        [ObservableProperty] private string _searchInput;
        private YTMusicHelper ytMusicHelper;

        public event EventHandler<ArtistResult> NextPage;

        public ObservableCollection<ArtistResultViewModel> SearchResults
        {
            get { return _results; }
            set
            {
                _results = value;
                OnPropertyChanged(nameof(SearchResults));
            }
        }

        public ArtistResultsViewModel()
        {
            _results = new ObservableCollection<ArtistResultViewModel>();
            ytMusicHelper = App.GetYTMusicHelper();
        }

        public async void SearchArtist()
        {
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
                _results.Add(newVM);
            }
        }
    }
}