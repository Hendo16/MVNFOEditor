using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using SukiUI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVNFOEditor.ViewModels
{
    public partial class EditArtistDialogViewModel : ObservableObject
    {
        private ArtistListParentViewModel _parentVM;
        public Bitmap? Banner { get; set; }
        private MusicDBHelper DBHelper;
        [ObservableProperty] private Artist _artist;
        [ObservableProperty] private string _name;

        public EditArtistDialogViewModel(Artist artist, Bitmap? coverInstance)
        {
            Artist = artist;
            Name = artist.Name;
            Banner = coverInstance;
            DBHelper = App.GetDBHelper();
            _parentVM = App.GetVM().GetParentView();
        }
        public async void UpdateArtist()
        {
            Artist.Name = Name;
            int success = await DBHelper.UpdateArtist(Artist);
            if (success == 0)
            {
                Debug.WriteLine(success);
            }
            //Refresh View
            RefreshView();
        }

        public void DeleteArtist()
        {
            DBHelper.DeleteArtist(Artist);
            //Refresh View
            RefreshView();
        }

        private void RefreshView()
        {
            _parentVM.RefreshList();
            SukiHost.CloseDialog();
        }
    }
}