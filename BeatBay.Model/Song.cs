using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeatBay.Model
{
    /// Canción publicada en BeatBay.
    /// Almacena metadatos esenciales y mantiene relaciones con el artista que la subió,
    /// las estadísticas de reproducción y las playlists en las que aparece.
    public class Song
    {
        /// Clave primaria de la canción.
        [Key]
        public int Id { get; set; }

        /// Título de la canción (máx. 200 caracteres).
        [Required, MaxLength(200)]
        public string Title { get; set; }

        /// Duración exacta de la pista.
        [Required]
        public TimeSpan Duration { get; set; }

        /// Género musical (opcional, máx. 100 caracteres).
        [MaxLength(100)]
        public string? Genre { get; set; }

        /// URL pública donde se reproduce la canción (Azure).
        [Required]
        public string StreamingUrl { get; set; }

        /// ID del artista (usuario) que subió la canción.
        [ForeignKey(nameof(Artist))]
        public int ArtistId { get; set; }

        /// Navegación al artista propietario.
        public virtual User Artist { get; set; }

        /// Indica si la canción está visible para los oyentes.
        public bool IsActive { get; set; } = true;

        /// Fecha de subida en UTC.
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// Estadísticas de reproducción agregadas (playbacks diarios por usuario).
        public virtual ICollection<PlaybackStatistic> PlaybackStatistics { get; set; } =new HashSet<PlaybackStatistic>();
    }
}
