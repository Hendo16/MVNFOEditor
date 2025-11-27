using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using MVNFOEditor.DB;
using MVNFOEditor.Models;
using MVNFOEditor.Settings;
using MVNFOEditor.ViewModels;

namespace MVNFOEditor.Helpers;

public delegate void ProgressUpdateDelegate(int progress);

public class MusicDBHelper
{
    private static ISettings _settings;
    private readonly MusicDbContext _db;
    private List<Album> initAlbums = new();

    private List<Artist> initArtists = new();
    private YTMusicHelper ytMusicHelper;

    private MusicDBHelper(MusicDbContext db)
    {
        _db = db;
        _settings = App.GetSettings();
        ytMusicHelper = App.GetYTMusicHelper();
    }

    public event ProgressUpdateDelegate ProgressUpdate;

    public static MusicDBHelper CreateHelper(MusicDbContext db)
    {
        var newHelper = new MusicDBHelper(db);
        return newHelper;
    }

    public async Task<ObservableCollection<ArtistViewModel>> GenerateArtists()
    {
        IEnumerable<ArtistViewModel> artists =
            await ArtistViewModel.GenerateViewModels(_db.Artist.Include(a => a.Metadata));
        return new ObservableCollection<ArtistViewModel>(artists.OrderBy(a => a.Name));
    }

    public async Task<ObservableCollection<AlbumViewModel>> GenerateAlbums(Artist artist)
    {
        var albums = new List<AlbumViewModel>();
        var albumList = _db.Album.Include(a => a.Metadata).Where(e => e.Artist.Id == artist.Id).ToList();
        foreach (var album in albumList)
        {
            var newVM = new AlbumViewModel(album);
            albums.Add(newVM);
            await newVM.LoadCover();
            if (!album.IsArtSaved()) await newVM.SaveCoverAsync();
        }

        return new ObservableCollection<AlbumViewModel>(albums.OrderBy(a => a.Year));
    }

    public async Task<ObservableCollection<VideoCardViewModel>> GenerateSingles(Artist artist)
    {
        var singles = new List<VideoCardViewModel>();
        var singleList = _db.MusicVideos.Where(e => e.artist.Id == artist.Id && e.album == null).ToList();
        foreach (var single in singleList)
        {
            var newVM = new VideoCardViewModel(single);
            singles.Add(newVM);
            await newVM.LoadThumbnail();
        }

        return new ObservableCollection<VideoCardViewModel>(singles.OrderByDescending(a => a.Year));
    }

    public async Task<ObservableCollection<VideoCardViewModel>> GenerateAllSingles()
    {
        var singles = new List<VideoCardViewModel>();
        var singleList = _db.MusicVideos.ToList();
        foreach (var single in singleList)
        {
            var newVM = new VideoCardViewModel(single);
            singles.Add(newVM);
            await newVM.LoadThumbnail();
        }

        return new ObservableCollection<VideoCardViewModel>(singles.OrderBy(a => a.Title));
    }

    public List<Album> GetAlbums(int artistId)
    {
        return _db.Album.Include(a => a.Metadata).Where(a => a.Artist.Id == artistId).ToList();
    }

    public async Task<int> UpdateMusicVideo(MusicVideo vid)
    {
        var path = $"{vid.nfoPath}";
        var x = XDocument.Load(path);

        foreach (var el in x.Descendants())
        {
            if (el.Name == "title" && vid.title != el.Value) el.Value = vid.title;
            if (el.Name == "year" && vid.year != el.Value) el.Value = vid.year;
            if (el.Name == "artist" && vid.artist.Name != el.Value) el.Value = vid.artist.Name;
            if (el.Name == "thumb" && vid.thumb != el.Value) el.Value = vid.thumb;
            if (el.Name == "album" && vid.album == null)
                el.Value = "";
            else if (el.Name == "album" && vid.album.Title != el.Value) el.Value = vid.album.Title;
        }

        if (!x.Descendants().Any(e => e.Name == "album") && vid.album != null)
        {
            var new_node = $"<album>{vid.album.Title}</album>";
            var new_el = new XElement("album", vid.album.Title);
            x.Root.Add(new_el);
        }

        x.Save(path);

        var updatedVid = _db.MusicVideos.SingleOrDefault(e => e.Id == vid.Id);
        updatedVid.studio = vid.studio;
        return await _db.SaveChangesAsync();
    }

