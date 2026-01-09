using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Dialogs;

namespace MVNFOEditor.ViewModels;

public partial class AMUserSubmissionViewModel(ISukiDialog dialog) : ObservableObject
{
    [ObservableProperty] private string _token;

    [RelayCommand]
    private async void SubmitToken()
    {
        Console.WriteLine(Token);
        var valid = await App.GetAppleMusicDLHelper().UpdateUserToken(Token);
        if (valid)
        {
            dialog.Dismiss();
        }
    }

    [RelayCommand]
    private void CloseDialog()
    {
        dialog.Dismiss();
    }
}