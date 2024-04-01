using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using SukiUI.Controls;

namespace MVNFOEditor.Models
{
    public class MusicVideo
    {
        public int Id { get; set; }
        public string title { get; set; }
        public Artist artist { get; set; }
        public Album? album { get; set; }
        public string? vidPath { get; set; }
        public string? thumb { get; set; }
        public string nfoPath { get; set; }
        public string year { get; set; }
        public List<MusicVideoGenre> MusicVideoGenres { get; set; }
        public string? videoID { get; set; }
        public string? userrating { get; set; }
        public string? track { get; set; }
        public string? studio { get; set; }
        public string? premiered { get; set; }
        public string? source { get; set; }
        public string? musicBrainzArtistID { get; set; }

        public async Task<Stream?> LoadThumbnailBitmapAsync()
        {
            var RootFolder = Path.GetDirectoryName(nfoPath);
            if (thumb != "null" && thumb != "")
            {
                try
                {
                    return File.OpenRead(RootFolder + $"/{thumb}");
                }
                catch (FileNotFoundException e)
                {
                    Debug.WriteLine($"Couldn't find {thumb}");
                }
            }
            return null;
        }

        public void SaveToNFO()
        {
            XDocument xDoc = new XDocument();

            XElement parentEl = new XElement("musicvideo");

            XElement titleEl = new XElement("title");
            titleEl.Value = title;
            parentEl.Add(titleEl);

            XElement artistEl = new XElement("artist");
            artistEl.Value = artist.Name;
            parentEl.Add(artistEl);

            if (album != null)
            {
                XElement albumEl = new XElement("album");
                albumEl.Value = album.Title;
                parentEl.Add(albumEl);
            }

            XElement yearEl = new XElement("year");
            yearEl.Value = year;
            parentEl.Add(yearEl);

            XElement thumbEl = new XElement("thumb");
            thumbEl.Value = thumb != null ? thumb : "null";
            parentEl.Add(thumbEl);

            XElement sourceEl = new XElement("source");
            sourceEl.Value = source;
            parentEl.Add(sourceEl);

            XElement videoIDEl = new XElement("videoID");
            videoIDEl.Value = videoID != null ? videoID : "null";
            parentEl.Add(videoIDEl);

            xDoc.Add(parentEl);
            xDoc.Save(nfoPath);
        }
    }
}