    public async Task<int> UpdateAlbum(Album album)
    {
        //Cycle through and update all videos
        var vidList = _db.MusicVideos.Where(mv => mv.album.Id == album.Id).ToList();
        if (vidList.Count > 0)
            for (var i = 0; i < vidList.Count; i++)
            {
                var currVid = vidList[i];
                currVid.year = album.Year;
                await UpdateMusicVideo(currVid);
            }

        //Update Album
        _db.Album.Update(album);
        return await _db.SaveChangesAsync();
    }

    public async Task<int> UpdateArtist(Artist artist)
    {
        //Update Artist
        _db.Artist.Update(artist);
        return await _db.SaveChangesAsync();
    }

    public int DeleteVideo(MusicVideo vid)
    {
        var nfoPath = vid.nfoPath;
        if (File.Exists(nfoPath)) File.Delete(nfoPath);
        var folderPath = Path.GetDirectoryName(nfoPath);
        var thumbFileName = folderPath + "/" + vid.thumb;
        if (File.Exists(thumbFileName)) File.Delete(thumbFileName);

        //Get Video
        var videoPath = Directory.GetFiles(folderPath + "/", $"{vid.title}.*");
        if (videoPath.Length > 0) File.Delete(videoPath[0]);

        //Remove MusicVideo Object
        _db.MusicVideos.Remove(vid);
        return _db.SaveChanges();
    }

    public void DeleteAlbum(Album album)
    {
        //Remove All Associated Videos
        var vidList = _db.MusicVideos.Where(mv => mv.album.Id == album.Id).ToList();
        for (var i = 0; i < vidList.Count; i++)
        {
            var currVid = vidList[i];
            DeleteVideo(currVid);
        }
        //Remove associated metadata
        var metadataList = _db.AlbumMetadata.Where(am => am.Album.Id == album.Id);
        foreach (var metadata in metadataList)
        {
            _db.AlbumMetadata.Remove(metadata);
        }

        //Delete downloaded cover art
        if (File.Exists($"./Cache/{album.Artist.Name}/{album.Title}.jpg"))
        {
            File.Delete($"./Cache/{album.Artist.Name}/{album.Title}.jpg");
        }
        //Remove Album
        _db.Album.Remove(album);
        _db.SaveChanges();
    }

    public void DeleteArtist(Artist artist)
    {
        //Remove All Associated Videos
        var vidList = _db.MusicVideos.Where(mv => mv.artist.Id == artist.Id).ToList();
        foreach (var vid in vidList)
        {
            DeleteVideo(vid);
        }
        //Remove all associated albums
        var albList = _db.Album.Where(alb => alb.Artist.Id == artist.Id);
        foreach (var alb in albList)
        {
            DeleteAlbum(alb);
        }
        //Remove associated metadata
        var metadataList = _db.ArtistMetadata.Where(am => am.Artist.Id == artist.Id);
        foreach (var metadata in metadataList)
        {
            _db.ArtistMetadata.Remove(metadata);
        }
        //Clear out downloaded images
        if (Directory.Exists($"./Cache/{artist.Name}"))
        {
            Directory.Delete($"./Cache/{artist.Name}", true);
        }
        //Remove Artist
        _db.Artist.Remove(artist);
        _db.SaveChanges();
    }

    protected virtual void OnProgressUpdate(int progress)
    {
        ProgressUpdate?.Invoke(progress);
    }
}