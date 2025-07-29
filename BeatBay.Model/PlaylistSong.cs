using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeatBay.Model
{
    public class PlaylistSong
    {
        /// Clave primaria de la relación Playlist-Song.
        public int PlaylistId { get; set; }
        /// Navegación a la playlist correspondiente.
        public virtual Playlist Playlist { get; set; }
        // Clave primaria de la relación Playlist-Song.
        public int SongId { get; set; }
        /// Navegación a la canción correspondiente.
        public virtual Song Song { get; set; }
    }
}
