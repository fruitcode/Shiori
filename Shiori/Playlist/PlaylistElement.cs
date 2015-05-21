﻿using System;
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
        public int Tracknumber { get; set; }
        public List<ListeningProgressRange> Progress { get; set; }

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

        public void AddProgressRange(ListeningProgressRange _range)
        {
            if (Progress == null)
                Progress = new List<ListeningProgressRange>();

            IsSaved = false;

            Progress.Add(_range);

            List<ListeningProgressRange> tmp = Progress;
            Progress = new List<ListeningProgressRange>();
            List<ListeningProgressRange> deleteFromList;

            while (tmp.Count > 0)
            {
                ListeningProgressRange range1 = tmp[0];

                deleteFromList = new List<ListeningProgressRange>();
                deleteFromList.Add(range1);
                // if some ListeningProgressRanges have been merged in current step,
                // we have items to delete from 'tmp' array and we should iterate
                // through this array one more time to check if we can merge more items
                while (deleteFromList.Count > 0)
                {
                    foreach (var r in deleteFromList)
                        tmp.Remove(r);
                    deleteFromList.Clear();

                    foreach (var range2 in tmp)
                        if (!(range1.Start > range2.End || range1.End < range2.Start)) // merge if intersects
                        {
                            range1.Merge(range2);
                            deleteFromList.Add(range2);
                        }
                }

                Progress.Add(range1);
            }
#if DEBUG
            PrintListeningProgress();
#endif
        }

        public void PrintListeningProgress()
        {
            Console.WriteLine("====================");
            foreach (var lpr in Progress)
            {
                Console.WriteLine("-- {0} -> {1}", lpr.Start, lpr.End);
            }
        }
    }
}
