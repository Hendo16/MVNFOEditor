using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MVNFOEditor.Helpers;
using MVNFOEditor.Models;
using MVNFOEditor.ViewModels;
using Newtonsoft.Json.Linq;

namespace MVNFOEditor.Views;

public partial class MainView : UserControl
{
    private List<MusicVideo> musicVideoList;
    private MainViewModel mainViewModel;
    private YTMusicHelper ytMusicHelper;
    private YTDLHelper ytDLHelper;
    private SettingsData settingsData;
    public MainView()
    {
        InitializeComponent();
        ytMusicHelper = new YTMusicHelper();
        ytDLHelper = new YTDLHelper();
        DataContextChanged += CheckIfPathExists;
    }

    private void HandleRootFolderChanged(object sender, string folder)
    {
        settingsData = mainViewModel.MVDBContext.SettingsData.SingleOrDefault();
        //RootFolder changed in MainViewModel so we come here to load the info (events need a secondary args so I just made it the folder)
        GenerateMVList(mainViewModel.RootFolder);
        musicVideoList = mainViewModel.MVDBContext.MusicVideos.ToList();
        ArtistLoadHandler();
    }

    public void CheckIfPathExists(object sender, EventArgs args)
    {
        //DataContextChanged triggered so we can now safely access the ViewModel
        mainViewModel = (MainViewModel)DataContext;

        mainViewModel.MVDBContext.Database.EnsureCreated();
        settingsData = mainViewModel.MVDBContext.SettingsData.SingleOrDefault();
        //Assign RootFolderChaned event to MainViewModel so we can come back and re-load Artists
        mainViewModel.RootFolderChanged += HandleRootFolderChanged;
        if (mainViewModel.MVDBContext.MusicVideos != null)
        {
            musicVideoList = mainViewModel.MVDBContext.MusicVideos.ToList();
        }
        else
        {
            musicVideoList = new List<MusicVideo>();
        }

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
            albumCard.ArtistBtn.Click += (sender, args) => { GetSongsByAlbum(album, artist); };
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
            Button vidDown = new Button();
            videoLabel.Content = $"{CleanYTName(vid["title"].ToString())}";
            if (artistCollection.Exists(e => e.title.ToLower() == CleanYTName(vid["title"].ToString()).ToLower()))
            {
                videoLabel.Background = Brush.Parse("Green");
            }
            else
            {
                videoLabel.Background = Brush.Parse("Red");
            }

            vidDown.Content = "Download";
            vidDown.Click += (sender, args) => { ytDLHelper.DownloadVideo(vid["videoId"].ToString()); };
            SongList.Children.Add(videoLabel);
            SongList.Children.Add(vidDown);
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

        Grid SongInfoGrid = new Grid();
        SongInfoGrid.ColumnDefinitions = ColumnDefinitions.Parse("Auto,Auto,Auto");
        SongInfoGrid.RowDefinitions = RowDefinitions.Parse("Auto,Auto,Auto,Auto,Auto");

        #region title
        Label titleLabel = new Label();
        titleLabel.Content = "Title: ";
        Grid.SetRow(titleLabel, 0);
        Grid.SetColumn(titleLabel, 0);

        TextBox titleBox = new TextBox();
        titleBox.Name = "titleBox";
        titleBox.Text = video.title;
        Grid.SetRow(titleBox, 0);
        Grid.SetColumn(titleBox, 1);
        SongInfo.Children.Add(titleLabel);
        SongInfo.Children.Add(titleBox);
        #endregion

        #region year
        Label yearLabel = new Label();
        yearLabel.Content = "Year: ";
        Grid.SetRow(yearLabel, 1);
        Grid.SetColumn(yearLabel, 0);

        TextBox yearBox = new TextBox();
        yearBox.Name = "yearBox";
        yearBox.Text = video.year;
        Grid.SetRow(yearBox, 1);
        Grid.SetColumn(yearBox, 1);
        SongInfo.Children.Add(yearLabel);
        SongInfo.Children.Add(yearBox);
        #endregion

        #region album
        Label albumLabel = new Label();
        albumLabel.Content = "Album: ";
        Grid.SetRow(albumLabel, 2);
        Grid.SetColumn(albumLabel, 0);

        TextBox albumBox = new TextBox();
        albumBox.Name = "albumBox";
        albumBox.Text = video.album;
        Grid.SetRow(yearBox, 2);
        Grid.SetColumn(yearBox, 1);
        SongInfo.Children.Add(albumLabel);
        SongInfo.Children.Add(albumBox);
        #endregion

        Button saveBtn = new Button(){Content = "Save", Background = Brush.Parse("Green")};
        saveBtn.Click += (sender, args) => { SaveNFO(video); };
        
        if (video.MusicVideoGenres != null)
        {
            Label genreHeading = new Label();
            genreHeading.Content = "Genres:";
            SongInfo.Children.Add(genreHeading);
            foreach (var genre in video.MusicVideoGenres)
            {
                Label currGenre = new Label();
                currGenre.Content = $"\t{genre.Genre.Name}";
                SongInfo.Children.Add(currGenre);
            }
        }
        SongInfo.Children.Add(saveBtn);
    }

