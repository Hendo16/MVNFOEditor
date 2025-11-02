using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVNFOEditor.Models;
using SukiUI.Dialogs;

namespace MVNFOEditor.ViewModels;

public partial class MergeArtistDialogViewModel(ISukiDialog dialog) : ObservableObject
{
    [ObservableProperty] private ArtistViewModel _artist1;
    [ObservableProperty] private ArtistViewModel _artist2;

    public MergeArtistDialogViewModel(ISukiDialog dialog, Artist orig, Artist merge_target) : this(dialog)
    {
        Artist1 = new ArtistViewModel(orig);
        Artist2 = new ArtistViewModel(merge_target);
        Artist1.LoadCover();
        Artist2.LoadCover();
    }

    public void HandleMerge()
    {
        Console.WriteLine($"Merging {Artist1.Name} with {Artist2.Name}");
    }

    [RelayCommand]
    private void CloseDialog()
    {
        dialog.Dismiss();
    }
}