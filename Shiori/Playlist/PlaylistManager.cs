using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.IO;
using System.Windows; // messagebox
using libZPlay;
using Newtonsoft.Json;

namespace Shiori.Playlist
{
    class PlaylistManager
    {
        private Boolean IsSaved = true;
        private String _title = "Untitled Playlist";
        public String Title { get { return _title; } set { _title = value; IsSaved = false; } }
        public ObservableCollection<PlaylistElement> PlaylistElementsArray;
        private ZPlay player = new ZPlay();
        private DirectoryInfo mutualPath;
        private String _playlistPath;
        public int NowPlayingIndex { get; set; }
        public PlaylistElement CurrentElement { get { return PlaylistElementsArray[NowPlayingIndex]; } }

        public PlaylistManager()
        {
            PlaylistElementsArray = new ObservableCollection<PlaylistElement>();
        }
        public PlaylistManager(String playlistPath)
        {
            _playlistPath = playlistPath;
            PlaylistRecoverData _data = JsonConvert.DeserializeObject<PlaylistRecoverData>(File.ReadAllText(playlistPath));
            _title = _data.Title;
            PlaylistElementsArray = _data.Tracks;

            foreach (var i in PlaylistElementsArray)
            {
                i.RegenerateBookmarkPercent();
                i.RegeneratePercents();
            }
        }

        public void AddFile(String filePath)
        {
            if (!player.OpenFile(filePath, TStreamFormat.sfAutodetect))
            {
                Console.WriteLine("Unable to open file " + filePath + ". Error: " + player.GetError());
                return;
            }

            PlaylistElement emt = new PlaylistElement();
            emt.FilePath = filePath;
            UpdateMutualPath(filePath);

            TID3Info id3Info = new TID3Info();
            player.LoadID3(TID3Version.id3Version2, ref id3Info);

            List<String> aa = new List<string>();
            if (id3Info.Artist != null && id3Info.Artist != "")
                aa.Add(id3Info.Artist);
            if (id3Info.Album != null && id3Info.Album != "")
                aa.Add(id3Info.Album);
            emt.ArtistAlbum = String.Join(" - ", aa);

            if (id3Info.Title != null && id3Info.Title != "")
                emt.Title = id3Info.Title;
            else
                emt.Title = filePath;

            if (id3Info.Track != null && id3Info.Track != "")
                emt.Tracknumber = int.Parse(id3Info.Track);
            else
                emt.Tracknumber = 0;

            TStreamInfo streamInfo = new TStreamInfo();
            player.GetStreamInfo(ref streamInfo);
            emt.Duration = streamInfo.Length.ms;
            emt.AddBookmark(0); // also, set IsSaved to false here

            player.Close();

            PlaylistElementsArray.Add(emt);
            IsSaved = false;
        }

        public void Save()
        {
            foreach (var item in PlaylistElementsArray)
            {
                item.FlattenProgress();
            }

            Boolean shouldSave = false;
            foreach (var item in PlaylistElementsArray)
            {
                if (!item.IsSaved)
                {
                    shouldSave = true;
                    break;
                }
            }
            if (!shouldSave && IsSaved) return;

            MessageBoxResult result = MessageBox.Show("Do you want to save changes of playlist?", "Shiori", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No) return;

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "Shiori playlist (*.shiori)|*.shiori";

            if (_playlistPath != null)
                saveFileDialog.InitialDirectory = _playlistPath;
            else if (mutualPath != null)
                saveFileDialog.InitialDirectory = mutualPath.ToString();

            if (saveFileDialog.ShowDialog() == true)
            {
                PlaylistRecoverData _data = new PlaylistRecoverData()
                {
                    Title = _title,
                    Tracks = PlaylistElementsArray
                };
                File.WriteAllText(saveFileDialog.FileName, JsonConvert.SerializeObject(_data));
            }
        }

        private Boolean IsDirectory(String path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return false;

            return (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
        }

        private DirectoryInfo DirectoryFromPath(String path)
        {
            if (IsDirectory(path))
                return new DirectoryInfo(path);
            else
                return new FileInfo(path).Directory;
        }

        private DirectoryInfo GetMutualPath(String path1, String path2)
        {
            String _path1 = DirectoryFromPath(path1).ToString();
            String _path2 = DirectoryFromPath(path2).ToString();

            String _mutualPath = "";
            String pathPart = "";
            int minLength = Math.Min(_path1.Length, _path2.Length);
            char c;

            for (int i = 0; i < minLength; i++)
            {
                c = _path2[i];
                if (c == _path1[i])
                {
                    pathPart += c;
                    if (c == '\\')
                    {
                        _mutualPath += pathPart;
                        pathPart = "";
                    }
                }
                else
                {
                    break;
                }
            }

            if (pathPart != "" && IsDirectory(_mutualPath + pathPart))
                _mutualPath += pathPart;

            if (_mutualPath != "")
                return new DirectoryInfo(_mutualPath);
            else
                return null;
        }

        private DirectoryInfo UpdateMutualPath(String filePath)
        {
            if (mutualPath == null)
                return mutualPath = DirectoryFromPath(filePath);

            return mutualPath = GetMutualPath(mutualPath.ToString(), filePath);
        }

        public void MoveFilesUp(int min, int max)
        {
            if (min > 0)
            {
                var f = PlaylistElementsArray[min - 1];
                PlaylistElementsArray.Remove(f);
                PlaylistElementsArray.Insert(max, f);
            } // else: already in the top
            IsSaved = false;
        }

        public void MoveFilesDown(int min, int max)
        {
            if (max < PlaylistElementsArray.Count - 1)
            {
                var f = PlaylistElementsArray[max + 1];
                PlaylistElementsArray.Remove(f);
                PlaylistElementsArray.Insert(min, f);
            } // else: already in the bottom
            IsSaved = false;
        }

        public void DeleteElement(int i)
        {
            PlaylistElementsArray.RemoveAt(i);
            IsSaved = false;
        }
    }

    class PlaylistRecoverData
    {
        public String Title;
        public ObservableCollection<PlaylistElement> Tracks;
    }
}
