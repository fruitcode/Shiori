using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using libZPlay;
using Newtonsoft.Json;

namespace Shiori.Playlist
{
    class PlaylistElement
    {
        [JsonIgnore]
        public Boolean IsSaved = true;

        public String FilePath { get; set; }
        public String ArtistAlbum { get; set; }
        public String Title { get; set; }

        private List<int> pBookmarks = new List<int>();
        public List<int> Bookmarks { get { return pBookmarks; } set { pBookmarks = value; } }
        public uint Duration { get; set; }

        private Boolean pIsCompleted = false;
        public Boolean IsCompleted { get { return pIsCompleted; } set { pIsCompleted = value; } }
        
        public TStreamTime GetPreviousBookmark(TStreamTime t)
        {
            uint max = 0;
            uint current = t.sec - 1; // minus one second, to leave a time to skip to previous bookmark when double-clicking 'back' button

            foreach (var i in pBookmarks)
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

            foreach (var i in pBookmarks)
            {
                if (i < min && i > current)
                    min = (uint)i;
            }
            return new TStreamTime() { sec = min };
        }

        public void AddBookmark(int t)
        {
            pBookmarks.Add(t);
            IsSaved = false;
        }
    }
}
