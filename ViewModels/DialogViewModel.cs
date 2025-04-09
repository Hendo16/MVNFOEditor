using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Dialogs;

namespace MVNFOEditor.ViewModels;

public partial class VmDialogViewModel(ISukiDialog dialog) : ObservableObject
{
    [RelayCommand]
    private void CloseDialog() => dialog.Dismiss();
}