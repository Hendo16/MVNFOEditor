using System;
using Avalonia.Controls.Notifications;
using SukiUI.Toasts;

namespace MVNFOEditor.Helpers;

public class ToastHelper
{
    public static void ShowError(string title, string content, NotificationType type =  NotificationType.Error, int seconds = 5)
    {
        App.GetVM().GetToastManager().CreateToast()
            .WithTitle(title)
            .WithContent(content)
            .OfType(type)
            .Dismiss()
            .After(TimeSpan.FromSeconds(seconds))
            .Queue();
    }
    
    public static void ShowSuccess(string title, string content, NotificationType type =  NotificationType.Success, int seconds = 5)
    {
        App.GetVM().GetToastManager().CreateToast()
            .WithTitle(title)
            .WithContent(content)
            .OfType(type)
            .Dismiss()
            .After(TimeSpan.FromSeconds(seconds))
            .Queue();
    }
}