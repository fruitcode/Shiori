using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using libZPlay;

namespace Shiori.Playlist
{
    class PlaylistElement
    {
        public Boolean IsSaved = false;
        public String FilePath { get; set; }
        public String Artist { get; set; }
        public String Album { get; set; }
        public String Title { get; set; }
        public String ArtistAlbum { get { return this.Artist + " - " + this.Album; } }

        public List<int> Bookmarks = new List<int>();
        public uint Duration { get; set; }

        public TStreamTime GetPreviousBookmark(TStreamTime t)
        {
            uint max = 0;
            uint current = t.sec - 1; // minus one second, to leave a time to skip to previous bookmark when double-clicking 'back' button

            foreach (var i in Bookmarks)
            {
                if (i > max && i < current)
                    max = (uint)i;
            }
            return new TStreamTime() { sec = max };
        }

        public TStreamTime GetNextBookmark(TStreamTime t)
        {
            uint min = Duration;
            uint current = t.sec;

            foreach (var i in Bookmarks)
            {
                if (i < min && i > current)
                    min = (uint)i;
            }
            return new TStreamTime() { sec = min };
        }
    }
}
