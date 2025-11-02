using System.Collections.Generic;

namespace MVNFOEditor.Models;

public class Genre
{
    public Genre()
    {
    }

    public Genre(string name, MusicVideo newVm)
    {
        Name = name;
        MusicVideos.Add(newVm);
    }

    public int Id { get; set; }
    public string Name { get; set; }

    public List<MusicVideo> MusicVideos { get; } = [];
}