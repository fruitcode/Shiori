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
    public class PlaylistElement : INotifyPropertyChanged
    {
        public String FilePath { get; set; }
        public String ArtistAlbum { get; set; }
        public String Title { get; set; }
        public int Tracknumber { get; set; }

        public uint Duration { get; set; }

        public ObservableCollection<Bookmark> Bookmarks { get; set; }
        public ObservableCollection<ListeningProgressRange> Progress { get; set; }

        private double _percentsCompleted;
        [JsonIgnore]
        public double PercentsCompleted {
            get
            {
                double currentPercents = CurrentListeningRange == null ? 0 : CurrentListeningRange.EndPercent - CurrentListeningRange.StartPercent;
                return _percentsCompleted + currentPercents;
            }
            set
            {
                _percentsCompleted = value;
            }
        }

        private ListeningProgressRange _currentListeningRange;
        [JsonIgnore]
        public ListeningProgressRange CurrentListeningRange
        {
            get
            {
                return _currentListeningRange;
            }
            set
            {
                _currentListeningRange = value;
                OnPropertyChanged("CurrentListeningRange");
            }
        }
        
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
                if (i.Time > max && i.Time < current)
                    max = i.Time;
            }
            return new TStreamTime() { ms = max };
        }

        public TStreamTime GetNextBookmark(TStreamTime t)
        {
            uint min = Duration;
            uint current = t.ms;

            foreach (var i in Bookmarks)
            {
                if (i.Time < min && i.Time > current)
                    min = i.Time;
            }
            return new TStreamTime() { ms = min };
        }

        public void AddBookmark(uint t)
        {
            if (Bookmarks == null)
                Bookmarks = new ObservableCollection<Bookmark>();

            int idx = 0;
            for (idx = 0; idx < Bookmarks.Count; idx++)
            {
                if (t < Bookmarks[idx].Time)
                    break;
            }

            Bookmarks.Insert(idx, new Bookmark()
            {
                Time = t,
                Percent = (double)t / Duration
            });
            OnPropertyChanged("self");
        }

        public void DeleteBookmark(Bookmark bookmark)
        {
            Bookmarks.Remove(bookmark);
            OnPropertyChanged("self");
        }

        public void RegeneratePercents()
        {
            if (Bookmarks != null)
            {
                foreach (var i in Bookmarks)
                    i.Percent = (double)i.Time / Duration;
            }

            double _totalListened = 0;

            if (Progress != null) {
                foreach (var i in Progress)
                {
                    _totalListened += i.End - i.Start;
                    i.StartPercent = (double)i.Start / Duration;
                    i.EndPercent = (double)i.End / Duration;
                }
            }

            PercentsCompleted = _totalListened / Duration;
        }

        public void StartListeningRange(uint t)
        {
            if (CurrentListeningRange != null)
                Progress.Add(CurrentListeningRange);

            CurrentListeningRange = new ListeningProgressRange()
            {
                Start = t,
                StartPercent = (double)t / Duration,
                End = t,
                EndPercent = (double)t / Duration
            };
        }

        public void UpdateListeningRange(uint t)
        {
            // if we have merged currentListeningRange and current position is not yet
            // reached currentListeningRange.End position, so NO action required
            if (CurrentListeningRange.End > t)
                return;

            CurrentListeningRange.End = t;
            CurrentListeningRange.EndPercent = (double)t / Duration;

            bool progressChanged = FlattenListeningRange();
            OnPropertyChanged("CurrentListeningRange");
            OnPropertyChanged("PercentsCompleted");

            if (progressChanged)
            {
                OnPropertyChanged("self");
            }
        }

        public void EndListeningRange(uint t)
        {
            UpdateListeningRange(t);
            Progress.Add(CurrentListeningRange);
            _percentsCompleted += CurrentListeningRange.EndPercent - CurrentListeningRange.StartPercent;
            CurrentListeningRange = null;
            OnPropertyChanged("PercentsCompleted");
        }

        private bool FlattenListeningRange()
        {
            List<ListeningProgressRange> delete = new List<ListeningProgressRange>();
            Boolean merged = true;
            Boolean progressChanged = false;

            while (merged == true)
            {
                merged = false;
                foreach (var range in Progress)
                {
                    if (delete.Contains(range)) // already merged with
                        continue;

                    if (!(CurrentListeningRange.Start > range.End || CurrentListeningRange.End < range.Start)) // merge if intersects
                    {
                        // if currentListeningRange expands range after merging
                        if (!progressChanged && (CurrentListeningRange.Start < range.Start || CurrentListeningRange.End > range.End))
                            progressChanged = true;

                        CurrentListeningRange.Merge(range);
                        delete.Add(range);
                        merged = true;
                        break;
                    }
                }
            }

            foreach (var item in delete)
            {
                Progress.Remove(item);
                _percentsCompleted -= ((double)item.End - item.Start) / Duration;
            }

            // return true if:
            // 1) progressChanged = true (ie.: currentListeningRange was merged and expanded range)
            // 2) currentListeningRange has no intersects with existing ranges
            return progressChanged ? true : delete.Count == 0;
        }
    }
}
