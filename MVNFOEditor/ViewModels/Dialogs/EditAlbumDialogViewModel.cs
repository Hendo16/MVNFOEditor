using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;

namespace MVNFOEditor.ViewModels;

public partial class EditAlbumDialogViewModel : ObservableObject
{
    private readonly ArtistListParentViewModel _parentVM;
    private readonly MusicDBHelper DBHelper;
    [ObservableProperty] private Album _album;
    [ObservableProperty] private Bitmap? _cover;

    [ObservableProperty] private string _coverURL;
    [ObservableProperty] private bool _localRadio;
    [ObservableProperty] private string _title;
    [ObservableProperty] private bool _uRLRadio;
    [ObservableProperty] private string _year;

    public EditAlbumDialogViewModel(Album album, Bitmap? coverInstance)
    {
        _album = album;
        Title = album.Title;
        Year = album.Year;
        Cover = coverInstance;
        DBHelper = App.GetDBHelper();
        _parentVM = App.GetVM().GetParentView();
        if (album.ArtURL != null)
        {
            CoverURL = album.ArtURL;
            _uRLRadio = true;
        }
        else
        {
            _localRadio = true;
        }
    }

    public async void UpdateAlbum()
    {
        _album.Title = Title;
        _album.Year = Year;
        var success = await DBHelper.UpdateAlbum(_album);
        if (success == 0) Debug.WriteLine(success);
        //Refresh View
        RefreshView();
    }

    public async void GrabURL()
    {
        var data = await NetworkHandler.GetFileData(CoverURL);
        if (data == null)
        {
            ToastHelper.ShowError("Cover Error", "Couldn't fetch album artwork, please check logs");
            return;
        }
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
        var ArtistDetailsVM = (ArtistDetailsViewModel)_parentVM.CurrentContent;
        ArtistDetailsVM.LoadAlbums();
        App.GetVM().GetDialogManager().DismissDialog();
    }

    [RelayCommand]
    public void CloseDialog()
    {
        App.GetVM().GetDialogManager().DismissDialog();
    }
}