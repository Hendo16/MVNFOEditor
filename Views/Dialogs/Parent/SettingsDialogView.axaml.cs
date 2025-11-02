using System;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using MVNFOEditor.ViewModels;
using SukiUI.Toasts;

namespace MVNFOEditor.Views;

public partial class SettingsDialogView : UserControl
{
    public SettingsDialogView()
    {
        InitializeComponent();
    }

    public void Previous(object source, RoutedEventArgs args)
    {
        if (DataContext is SettingsDialogViewModel viewModel) viewModel.HandleBackwards(settingsPages);
    }

    public void Next(object source, RoutedEventArgs args)
    {
        if (DataContext is SettingsDialogViewModel viewModel)
        {
            if (viewModel.StepIndex == 1 && FolderView.MVInput.Text == "n/a")
            {
                App.GetVM().GetToastManager().CreateToast()
                    .WithTitle("Error!")
                    .WithContent("Music Video Folder Missing!")
                    .OfType(NotificationType.Error)
                    .Dismiss().After(TimeSpan.FromSeconds(3))
                    .Queue();
                return;
            }

            viewModel.HandleForward(settingsPages);
        }
    }
}