using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MVNFOEditor.Models;

namespace MVNFOEditor.Helpers;

public static class ResultHelper
{
    public static async Task<Artist?> ArtistResultHelper(ArtistResult currResult)
    {
        Artist? newArtist;
        var dbContext = App.GetDBContext();
        //Prevent duplicates being stored
        if (!dbContext.ArtistMetadata.Any(am =>
                am.SourceId == currResult.Source && am.BrowseId == currResult.SourceId))
        {
            newArtist = await Artist.CreateArtist(currResult);
        }
        else
        {
            newArtist = dbContext.ArtistMetadata.Include(am => am.Artist).First(am =>
                am.SourceId == currResult.Source && am.BrowseId == currResult.SourceId).Artist;
        }

        return newArtist;
    }

    public static async Task<Artist?> ArtistResultHelper(string artistName)
    {
        Artist? newArtist;
        var dbContext = App.GetDBContext();
        if (!dbContext.Artist.Any(a =>
                a.Name == artistName))
        {
            newArtist = await Artist.CreateArtist(artistName);
        }
        else
        {
            newArtist = dbContext.Artist.First(a => a.Name == artistName);
        }

        return newArtist;
    }

    public static async Task<Album?> AlbumResultHelper(AlbumResult currResult)
    {
        Album? album;
        var dbContext = App.GetDBContext();
        if (!dbContext.AlbumMetadata.Any(am =>
                am.SourceId == currResult.Source && am.BrowseId == currResult.SourceId))
        {
            album = await Album.CreateAlbum(currResult, currResult.Source);
        }
        else
        {
            album = dbContext.AlbumMetadata.Include(am => am.Album).First(am =>
                am.SourceId == currResult.Source && am.BrowseId == currResult.SourceId).Album;
        }

        return album;
    }
}