using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SukiUI.Controls;

namespace MVNFOEditor.Views;

public partial class GroupBoxFilter : UserControl
{
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<GroupBox, object?>(nameof(Header), "Header");

    public GroupBoxFilter()
    {
        InitializeComponent();
    }

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}