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
using DynamicData;
using Microsoft.EntityFrameworkCore;
using MVNFOEditor.DB;
using MVNFOEditor.Features;
using MVNFOEditor.Models;
using MVNFOEditor.ViewModels;
using MVNFOEditor.Views;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Helpers
{
    public delegate void ProgressUpdateDelegate(int progress);
    public class MusicDBHelper
    {
        private MusicDbContext _db;
        private YTMusicHelper ytMusicHelper;

        private List<Artist> initArtists = new List<Artist>();
        private List<Album> initAlbums = new List<Album>();
        public event ProgressUpdateDelegate ProgressUpdate;
        public MusicDBHelper(MusicDbContext db)
        {
            _db = db;
            ytMusicHelper = App.GetYTMusicHelper();
            if (_db.MusicVideos.Count() == 0)
            {
                if (CheckIfSettingsValid())
                {
                    InitilizeData();
                }
                else
                {
                    SettingsData setData = new SettingsData();
                    setData.RootFolder = "null";
                    setData.FFMPEGPath = "null";
                    setData.YTDLPath = "null";
                    setData.AnimatedBackground = false;
                    db.SettingsData.Add(setData);
                    db.SaveChanges();
                }
            }
        }

        public bool CheckIfSettingsValid()
        {
            if (_db.SettingsData.Any())
            {
                return !_db.SettingsData.FirstOrDefault().RootFolder.Equals("null");
            }

            return false;
        }

        public async Task<int> InitilizeData()
        {
            SettingsData setData = _db.SettingsData.SingleOrDefault();
            string[] nfoList = Directory.GetFiles(setData.RootFolder, "*.nfo", SearchOption.AllDirectories);
            //Create list we can add to, ensuring we don't duplicate anything
            for (int i = 0; i < nfoList.Length; i++)
            {
                string currNFO = nfoList[i];
                int currProgress = (int)Math.Round(((double)i / nfoList.Length) * 100);
                OnProgressUpdate(currProgress);
                if (!currNFO.Contains("artist.nfo"))
                {
                    XmlDocument nfoDoc = new XmlDocument();
                    StreamReader reader = new StreamReader(currNFO);

                    nfoDoc.Load(reader);
                    //Artist
                    Artist Artist = GetArtistFromNfo(nfoDoc, currNFO);
                    Album? Album = null;
                    //Album
                    if (nfoDoc.SelectSingleNode("//album") != null)
                    {
                        Album = GetAlbumFromNfo(nfoDoc, currNFO);
                        if (Album.Artist == null)
                        {
                            Album.Artist = Artist;
                            if (Artist.YTMusicId != "null")
                            {
                                JObject albumObj = ytMusicHelper.GetAlbumObj(Album.Title, Artist);
                                if (albumObj != null)
                                {
                                    Album.ArtURL = ytMusicHelper.GetHighQualityArt(albumObj);
                                    Album.ytMusicBrowseID = albumObj["browseId"].ToString();
                                }
                            }
                            initAlbums.Add(Album);
                        }
                    }
                    //Music Video
                    MusicVideo newVid = MapNfoToMusicVideo(nfoDoc, currNFO);
                    newVid.artist = Artist;
                    newVid.album = Album;

                    reader.Close();
                    _db.MusicVideos.Add(newVid);
                }
            }

            for (int i = 0; i < initArtists.Count; i++)
            {
                Artist currArt = initArtists[i];
                _db.Artist.Add(currArt);
            }

            for (int i = 0; i < initAlbums.Count; i++)
            {
                Album currAlbum = initAlbums[i];
                _db.Album.Add(currAlbum);
            }

            return await _db.SaveChangesAsync();
        }

        public async Task<ObservableCollection<ArtistViewModel>> GenerateArtists()
        {
            List<ArtistViewModel> artists = new List<ArtistViewModel>();
            foreach (Artist artist in _db.Artist.ToList())
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

        private Artist GetArtistFromNfo(XmlDocument songDoc, string origPath)
        {
            Artist artistObj = new Artist();
            XmlNode artistNode = songDoc.SelectSingleNode("//artist");
            string artist = artistNode != null ? artistNode.InnerText : "null";
            if (!initArtists.Any(a => a.Name == artist))
            {
                //Getting the artists banner
                string artistID = ytMusicHelper.get_artistID(artist);

                artistObj.YTMusicId = artistID;
                artistObj.Name = artist;
                if (artistID != "null")
                {
                    artistObj.CardBannerURL = ytMusicHelper.GetArtistBanner(artistID, 540);
                    artistObj.LargeBannerURL = ytMusicHelper.GetArtistBanner(artistID, 1080);
                    artistObj.YTMusicAlbumResults = ytMusicHelper.GetAlbums(artistID);
                }
                initArtists.Add(artistObj);
            }
            else
            {
                artistObj = initArtists.First(a => a.Name == artist);
            }
            return artistObj;
        }

        private Album GetAlbumFromNfo(XmlDocument songDoc, string origPath)
        {
            Album albumObj = new Album();
            XmlNode albumNode = songDoc.SelectSingleNode("//album");
            XmlNode yearNode = songDoc.SelectSingleNode("//year");
            string albumName = albumNode.InnerText;
            string year = yearNode != null ? yearNode.InnerText : "null";
            if (!initAlbums.Any(a => a.Title == albumName))
            {
                albumObj.Title = albumName;
                albumObj.Year = year;
            }
            else
            {
                albumObj = initAlbums.First(a => a.Title == albumName);
            }
            return albumObj;
        }

        private MusicVideo MapNfoToMusicVideo(XmlDocument songDoc, string origPath)
        {
            MusicVideo video = new MusicVideo();

            #region XmlNodes

            XmlNode titleNode = songDoc.SelectSingleNode("//title");
            XmlNode userRatingNode = songDoc.SelectSingleNode("//userrating");
            XmlNode trackNode = songDoc.SelectSingleNode("//track");
            XmlNode studioNode = songDoc.SelectSingleNode("//studio");
            XmlNode premiereNode = songDoc.SelectSingleNode("//premiered");
            XmlNode yearNode = songDoc.SelectSingleNode("//year");
            XmlNode thumbNode = songDoc.SelectSingleNode("//thumb");
            XmlNode sourceNode = songDoc.SelectSingleNode("//source");
            XmlNode mbIDNode = songDoc.SelectSingleNode("//musicBrainzArtistID");

            XmlNodeList genreNodes = songDoc.SelectNodes("//genre");
            #endregion

            #region NodeToVariable
            string title = titleNode != null ? titleNode.InnerText : "null";
            string userRating = userRatingNode != null ? userRatingNode.InnerText : "null";
            string track = trackNode != null ? trackNode.InnerText : "null";
            string studio = studioNode != null ? studioNode.InnerText : "null";
            string premiere = premiereNode != null ? premiereNode.InnerText : "null";
            string year = yearNode != null ? yearNode.InnerText : "null";
            string thumb = thumbNode != null ? thumbNode.InnerText : "null";
            string source = sourceNode != null ? sourceNode.InnerText : "null";
            string mbID = mbIDNode != null ? mbIDNode.InnerText : "null";

            List<MusicVideoGenre> mvGenres = new List<MusicVideoGenre>();

            #endregion

            #region MappingToMusicVideo
            video.title = title;
            video.userrating = userRating;
            video.track = track;
            video.studio = studio;
            video.premiered = premiere;
            video.year = year;
            video.thumb = thumb;
            video.source = source;
            video.musicBrainzArtistID = mbID;
            video.nfoPath = origPath;
            video.videoID = "";

            //We can't assume what the video filenames are going to be, so we guess it'll be something with the same name
            var vidFiles = Directory.GetFiles(Path.GetDirectoryName(origPath) + "/", Path.GetFileNameWithoutExtension(origPath) + ".*");
            var vidPath = vidFiles.Where(e => (!e.Contains(".jpg") && !e.Contains(".nfo"))).ToList()[0];

            if (vidPath != null)
            {
                video.vidPath = vidPath;
            }

            //Generate Genre and link to MusicVideo object
            for (int i = 0; i < genreNodes.Count; i++)
            {
                MusicVideoGenre mvGenre = new MusicVideoGenre();

                Genre? storedGenre = _db.Genres.Count() != 0 ? _db.Genres.First(e => e.Name == genreNodes[i].InnerText) : null;
                if (storedGenre == null)
                {
                    Genre currGenre = new Genre();
                    currGenre.Name = genreNodes[i].InnerText;
                    mvGenre.Genre = currGenre;
                    _db.Genres.Add(currGenre);
                }
                else
                {
                    mvGenre.Genre = storedGenre;
                }
                mvGenre.MusicVideo = video;
                mvGenres.Add(mvGenre);
            }
            video.MusicVideoGenres = mvGenres;
            #endregion

            return video;
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
            x.Save(path);

            var updatedVid = _db.MusicVideos.SingleOrDefault(e => e.Id == vid.Id);
            updatedVid.studio = vid.studio;
            return await _db.SaveChangesAsync();
        }

        public async Task<int> UpdateAlbum(Album album)
        {
            //Cycle through and update all videos
            List<MusicVideo> vidList = _db.MusicVideos.Where(mv => mv.album.Id == album.Id).ToList();
            if (vidList.Count > 0 && vidList[0].year != album.Year)
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
            var videoPath = Directory.GetFiles(folderPath + "/", vid.title + "-video.*");
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
            //Remove Album
            _db.Artist.Remove(artist);
            _db.SaveChanges();
        }

        private string CleanseString(string str)
        {
            return str.Replace("&", "&amp;");
        }

        protected virtual void OnProgressUpdate(int progress)
        {
            ProgressUpdate?.Invoke(progress);
        }
    }
}