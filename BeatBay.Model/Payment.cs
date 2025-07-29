using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeatBay.Model
{
    /// Posibles estados de un pago dentro del sistema.
    public enum PaymentStatus
    {
        /// El pago ha sido creado pero aún no se ha completado (en espera de aprobación en PayPal, por ejemplo).
        Pending,

        /// El pago se procesó correctamente y los fondos fueron recibidos.
        Completed,

        /// El pago se intentó procesar, pero falló (fondos insuficientes, tarjeta rechazada, etc.).
        Failed,

        /// El usuario o el sistema cancelaron el pago antes de completarse.
        Cancelled
    }

    /// Entidad que representa un registro de pago realizado por un usuario para adquirir un plan.
    public class Payment
    {
        /// Clave primaria del pago.
        [Key]
        public int Id { get; set; }

        /// Identificador del usuario que realizó el pago (clave foránea).
        [ForeignKey("User")]
        public int UserId { get; set; }

        /// Navegación hacia el usuario que realizó el pago.
        public virtual User User { get; set; }

        /// Identificador del plan que se está pagando (clave foránea).
        [ForeignKey("Plan")]
        public int PlanId { get; set; }

        /// Navegación hacia el plan adquirido.
        public virtual Plan Plan { get; set; }

        /// Estado actual del pago (Pending, Completed, etc.).
        [Required]
        public PaymentStatus Status { get; set; }

        /// Fecha y hora en la que se registró el pago (UTC).
        [Required]
        public DateTime PaymentDate { get; set; }

        /// Importe pagado en dólares (máx. 18 dígitos enteros, 2 decimales).
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
    }
}