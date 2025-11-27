using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using MVNFOEditor.Helpers;

namespace MVNFOEditor.ViewModels;

public partial class MusicVideoListViewModel : ObservableObject
{
    private readonly MusicDBHelper DBHelper;
    [ObservableProperty] private string _busyText;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private ObservableCollection<VideoCardViewModel> _singleCards;

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
        if (asc)
            SingleCards = new ObservableCollection<VideoCardViewModel>(SingleCards.OrderBy(a => a.Title));
        else
            SingleCards = new ObservableCollection<VideoCardViewModel>(SingleCards.OrderByDescending(a => a.Title));
    }

    public void CodecFilter()
    {
    }
}