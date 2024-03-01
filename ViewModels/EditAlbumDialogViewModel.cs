using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using SukiUI.Controls;

namespace MVNFOEditor.ViewModels
{
    public partial class EditAlbumDialogViewModel : ObservableObject
    {
        private ArtistListParentViewModel _parentVM;
        private Album _album;
        private string _title;
        private string _year;
        public Artist Artist => _album.Artist;
        public Bitmap? Cover { get; set; }
        private MusicDBHelper DBHelper;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public string Year
        {
            get { return _year; }
            set
            {
                _year = value;
                OnPropertyChanged(nameof(Year));
            }
        }

        public EditAlbumDialogViewModel(Album album, Bitmap? coverInstance)
        {
            _album = album;
            Title = album.Title;
            Year = album.Year;
            Cover = coverInstance;
            DBHelper = App.GetDBHelper();
            _parentVM = App.GetVM().GetParentView();
        }
        public async void UpdateAlbum()
        {
            _album.Title = Title;
            _album.Year = Year;
            int success = await DBHelper.UpdateAlbum(_album);
            if (success == 0)
            {
                Debug.WriteLine(success);
            }
            //Refresh View
            RefreshView();
        }

        public void DeleteAlbum()
        {
            DBHelper.DeleteAlbum(_album);
            //Refresh View
            RefreshView();
        }

        private void RefreshView()
        {
            ArtistDetailsViewModel ArtistDetailsVM = (ArtistDetailsViewModel)_parentVM.CurrentContent;
            ArtistDetailsVM.LoadAlbums();
            SukiHost.CloseDialog();
        }
    }
}