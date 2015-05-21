using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiori.Playlist
{
    class ListeningProgressRange
    {
        public double Start { get; set; }
        public double End { get; set; }

        public ListeningProgressRange()
        {
            Start = 0;
            End = 0;
        }

        public void Merge(ListeningProgressRange other)
        {
            if (other.Start < this.Start)
                this.Start = other.Start;
            if (other.End > this.End)
                this.End = other.End;
        }
    }
}
