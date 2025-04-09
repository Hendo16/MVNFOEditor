using Avalonia.Media.Imaging;
using MVNFOEditor.Models;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using YoutubeDLSharp.Metadata;

namespace MVNFOEditor.ViewModels;

public class ManualMVEntryViewModel : ObservableObject
{
    public string Title { get; set; }
    public string VidID { get; set; }
    public Album Album { get; set; }
    public string Year { get; set; }
    public VideoData? VidData { get; set; }
    public Bitmap? Thumbnail { get; set; }
    
    public event EventHandler RemoveCallback;

    public ManualMVEntryViewModel(string _title,
        string _year, 
        Bitmap? _thumbnail, 
        string _vidID, 
        Album _album,
        VideoData _vidData)
    {
        Title = _title;
        Year = _year;
        Thumbnail = _thumbnail;
        VidID = _vidID;
        Album = _album;
        VidData = _vidData;
    }

    public void RemoveListing()
    {
        RemoveCallback?.Invoke(this, EventArgs.Empty);
    }
}