using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeatBay.Model
{
    /// Entidad que representa una lista de reproducción creada por un
    /// usuario. Puede ser pública o privada según la lógica de negocio (en
    /// este modelo la visibilidad se gestiona a nivel de controlador, no con
    /// un campo dedicado).
    public class Playlist
    {
        /// Clave primaria de la playlist.
        [Key]
        public int Id { get; set; }

        /// Nombre de la playlist (máx. 150 caracteres).
        [Required, MaxLength(150)]
        public string Name { get; set; }

        /// Propietario de la playlist (FK a <see cref="User"/>).
        [ForeignKey(nameof(User))]
        public int UserId { get; set; }

        /// Navegación al usuario propietario.
        public virtual User User { get; set; }

        /// Canciones que pertenecen a la playlist 
        public virtual ICollection<PlaylistSong> PlaylistSongs { get; set; } =
            new HashSet<PlaylistSong>();
    }
}
