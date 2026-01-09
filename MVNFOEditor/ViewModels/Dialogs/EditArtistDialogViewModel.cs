using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using Responsive.Avalonia;

namespace MVNFOEditor.ViewModels;

public partial class EditArtistDialogViewModel : ObservableObject
{
    [ObservableProperty] private Artist _artist;
    [ObservableProperty] private List<Bitmap> _banners = new();
    [ObservableProperty] private Bitmap _selectedBanner;
    [ObservableProperty] private bool _showBannerButtons;
    [ObservableProperty] private bool _showMetadata = true;
    [ObservableProperty] private bool _amEnabled;
    [ObservableProperty] private bool _ytEnabled;
    [ObservableProperty] private string _description;
    [ObservableProperty] private string _name;
    public event EventHandler<bool> RefreshArtistCard;

    public EditArtistDialogViewModel(Artist artist)
    {
        Artist = artist;
        Name = artist.Name;
        Description = artist.Description ?? "";
        YtEnabled = Artist.Metadata.Any(am => am.SourceId == SearchSource.YouTubeMusic);
        AmEnabled = Artist.Metadata.Any(am => am.SourceId == SearchSource.AppleMusic);
        if (!YtEnabled && !AmEnabled)
        {
            ShowMetadata = false;
        }
    }


    public async void UpdateArtist()
    {
        Artist.Name = Name;
        Artist.Description = Description;
        if (ShowMetadata)
        {
            Artist.UpdateArtistBanner(SelectedBanner);
        }
        var success = await App.GetDBHelper().UpdateArtist(Artist);
        if (success == 0) Debug.WriteLine(success);
        //Refresh View
        HandleClose();
    }

    public void PullYoutubeInfo()
    {
        var amData = Artist.GetArtistMetadata(SearchSource.YouTubeMusic);
        Name = amData.OriginalTitle;
        Description = amData.Description;
    }

    public void RemoveYoutubeMetadata()
    {
        if (!AmEnabled) return;
        var amData = Artist.GetArtistMetadata(SearchSource.YouTubeMusic);
        Artist.Metadata.Remove(amData);
        App.GetDBContext().ArtistMetadata.Remove(amData);
        App.GetDBContext().SaveChanges();
        YtEnabled = false;
        RefreshArtistCard?.Invoke(this, true);
    }

    public void RemoveAMMetadata()
    {
        if (!YtEnabled) return;
        var amData = Artist.GetArtistMetadata(SearchSource.AppleMusic);
        Artist.Metadata.Remove(amData);
        App.GetDBContext().ArtistMetadata.Remove(amData);
        App.GetDBContext().SaveChanges();
        AmEnabled = false;
        RefreshArtistCard?.Invoke(this, true);
    }

    public void PullAMInfo()
    {
        var amData = Artist.GetArtistMetadata(SearchSource.AppleMusic);
        Name = amData.OriginalTitle;
        Description = amData.Description;
    }

    public async Task LoadBanners()
    {
        List<string?> coverUrLs = Artist.GetBannerURLs();
        ShowBannerButtons = coverUrLs.Count > 1 && coverUrLs.All(c => c != null);
        foreach (string? coverUrl in coverUrLs)
        {
            if (coverUrl == null)
            {
                continue;
            }
            var stream = await NetworkHandler.GetFileData(coverUrl);
            if (stream == null)
            {
                ToastHelper.ShowError("Cover Error", "Couldn't fetch album artwork, please check logs");
                Banners.Add(new Bitmap(File.OpenRead("./Assets/defaultBanner.jpg")));
                continue;
            }
            //var bitmap = await Task.Run(() => Bitmap.DecodeToWidth(stream, 800));
            //Banners.Add(bitmap);
            Banners.Add(new Bitmap(new MemoryStream(stream)));
        }
    }

    public void DeleteArtist()
    {
        App.GetDBHelper().DeleteArtist(Artist);
        //Refresh View
        HandleClose();
    }

    private void HandleClose()
    {
        //RefreshArtistCard?.Invoke(this, true);
        App.GetVM().GetParentView().RefreshArtistList();
        App.GetVM().GetDialogManager().DismissDialog();
    }

    [RelayCommand]
    public void CloseDialog()
    {
        App.GetVM().GetDialogManager().DismissDialog();
    }
}