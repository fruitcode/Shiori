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
        public List<PlaylistElement> PlaylistElementsArray = new List<PlaylistElement>();
        private ZPlay player = new ZPlay();
        public int NowPlayingIndex { get; set; }
        public PlaylistElement CurrentElement { get { return PlaylistElementsArray[NowPlayingIndex]; } }

        public void AddFile(String filePath)
        {
            PlaylistElement emt = new PlaylistElement();
            emt.Bookmarks.Add(0);
            emt.FilePath = filePath;

            player.OpenFile(filePath, TStreamFormat.sfAutodetect);

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

    }
}
