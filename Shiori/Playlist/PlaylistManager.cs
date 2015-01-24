using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.IO;
using System.Windows; // messagebox
using libZPlay;

namespace Shiori.Playlist
{
    class PlaylistManager
    {
        private Boolean IsSaved = false;
        public ObservableCollection<PlaylistElement> PlaylistElementsArray = new ObservableCollection<PlaylistElement>();
        private ZPlay player = new ZPlay();
        private DirectoryInfo mutualPath;
        public int NowPlayingIndex { get; set; }
        public PlaylistElement CurrentElement { get { return PlaylistElementsArray[NowPlayingIndex]; } }

        public void AddFile(String filePath)
        {
            if (!player.OpenFile(filePath, TStreamFormat.sfAutodetect))
            {
                Console.WriteLine("Unable to open file " + filePath + ". Error: " + player.GetError());
                return;
            }

            PlaylistElement emt = new PlaylistElement();
            emt.Bookmarks.Add(0);
            emt.FilePath = filePath;
            UpdateMutualPath(filePath);

            TID3Info id3Info = new TID3Info();
            player.LoadID3(TID3Version.id3Version2, ref id3Info);

            if (id3Info.Artist != null && id3Info.Artist != "")
                emt.Artist = id3Info.Artist;
            if (id3Info.Album != null && id3Info.Album != "")
                emt.Album = id3Info.Album;
            if (id3Info.Title != null && id3Info.Title != "")
                emt.Title = id3Info.Title;

            if (emt.Artist == null && emt.Album == null && emt.Title == null)
            {
                emt.Artist = "?";
                emt.Album = "?";
                emt.Title = filePath;
            }

            TStreamInfo streamInfo = new TStreamInfo();
            player.GetStreamInfo(ref streamInfo);
            emt.Duration = streamInfo.Length.sec;

            PlaylistElementsArray.Add(emt);
            player.Close();
        }

        public void Save()
        {
            if (IsSaved) return;

            MessageBoxResult result = MessageBox.Show("Do you want to save changes of playlist?", "Shiori", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No) return;

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "Shiori playlist (*.shiori)|*.shiori";
            if (mutualPath != null)
                saveFileDialog.InitialDirectory = mutualPath.ToString();
            if (saveFileDialog.ShowDialog() == true)
            {
                MessageBox.Show(saveFileDialog.FileName);
            }
        }

        private Boolean IsDirectory(String path)
        {
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
    }
}
