using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using System;

namespace MVNFOEditor
{
    public class ArtistCard : TemplatedControl
    {
        public static readonly StyledProperty<Image> ImageProperty =
            AvaloniaProperty.Register<ArtistCard, Image>(nameof(Image));

        public static readonly StyledProperty<string> ArtistNameProperty =
            AvaloniaProperty.Register<ArtistCard, string>(nameof(ArtistName));

        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
            RoutedEvent.Register<ArtistCard, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

        public Image Image
        {
            get => GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        public string ArtistName
        {
            get => GetValue(ArtistNameProperty);
            set => SetValue(ArtistNameProperty, value);
        }

        public event EventHandler<RoutedEventArgs> Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        public ArtistCard()
        {
            ArtistNameProperty.Changed.AddClassHandler<ArtistCard>((sender, e) => sender.OnArtistNameChanged(e));
        }

        private void OnArtistNameChanged(AvaloniaPropertyChangedEventArgs e)
        {
            // Update the visual representation of the artist name
            // This could involve updating the UI or some other logic
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            var button = e.NameScope.Find<Button>("PART_Button");
            if (button != null)
            {
                button.Click += HandleButtonClick;
            }
        }

        private void HandleButtonClick(object sender, RoutedEventArgs e)
        {
            // Handle the button click event here
            RaiseEvent(new RoutedEventArgs(ClickEvent));
        }
    }
}
