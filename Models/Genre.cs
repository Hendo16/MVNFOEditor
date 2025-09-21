using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVNFOEditor.Models
{
    public class Genre
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<MusicVideo> MusicVideos { get; } = [];

        public Genre() {}

        public Genre(string name, MusicVideo newVm)
        {
            Name = name;
            MusicVideos.Add(newVm);
        }

    }
}
