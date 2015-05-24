using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;

using libZPlay;
using Newtonsoft.Json;

namespace Shiori.Playlist
{
    class PlaylistElement
    {
        public delegate void PlaylistElementChangedEventHandler();
        public event PlaylistElementChangedEventHandler PlaylistElementChanged;

        public String FilePath { get; set; }
        public String ArtistAlbum { get; set; }
        public String Title { get; set; }
        public int Tracknumber { get; set; }

        public ObservableCollection<ListeningProgressRange> Progress { get; set; }
        [JsonIgnore]
        private uint progressStart;

        public List<uint> Bookmarks { get; set; }
        [JsonIgnore]
        public ObservableCollection<double> BookmarksPercents { get; set; }
        public uint Duration { get; set; }
        
        public PlaylistElement()
        {
            Progress = new ObservableCollection<ListeningProgressRange>();
        }

        private void OnPlaylistElementChanged()
        {
            if (PlaylistElementChanged != null)
                PlaylistElementChanged();
        }

        public TStreamTime GetPreviousBookmark(TStreamTime t)
        {
            uint max = 0;
            uint current = t.ms - 1000; // minus one second, to leave a time to skip to previous bookmark when double-clicking 'back' button

            foreach (var i in Bookmarks)
            {
                if (i > max && i < current)
                    max = i;
            }
            return new TStreamTime() { ms = max };
        }

        public TStreamTime GetNextBookmark(TStreamTime t)
        {
            uint min = Duration;
            uint current = t.ms;

            foreach (var i in Bookmarks)
            {
                if (i < min && i > current)
                    min = i;
            }
            return new TStreamTime() { ms = min };
        }

        public void AddBookmark(uint t)
        {
            if (Bookmarks == null)
            {
                Bookmarks = new List<uint>();
                BookmarksPercents = new ObservableCollection<double>();
            }

            Bookmarks.Add(t);
            BookmarksPercents.Add(100.0 * t / Duration);
            OnPlaylistElementChanged();
        }

        public void RegenerateBookmarkPercent()
        {
            BookmarksPercents = new ObservableCollection<double>();

            foreach (var i in Bookmarks)
            {
                BookmarksPercents.Add(100.0 * i / Duration);
            }
        }

        public void RegeneratePercents()
        {
            if (Progress != null) {
                foreach (var i in Progress)
                {
                    i.StartPercent = 100.0 * i.Start / Duration;
                    i.EndPercent = 100.0 * i.End / Duration;
                }
            }
        }

        public void StartProgressRange(uint t)
        {
            progressStart = t;
        }

        public void EndProgressRange(uint t)
        {
            ListeningProgressRange pr = new ListeningProgressRange()
            {
                Start = progressStart,
                StartPercent = 100.0 * progressStart / Duration,
                End = t,
                EndPercent = 100.0 * t / Duration
            };

            Progress.Add(pr);
            OnPlaylistElementChanged();
            progressStart = 0;
        }

        public void FlattenProgress()
        {
            if (Progress == null)
                return;

            var oldProgress = Progress;
            int oldProgressCount = oldProgress.Count;
            var newProgress = new ObservableCollection<ListeningProgressRange>();
            List<ListeningProgressRange> deleteFromList;

            while (oldProgress.Count > 0)
            {
                ListeningProgressRange range1 = oldProgress[0];

                deleteFromList = new List<ListeningProgressRange>();
                deleteFromList.Add(range1);
                // if some ListeningProgressRanges have been merged in current step,
                // we have items to delete from 'oldProgress' array and we should iterate
                // through this array one more time to check if we can merge more items
                while (deleteFromList.Count > 0)
                {
                    foreach (var r in deleteFromList)
                        oldProgress.Remove(r);
                    deleteFromList.Clear();

                    foreach (var range2 in oldProgress)
                        if (!(range1.Start > range2.End || range1.End < range2.Start)) // merge if intersects
                        {
                            range1.Merge(range2);
                            deleteFromList.Add(range2);
                        }
                }

                newProgress.Add(range1);
            }

            Progress.Clear();
            foreach (var item in newProgress)
                Progress.Add(item);

            if (oldProgressCount != newProgress.Count) // if some ProgressRanges have been merged
                OnPlaylistElementChanged();
        }
    }
}
