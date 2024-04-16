using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Styling;
using SukiUI.Models;

namespace MVNFOEditor.Models
{
    public class SettingsData
    {
        public int Id { get; set; }
        public string RootFolder { get; set; }
        public string FFMPEGPath { get; set; }
        public string YTDLPath { get; set; }
        public string YTDLFormat { get; set; }
        public int ScreenshotSecond { get; set; }
        public bool AnimatedBackground { get; set; }
        public ThemeVariant? LightOrDark { get; set; }
        public SukiColorTheme? Theme { get; set; }
    }
}