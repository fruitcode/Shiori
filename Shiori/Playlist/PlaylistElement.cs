using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiori.Playlist
{
    class PlaylistElement
    {
        public String FilePath { get; set; }
        public String Artist { get; set; }
        public String Album { get; set; }
        public String Title { get; set; }

        public String ArtistAlbum { get { return this.Artist + " - " + this.Album; } }
    }
}
