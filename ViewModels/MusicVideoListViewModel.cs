using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Helpers;
using MVNFOEditor.Views;
using SukiUI.Controls;

namespace MVNFOEditor.ViewModels
{
    public partial class MusicVideoListViewModel : ObservableObject
    {
        private MusicDBHelper DBHelper;

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _busyText;
        [ObservableProperty] private ObservableCollection<SingleViewModel> _singleCards;

        public MusicVideoListViewModel()
        {
            DBHelper = App.GetDBHelper();
            GetSingles();
        }

        public async void GetSingles()
        {
            IsBusy = true;
            BusyText = "Getting Videos...";
            SingleCards = await DBHelper.GenerateAllSingles();
            IsBusy = false;
        }

        public void AddSingle()
        {
            Debug.WriteLine("coming");
        }

        public void ReverseNameOrder(bool asc)
        {
            if (asc){ SingleCards= new ObservableCollection<SingleViewModel>(SingleCards.OrderBy(a => a.Title));}
            else{ SingleCards= new ObservableCollection<SingleViewModel>(SingleCards.OrderByDescending(a => a.Title));}
        }

        public void CodecFilter()
        {

        }
    }
}