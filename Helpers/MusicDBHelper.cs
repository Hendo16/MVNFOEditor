using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using MVNFOEditor.DB;
using MVNFOEditor.Features;
using MVNFOEditor.Models;
using MVNFOEditor.Settings;
using MVNFOEditor.ViewModels;
using MVNFOEditor.Views;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Helpers
{
    public delegate void ProgressUpdateDelegate(int progress);
    public class MusicDBHelper
    {
        private MusicDbContext _db;
        private static ISettings _settings;
        private YTMusicHelper ytMusicHelper;

        private List<Artist> initArtists = new List<Artist>();
        private List<Album> initAlbums = new List<Album>();
        public event ProgressUpdateDelegate ProgressUpdate;
        public MusicDBHelper(MusicDbContext db)
        {
            _db = db;
            _settings = App.GetSettings();
            ytMusicHelper = App.GetYTMusicHelper();
        }

        public async Task<ObservableCollection<ArtistViewModel>> GenerateArtists()
        {
            List<ArtistViewModel> artists = new List<ArtistViewModel>();
            foreach (Artist artist in _db.Artist.Include(a => a.Metadata).ToList())
            {
                ArtistViewModel newVM = new ArtistViewModel(artist);
                artists.Add(newVM);
                await newVM.LoadCover();

                if (!artist.IsCardSaved())
                {
                    await newVM.SaveCoverAsync();
                }

                if (!artist.IsBannerSaved())
                {
                    await newVM.SaveLargeBannerAsync();
                }
            }
            return new ObservableCollection<ArtistViewModel>(artists.OrderBy(a => a.Name));
        }

        public async Task<ObservableCollection<AlbumViewModel>> GenerateAlbums(Artist artist)
        {
            List<AlbumViewModel> albums = new List<AlbumViewModel>();
            List<Album> albumList = _db.Album.Where(e => e.Artist.Id == artist.Id).ToList();
            foreach (Album album in albumList)
            {
                AlbumViewModel newVM = new AlbumViewModel(album);
                albums.Add(newVM);
                await newVM.LoadCover();
                if (!album.IsArtSaved()) { await newVM.SaveCoverAsync(); }
            }
            return new ObservableCollection<AlbumViewModel>(albums.OrderBy(a => a.Year));
        }

        public async Task<ObservableCollection<SingleViewModel>> GenerateSingles(Artist artist)
        {
            List<SingleViewModel> singles = new List<SingleViewModel>();
            List<MusicVideo> singleList = _db.MusicVideos.Where(e => e.artist.Id == artist.Id && e.album == null).ToList();
            foreach (MusicVideo single in singleList)
            {
                SingleViewModel newVM = new SingleViewModel(single);
                singles.Add(newVM);
                await newVM.LoadThumbnail();
            }
            return new ObservableCollection<SingleViewModel>(singles.OrderByDescending(a => a.Year));
        }

        public async Task<ObservableCollection<SingleViewModel>> GenerateAllSingles()
        {
            List<SingleViewModel> singles = new List<SingleViewModel>();
            List<MusicVideo> singleList = _db.MusicVideos.ToList();
            foreach (MusicVideo single in singleList)
            {
                SingleViewModel newVM = new SingleViewModel(single);
                singles.Add(newVM);
                await newVM.LoadThumbnail();
            }
            return new ObservableCollection<SingleViewModel>(singles.OrderBy(a => a.Title));
        }

        public List<Album> GetAlbums(int artistId)
        {
            return _db.Album.Where(a => a.Artist.Id == artistId).ToList();
        }

        public async Task<int> UpdateMusicVideo(MusicVideo vid)
        {
            var path = $"{vid.nfoPath}";
            XDocument x = XDocument.Load(path);

            foreach (XElement el in x.Descendants())
            {
                if (el.Name == "title" && vid.title != el.Value)
                {
                    el.Value = vid.title;
                }
                if (el.Name == "year" && vid.year != el.Value)
                {
                    el.Value = vid.year;
                }
                if (el.Name == "artist" && vid.artist.Name != el.Value)
                {
                    el.Value = vid.artist.Name;
                }
                if (el.Name == "thumb" && vid.thumb != el.Value)
                {
                    el.Value = vid.thumb;
                }
                if (el.Name == "album" && vid.album == null)
                {
                    el.Value = "";
                }
                else if (el.Name == "album" && vid.album.Title != el.Value)
                {
                    el.Value = vid.album.Title;
                }
            }

            if (!x.Descendants().Any(e => e.Name == "album") && vid.album != null)
            {
                string new_node = $"<album>{vid.album.Title}</album>";
                XElement new_el = new XElement("album", vid.album.Title);
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
            List<MusicVideo> vidList = _db.MusicVideos.Where(mv => mv.album.Id == album.Id).ToList();
            if (vidList.Count > 0)
            {
                for (int i = 0; i < vidList.Count; i++)
                {
                    MusicVideo currVid = vidList[i];
                    currVid.year = album.Year;
                    await UpdateMusicVideo(currVid);
                }
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
            if (File.Exists(nfoPath)) { File.Delete(nfoPath); }
            var folderPath = Path.GetDirectoryName(nfoPath);
            var thumbFileName = folderPath + "/" + vid.thumb;
            if (File.Exists(thumbFileName)) { File.Delete(thumbFileName); }

            //Get Video
            var videoPath = Directory.GetFiles(folderPath + "/",$"{vid.title}.*");
            if (videoPath.Length > 0) {File.Delete(videoPath[0]);}

            //Remove MusicVideo Object
            _db.MusicVideos.Remove(vid);
            return _db.SaveChanges();
        }

        public void DeleteAlbum(Album album)
        {
            //Remove All Associated Videos
            List<MusicVideo> vidList = _db.MusicVideos.Where(mv => mv.album.Id == album.Id).ToList();
            for (int i = 0; i < vidList.Count; i++)
            {
                MusicVideo currVid = vidList[i];
                DeleteVideo(currVid);
            }
            //Delete downloaded cover art
            File.Delete($"./Cache/{album.Artist.Name}/{album.Title}.jpg");
            //Remove Album
            _db.Album.Remove(album);
            _db.SaveChanges();
        }

        public void DeleteArtist(Artist artist)
        {
            //Remove All Associated Videos
            List<MusicVideo> vidList = _db.MusicVideos.Where(mv => mv.artist.Id == artist.Id).ToList();
            for (int i = 0; i < vidList.Count; i++)
            {
                MusicVideo currVid = vidList[i];
                DeleteVideo(currVid);
            }
            //Clear out downloaded images
            Directory.Delete($"./Cache/{artist.Name}", true);
            //Remove Artist
            _db.Artist.Remove(artist);
            _db.SaveChanges();
        }

        protected virtual void OnProgressUpdate(int progress)
        {
            ProgressUpdate?.Invoke(progress);
        }
    }
}