    private void SaveNFO(MusicVideo vid)
    {
        vid.title = ((TextBox)SongInfo.Children[1]).Text;
        vid.year = ((TextBox)SongInfo.Children[3]).Text;
        vid.album = ((TextBox)SongInfo.Children[5]).Text;
        //Refresh data
        UpdateMV(vid);
    }

    private void GenerateMVList(string rootFolder)
    {
        string[] nfoList = Directory.GetFiles(rootFolder, "*.nfo", SearchOption.AllDirectories);
        settingsData.RootFolder = rootFolder;
        for (int i = 0; i < nfoList.Length; i++)
        {
            string currNFO = nfoList[i];
            if (!currNFO.Contains("artist.nfo"))
            {
                XmlDocument nfoDoc = new XmlDocument();
                StreamReader reader = new StreamReader(currNFO);
                
                nfoDoc.Load(reader);
                MusicVideo newVid = MapNfoToMusicVideo(nfoDoc, currNFO);
                
                reader.Close();
                //musicVideoList.Add(newVid);
                mainViewModel.MVDBContext.MusicVideos.Add(newVid);
            }
        }

        mainViewModel.MVDBContext.SaveChanges();
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
        video.filePath = origPath;
        video.videoID = "";

        //Generate Genre and link to MusicVideo object
        for (int i = 0; i < genreNodes.Count; i++)
        {
            MusicVideoGenre mvGenre = new MusicVideoGenre();
            
            Genre? storedGenre = mainViewModel.MVDBContext.Genres.Count() != 0 ? mainViewModel.MVDBContext.Genres.First(e => e.Name == genreNodes[i].InnerText) : null;
            if (storedGenre == null)
            {
                Genre currGenre = new Genre();
                currGenre.Name = genreNodes[i].InnerText;
                mvGenre.Genre = currGenre;
                mainViewModel.MVDBContext.Genres.Add(currGenre);
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

    private void UpdateMV(MusicVideo vid)
    {
        var path = $"{vid.filePath}";
        XDocument x = XDocument.Load(path);

        foreach (XElement el in x.Descendants())
        {
            if (el.Name == "title")
            {
                el.Value = vid.title;
            }
            else if (el.Name == "year")
            {
                el.Value = vid.year;
            }
            else if (el.Name == "album")
            {
                el.Value = vid.album;
            }
        }
        x.Save(path);
        
        var updatedVid = mainViewModel.MVDBContext.MusicVideos.SingleOrDefault(e => e.Id == vid.Id);
        updatedVid.studio = vid.studio;
        mainViewModel.MVDBContext.SaveChanges();

        Debug.WriteLine("DONE!");
    }

    private string CleanYTName(string name)
    {
        return name.Replace(" (Official Video)", "")
            .Replace(" (Official Music Video)", "")
            .Replace(" (Official HD Video)", "");
    }
}
