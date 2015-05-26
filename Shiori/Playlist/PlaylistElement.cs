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
    class PlaylistElement : INotifyPropertyChanged
    {
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
        [JsonIgnore]
        public double PercentsCompleted { get; set; }
        
        public PlaylistElement()
        {
            Progress = new ObservableCollection<ListeningProgressRange>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
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
            OnPropertyChanged("self");
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
            double _totalListened = 0;

            if (Progress != null) {
                foreach (var i in Progress)
                {
                    _totalListened += i.End - i.Start;
                    i.StartPercent = 100.0 * i.Start / Duration;
                    i.EndPercent = 100.0 * i.End / Duration;
                }
            }

            PercentsCompleted = _totalListened / Duration;
        }

        public void StartProgressRange(uint t)
        {
            progressStart = t;
        }

        public void EndProgressRange(uint t)
        {
            ListeningProgressRange newRange = new ListeningProgressRange()
            {
                Start = progressStart,
                StartPercent = 100.0 * progressStart / Duration,
                End = t,
                EndPercent = 100.0 * t / Duration
            };

            List<ListeningProgressRange> delete = new List<ListeningProgressRange>();
            Boolean merged = true;

            while (merged == true)
            {
                merged = false;
                foreach (var range in Progress)
                {
                    if (delete.Contains(range)) // already merged with
                        continue;

                    if (!(newRange.Start > range.End || newRange.End < range.Start)) // merge if intersects
                    {
                        if (newRange.Start >= range.Start && newRange.End <= range.End) // range2 contains range1; no actions required
                        {
                            newRange = null;
                            break;
                        }

                        newRange.Merge(range);
                        delete.Add(range);
                        merged = true;
                        break;
                    }
                }
            }

            if (newRange != null)
            {
                foreach (var item in delete)
                {
                    Progress.Remove(item);
                    PercentsCompleted -= ((double)item.End - item.Start) / Duration;
                }

                Progress.Add(newRange);
                PercentsCompleted += ((double)newRange.End - newRange.Start) / Duration;

                OnPropertyChanged("self");
                OnPropertyChanged("PercentsCompleted");
            }

            progressStart = 0;
        }
    }
}
