using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MVNFOEditor.DB;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using MVNFOEditor.ViewModels;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace MVNFOEditor.Views;

public partial class MainView : UserControl
{
    private List<MusicVideo> musicVideoList;
    private MainViewModel mainViewModel;
    private MusicDbContext db;
    private YTMusicHelper ytMusicHelper;
    public MainView()
    {
        InitializeComponent();
        db = new MusicDbContext();
        db.Database.EnsureCreated();
        if (db.MusicVideos != null)
        {
            musicVideoList = db.MusicVideos.ToList();
        }
        else
        {
            musicVideoList = new List<MusicVideo>();
        }
        ytMusicHelper = new YTMusicHelper();
        DataContextChanged += CheckIfPathExists;
    }

    private void HandleRootFolderChanged(object sender, string folder)
    {
        //RootFolder changed in MainViewModel so we come here to load the info (events need a secondary args so I just made it the folder)
        GenerateMVList(mainViewModel.RootFolder);
        musicVideoList = db.MusicVideos.ToList();
        ArtistLoadHandler();
    }

    public void CheckIfPathExists(object sender, EventArgs args)
    {
        //DataContextChanged triggered so we can now safely access the ViewModel
        mainViewModel = (MainViewModel)DataContext;
        //Assign RootFolderChaned event to MainViewModel so we can come back and re-load Artists
        mainViewModel.RootFolderChanged += HandleRootFolderChanged;

        //if (mainViewModel.RootFolder == null)
        if (musicVideoList.Count == 0)
        {
            var settingsViewModel = new SettingsViewModel(mainViewModel);
            Settings setPanel = new Settings
            {
                DataContext = settingsViewModel
            };

            setPanel.Show();
        }
        else
        {
            ArtistLoadHandler();
        }
    }

    public void ArtistLoadHandler()
    {
        List<string> artistList = musicVideoList.Select(e => e.artist).Distinct().ToList();

        int row = 0;
        int column = 0;

        foreach (string artist in artistList)
        {
            //Button newArtistButt = new Button() {Content = artist };
            Card newArtistCard = new Card();
            CardViewModel artistCardVM = new CardViewModel();

            artistCardVM.ArtistName = artist;

            newArtistCard.ArtistBtn.Click += (o, eventArgs) => { GetVideoList(artist); };
            Grid.SetColumn(newArtistCard, column);
            Grid.SetRow(newArtistCard, row);

            column++;
            if (column >= ArtistGrid.ColumnDefinitions.Count)
            {
                // If the column exceeds the defined columns, reset column to 0 and increment row
                column = 0;
                row++;
                // Add a new row definition if needed
                ArtistGrid.RowDefinitions.Add(new RowDefinition());
            }
            newArtistCard.DataContext = artistCardVM;
            ArtistGrid.Children.Add(newArtistCard);
        }
    }

    public void GetVideoList(string artist)
    {
        AlbumList.Children.Clear();
        SongList.Children.Clear();
        SongInfo.Children.Clear();
        List<MusicVideo> currVideos = musicVideoList.FindAll(e => e.artist == artist);
        List<string> albums = currVideos.Select(e => e.album).Distinct().ToList();

        foreach (var album in albums)
        {
            //Button albumButton = new Button();

            Card albumCard = new Card();
            CardViewModel albumCardViewModel = new CardViewModel();

            if (album == "null")
            {
                //albumButton.Background = Brush.Parse("Orange");
                albumCardViewModel.ArtistName = "Singles";
            }
            else
            {
                albumCardViewModel.ArtistName = album;
            }
            //albumButton.Click += (sender, args) => { GetSongsByAlbum(album, artist); };
            albumCard.DataContext = albumCardViewModel;
            AlbumList.Children.Add(albumCard);
        }

        Button btnSyncTest = new Button();
        btnSyncTest.Content = "Sync Videos Test";
        btnSyncTest.Click += (sender, args) => { GetVideosByArtist(artist); };
        btnSyncTest.Background = Brush.Parse("Aqua");
        AlbumList.Children.Add(btnSyncTest);

    }

    public void GetVideosByArtist(string artist)
    {
        SongList.Children.Clear();
        SongInfo.Children.Clear();
        string artistID = ytMusicHelper.get_artistID(artist);
        JArray result = ytMusicHelper.get_videos(artistID);
        List<MusicVideo> artistCollection = musicVideoList.FindAll(e => e.artist == artist);
        foreach (JToken vid in result)
        {
            Label videoLabel = new Label();
            videoLabel.Content = $"{CleanYTName(vid["title"].ToString())} Link: https://www.youtube.com/watch?v={vid["videoId"]}";
            if (artistCollection.Exists(e => e.title.ToLower() == CleanYTName(vid["title"].ToString()).ToLower()))
            {
                videoLabel.Background = Brush.Parse("Green");
            }
            else
            {
                videoLabel.Background = Brush.Parse("Red");
            }
            SongList.Children.Add(videoLabel);
        }

    }

