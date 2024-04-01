using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        public Artist Artist => _album.Artist;
        private MusicDBHelper DBHelper;
        private static HttpClient s_httpClient = new();

        [ObservableProperty] private string _coverURL;
        [ObservableProperty] private Album _album;
        [ObservableProperty] private Bitmap? _cover;
        [ObservableProperty] private string _title;
        [ObservableProperty] private string _year;
        [ObservableProperty] private bool _uRLRadio;
        [ObservableProperty] private bool _localRadio;

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
        public async void GrabURL()
        {
            var data = await s_httpClient.GetByteArrayAsync(CoverURL);
            var ms = new MemoryStream(data);
            Cover = await Task.Run(() => Bitmap.DecodeToWidth(ms, 200));
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