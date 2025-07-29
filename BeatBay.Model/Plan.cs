using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeatBay.Model
{
    /// Representa un plan de suscripción disponible para los usuarios de BeatBay
    /// (p.e. Personal, Familiar, Empresarial).  
    /// Define precio, número máximo de conexiones y colecciones de navegación
    /// hacia usuarios y pagos asociados.
    public class Plan
    {
        /// Clave primaria.
        [Key]
        public int Id { get; set; }

        /// Nombre del plan (máx. 50 caracteres).  
        /// Ejemplos: “Personal”, “Familiar”, “Empresarial”.
        [Required, MaxLength(50)]
        public string Name { get; set; }

        /// Precio en dólares estadounidenses.  
        /// Se almacena con precisión decimal(18,2) para evitar errores de redondeo.
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceUSD { get; set; }

        /// Número máximo de cuentas que pueden estar vinculadas simultáneamente a este plan.
        /// Ej.: 1 para Personal, 4 para Familiar, 50 para Empresarial.
        [Required]
        public int MaxConnections { get; set; }

        /// Colección de usuarios que actualmente tienen asignado este plan
        ///User.PlanId
        public virtual ICollection<User> Users { get; set; } = new HashSet<User>();

        /// Colección de pagos registrados para este plan.
        public virtual ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
    }
}