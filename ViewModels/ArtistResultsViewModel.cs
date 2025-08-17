using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using MVNFOEditor.Models;
using Newtonsoft.Json.Linq;
using Avalonia.Input;
using Flurl.Http;
using log4net;
using SukiUI.Toasts;
using YtMusicNet.Records;

namespace MVNFOEditor.ViewModels
{
    public partial class ArtistResultsViewModel : ObservableObject
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ArtistResultsViewModel));
        [ObservableProperty] private ObservableCollection<ArtistResultViewModel> _searchResults;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _searching;
        [ObservableProperty] private string _busyText;
        [ObservableProperty] private string _searchInput;
        [ObservableProperty] private ArtistResultViewModel _selectedArtist;
        private YTMusicHelper ytMusicHelper;
        private iTunesAPIHelper itunesHelper;

        public event EventHandler<ArtistResultCard> NextPage;
        public event EventHandler<bool> ValidSearch;
        public SearchSource selectedSource;

        public ArtistResultsViewModel(SearchSource _source, string searchInput = "")
        {
            SearchResults = new ObservableCollection<ArtistResultViewModel>();
            ytMusicHelper = App.GetYTMusicHelper();
            itunesHelper = App.GetiTunesHelper();
            Searching = false;
            selectedSource = _source;
            SearchInput = searchInput;
        }

        public void SearchArtist()
        {
            Searching = true;
            if (ValidSearch != null)
            {
                ValidSearch(null, true);
            }
            switch (selectedSource)
            {
                case SearchSource.YouTubeMusic:
                    YouTubeSearch();
                    return;
                case SearchSource.AppleMusic:
                    iTunesSearch();
                    return;
            }
        }

        private async void YouTubeSearch()
        {
            ObservableCollection<ArtistResultViewModel> _tempResults = new ObservableCollection<ArtistResultViewModel>();
            //JArray results = new JArray();
            List<ArtistResult> results = new List<ArtistResult>();
            try
            {
                results = await ytMusicHelper.searchArtists(SearchInput);
                
                if (ValidSearch != null)
                {
                    ValidSearch(null, true);
                }
            }
            catch (FlurlHttpException e)
            {
                Log.Error($"Error fetching results from service");
                Log.Error(e);
                App.GetToastManager().CreateToast()
                    .OfType(NotificationType.Error)
                    .WithContent("Error: Couldn't connect to server. Check logs")
                    .Dismiss().ByClicking()
                    .Queue();
                return;
            }
            for (int i = 0; i < results.Count; i++)
            {
                /*
                JObject result = results[i].ToObject<JObject>();
                ArtistResultCard arrResultCard = new ArtistResultCard();

                arrResultCard.Name = result["artist"].ToString();
                arrResultCard.browseId = result["browseId"].ToString();
                arrResultCard.thumbURL = ytMusicHelper.GetHighQualityArt(result);
                */
                ArtistResultCard arrResultCard = new ArtistResultCard(results[i]);
                ArtistResultViewModel newVM = new ArtistResultViewModel(arrResultCard);
                newVM.NextPage += NextPage;
                await newVM.LoadThumbnail();
                _tempResults.Add(newVM);
            }
            SearchResults = _tempResults;
            Searching = false;
        }

        private async void iTunesSearch()
        {
            Searching = true;
            ObservableCollection<ArtistResultViewModel> _tempResults = new ObservableCollection<ArtistResultViewModel>();
            JObject apiResults = new JObject();
            try
            {
                apiResults = await itunesHelper.ArtistSearch(_searchInput);
                if (ValidSearch != null)
                {
                    ValidSearch(null, true);
                }
            }
            catch (FlurlHttpException e)
            {
                Log.Error($"Error fetching results from service");
                Log.Error(e);
                App.GetToastManager().CreateToast()
                    .OfType(NotificationType.Error)
                    .WithContent($"Error: Couldn't connect to server. {e.InnerException.Message}")
                    .Dismiss().ByClicking()
                    .Queue();
                Searching = false;
                return;
            }
            Console.WriteLine($"Total Results {apiResults.Value<int>("resultCount")}");
            JArray results = apiResults.Value<JArray>("results");
            //TODO: Handle search results greater than the first 10 artists
            int limit = results.Count < 10 ? results.Count : 10;
            for (int i = 0; i < limit; i++)
            {
                JObject result = results[i].ToObject<JObject>();
                ArtistResultCard arrResultCard = new ArtistResultCard();

                arrResultCard.Name = result.Value<string>("artistName");
                arrResultCard.browseId = result.Value<int>("artistId").ToString();
                
                //Some results return outdated iTunes based links - so we need to fix these
                arrResultCard.artistLinkURL = result.Value<string>("artistLinkUrl").Replace("itunes.apple.com", "music.apple.com");
                arrResultCard.thumbURL =  itunesHelper.GetArtistThumb(arrResultCard.artistLinkURL);

                ArtistResultViewModel newVM = new ArtistResultViewModel(arrResultCard);
                newVM.NextPage += NextPage;
                await newVM.LoadThumbnail();
                _tempResults.Add(newVM);
            }
            SearchResults = _tempResults;
            Searching = false;
        }
    }
}