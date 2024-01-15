using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MVNFOEditor.Models
{
    public class MusicVideo
    {
        public int Id { get; set; }
        public string title { get; set; }
        public string userrating { get; set; }
        public string track { get; set; }
        public string studio { get; set; }
        public List<MusicVideoGenre> MusicVideoGenres { get; set; }
        public string premiered { get; set; }
        public string year { get; set; }
        public string artist { get; set; }
        public string album { get; set; }
        public string thumb { get; set; }
        public string source { get; set; }
        public string musicBrainzArtistID { get; set; }
        public string videoID { get; set; }
    }
}
