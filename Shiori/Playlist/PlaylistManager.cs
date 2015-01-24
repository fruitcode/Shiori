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
            saveFileDialog.InitialDirectory = mutualPath.ToString();
            if (saveFileDialog.ShowDialog() == true)
            {
                MessageBox.Show(saveFileDialog.FileName);
            }
        }

        private DirectoryInfo UpdateMutualPath(String filePath)
        {
            DirectoryInfo fileDir = (new FileInfo(filePath)).Directory;

            if (mutualPath == null)
                return mutualPath = fileDir;

            String _fileDir = fileDir.ToString();
            string _mutualPath = mutualPath.ToString();
            String newMutualPath = "";
            String pathPart = "";
            int minLength = Math.Min(_fileDir.Length, _mutualPath.Length);
            char c;
            for (int i = 0; i < minLength; i++)
            {
                c = _mutualPath[i];
                if (c == _fileDir[i])
                {
                    pathPart += c;
                    if (c == '\\')
                    {
                        newMutualPath += pathPart;
                        pathPart = "";
                    }
                }
                else
                {
                    break;
                }
            }
            
            if ( pathPart != "" &&
                (File.GetAttributes(newMutualPath + pathPart) & FileAttributes.Directory) == FileAttributes.Directory )
            {
                newMutualPath += pathPart;
            }

            return mutualPath = new DirectoryInfo(newMutualPath);
        }
    }
}
