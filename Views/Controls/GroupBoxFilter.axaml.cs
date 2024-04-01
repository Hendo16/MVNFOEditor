using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SukiUI.Controls;

namespace MVNFOEditor.Views
{
    public partial class GroupBoxFilter : UserControl
    {
        public GroupBoxFilter()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static readonly StyledProperty<object?> HeaderProperty =
            AvaloniaProperty.Register<GroupBox, object?>(nameof(Header), defaultValue: "Header");

        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }
    }
}