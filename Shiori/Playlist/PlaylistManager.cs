using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using libZPlay;

namespace Shiori.Playlist
{
    class PlaylistManager
    {
        public List<PlaylistElement> PLElements = new List<PlaylistElement>();
        private ZPlay player = new ZPlay();

        public void AddFile(String filePath)
        {
            PlaylistElement e = new PlaylistElement();
            e.FilePath = filePath;

            player.OpenFile(filePath, TStreamFormat.sfAutodetect);

            TID3Info i3 = new TID3Info();
            player.LoadID3(TID3Version.id3Version2, ref i3);

            if (i3.Artist != null && i3.Artist != "")
                e.Artist = i3.Artist;
            if (i3.Album != null && i3.Album != "")
                e.Album = i3.Album;
            if (i3.Title != null && i3.Title != "")
                e.Title = i3.Title;

            if (e.Artist == null && e.Album == null && e.Title == null)
            {
                e.Artist = "?";
                e.Album = "?";
                e.Title = filePath;
            }

            PLElements.Add(e);
            player.Close();
        }
    }
}
