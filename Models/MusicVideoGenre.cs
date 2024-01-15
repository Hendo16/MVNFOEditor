using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVNFOEditor.Models
{
    public class MusicVideoGenre
    {
        public int MusicVideoID { get; set; }
        public MusicVideo MusicVideo { get; set; }

        public int GenreID { get; set; }
        public Genre Genre { get; set; }
    }
}
