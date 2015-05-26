using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Shiori.Playlist
{
    class ListeningProgressRange
    {
        public uint Start { get; set; }
        [JsonIgnore]
        public double StartPercent { get; set; }

        public uint End { get; set; }
        [JsonIgnore]
        public double EndPercent { get; set; }

        public void Merge(ListeningProgressRange other)
        {
            if (other.Start < this.Start)
            {
                this.Start = other.Start;
                this.StartPercent = other.StartPercent;
            }
            if (other.End > this.End)
            {
                this.End = other.End;
                this.EndPercent = other.EndPercent;
            }
        }
    }
}
