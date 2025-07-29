using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeatBay.Model
{
    /// Representa la suscripción activa (o histórica) de un usuario a un plan
    /// determinado. Almacena fecha de inicio/fin, monto pagado y vínculos a
    /// usuarios “hijos” (conexiones) en planes Familiar / Empresarial.
    public class PlanSubscription
    {
        /// Clave primaria de la suscripción.
        [Key]
        public int Id { get; set; }

        /// ID del usuario propietario de la suscripción (FK).
        [ForeignKey(nameof(User))]
        public int UserId { get; set; }

        /// Navegación al usuario propietario.
        public virtual User User { get; set; }

        /// ID del plan adquirido (FK).
        [ForeignKey(nameof(Plan))]
        public int PlanId { get; set; }

        /// Navegación al plan correspondiente.
        public virtual Plan Plan { get; set; }

        /// Fecha y hora UTC en la que comienza la suscripción.
        [Required]
        public DateTime StartDate { get; set; }

        /// Fecha y hora UTC de expiración de la suscripción.
        [Required]
        public DateTime EndDate { get; set; }

        /// Indica si la suscripción está activa (true) o cancelada/expirada (false).
        public bool IsActive { get; set; } = true;

        /// Importe pagado por el periodo (moneda USD, precisión 18,2).
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        /// Fecha en que se creó el registro (para historial y auditoría).
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// Conexiones activas a la suscripción (usuarios hijos).
        public virtual ICollection<UserConnection> UserConnections { get; set; } =
            new HashSet<UserConnection>();
    }
}