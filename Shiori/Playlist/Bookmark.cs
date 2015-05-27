using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Shiori.Playlist
{
    public class Bookmark
    {
        public uint Time { get; set; }
        public String Title { get; set; }
        [JsonIgnore]
        public double Percent { get; set; }
    }
}
