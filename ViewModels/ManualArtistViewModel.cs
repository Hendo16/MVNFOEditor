using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using MVNFOEditor.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace MVNFOEditor.ViewModels
{
    public class ManualArtistViewModel : ReactiveObject
    {
        private string _artistNameText;
        public string ArtistNameText
        {
            get => _artistNameText;
            set => this.RaiseAndSetIfChanged(ref _artistNameText, value);
        }
        
        private Bitmap? _banner;
        private string _bannerPath;

        public Bitmap? ArtistBanner
        {
            get => _banner;
            private set => this.RaiseAndSetIfChanged(ref _banner, value);
        }
        public string BannerPath
        {
            get => _bannerPath;
            set => this.RaiseAndSetIfChanged(ref _bannerPath, value);
        }

        public async void LoadBanner(string path)
        {
            _bannerPath = path;
            ArtistBanner = await Task.Run(() => Bitmap.DecodeToWidth(File.OpenRead(path), 270));
        }
    }
}