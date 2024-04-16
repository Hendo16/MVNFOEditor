using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using MVNFOEditor.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using MVNFOEditor.Features;
using Material.Icons;

namespace MVNFOEditor.ViewModels
{
    public class ArtistListParentViewModel : PageBase
    {
        private ArtistListViewModel _listVM;
        private ArtistDetailsViewModel _detailsVM;
        private MusicDBHelper _dbHelper;
        private object _currentContent;
        public object CurrentContent
        {
            get { return _currentContent; }
            set
            {
                _currentContent = value;
                OnPropertyChanged(nameof(CurrentContent));
            }
        }

        public ArtistListParentViewModel() : base("Artist List", MaterialIconKind.AccountMusic, 1)
        {
            ArtistListViewModel currView = new ArtistListViewModel();
            CurrentContent = currView;
            _listVM = currView;
            _dbHelper = App.GetDBHelper();
        }

        public void SetDetailsVM(ArtistDetailsViewModel vm)
        {
            _detailsVM = vm;
        }

        public void RefreshDetails()
        {
            _detailsVM.LoadAlbums();;
        }

        public void RefreshList()
        {
            _listVM.LoadArtists();
        }

        public void BackToDetails(bool reload = false)
        {
            if (reload) { _detailsVM.LoadAlbums(); }
            CurrentContent = _detailsVM;
        }
        
        public void InitList()
        {
            _listVM.InitData();
        }

        public async void BackToList(bool reload = false)
        {
            if (reload){ await _listVM.LoadArtists();}

            if (_detailsVM != null)
            {
                _detailsVM.ClearImages();
            }
            CurrentContent = _listVM;
        }
    }
}