    public void GetSongsByAlbum(string album, string artist)
    {
        SongList.Children.Clear();
        SongInfo.Children.Clear();
        List<MusicVideo> albumVideos = GetVideosByAlbum(album, artist);

        foreach (MusicVideo vid in albumVideos)
        {
            Button songButton = new Button();

            if (vid.album == "null")
            {
                songButton.Background = Brush.Parse("Orange");
            }

            songButton.Content = vid.title;
            songButton.Click += (sender, args) => { GetSongInfo(vid); };
            SongList.Children.Add(songButton);
        }
    }

    public void GetSongInfo(MusicVideo video)
    {
        SongInfo.Children.Clear();
        Label albumLabel = new Label();
        albumLabel.Content = $"Album: {video.album}";

        Label sourceLabel = new Label();
        sourceLabel.Content = $"Source: {video.source}";

        Label yearLabel = new Label();
        yearLabel.Content = $"Year: {video.year}";

        Label genreHeading = new Label();
        genreHeading.Content = "Genres:";
        
        SongInfo.Children.Add(albumLabel);
        SongInfo.Children.Add(sourceLabel);
        SongInfo.Children.Add(yearLabel);
        SongInfo.Children.Add(genreHeading);
        foreach (var genre in video.MusicVideoGenres)
        {
            Label currGenre = new Label();
            currGenre.Content = $"\t{genre.Genre.Name}";
            SongInfo.Children.Add(currGenre);
        }
    }

    private void GenerateMVList(string rootFolder)
    {
        string[] nfoList = Directory.GetFiles(rootFolder, "*.nfo", SearchOption.AllDirectories);
        for (int i = 0; i < nfoList.Length; i++)
        {
            string currNFO = nfoList[i];
            if (!currNFO.Contains("artist.nfo"))
            {
                XmlDocument nfoDoc = new XmlDocument();
                StreamReader reader = new StreamReader(currNFO);
                
                nfoDoc.Load(reader);
                MusicVideo newVid = MapNfoToMusicVideo(nfoDoc);
                
                reader.Close();
                //musicVideoList.Add(newVid);
                db.MusicVideos.Add(newVid);
            }
        }

        db.SaveChanges();
    }

    private MusicVideo MapNfoToMusicVideo(XmlDocument songDoc)
    {
        MusicVideo video = new MusicVideo();

        #region XmlNodes

        XmlNode titleNode = songDoc.SelectSingleNode("//title");
        XmlNode userRatingNode = songDoc.SelectSingleNode("//userrating");
        XmlNode trackNode = songDoc.SelectSingleNode("//track");
        XmlNode studioNode = songDoc.SelectSingleNode("//studio");
        XmlNode premiereNode = songDoc.SelectSingleNode("//premiered");
        XmlNode yearNode = songDoc.SelectSingleNode("//year");
        XmlNode artistNode = songDoc.SelectSingleNode("//artist");
        XmlNode thumbNode = songDoc.SelectSingleNode("//thumb");
        XmlNode albumNode = songDoc.SelectSingleNode("//album");
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
        string artist = artistNode != null ? artistNode.InnerText : "null";
        string thumb = thumbNode != null ? thumbNode.InnerText : "null";
        string album = albumNode != null ? albumNode.InnerText : "null";
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
        video.artist = artist;
        video.thumb = thumb;
        video.album = album;
        video.source = source;
        video.musicBrainzArtistID = mbID;

        video.videoID = "";

        //Generate Genre and link to MusicVideo object
        for (int i = 0; i < genreNodes.Count; i++)
        {
            MusicVideoGenre mvGenre = new MusicVideoGenre();
            
            Genre? storedGenre = db.Genres.Count() != 0 ? db.Genres.First(e => e.Name == genreNodes[i].InnerText) : null;
            if (storedGenre == null)
            {
                Genre currGenre = new Genre();
                currGenre.Name = genreNodes[i].InnerText;
                mvGenre.Genre = currGenre;
                db.Genres.Add(currGenre);
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

    private List<MusicVideo> GetVideosByAlbum(string album, string artist)
    {
        return musicVideoList.FindAll(e => e.album == album && e.artist == artist);
    }

    private string CleanYTName(string name)
    {
        return name.Replace(" (Official Video)", "")
            .Replace(" (Official Music Video)", "")
            .Replace(" (Official HD Video)", "");
    }
}
