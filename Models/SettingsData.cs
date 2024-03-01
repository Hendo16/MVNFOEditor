using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVNFOEditor.Models
{
    public class SettingsData
    {
        public int Id { get; set; }
        public string RootFolder { get; set; }
        public string FFMPEGPath { get; set; }
        public string YTDLPath { get; set; }
    }
}