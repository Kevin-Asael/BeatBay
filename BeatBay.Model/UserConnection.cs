using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatBay.Model
{
    // Relación “padre-hijo” entre un plan familiar/empresarial y un usuario invitado
    public class UserConnection
    {
        // Clave primaria
        [Key]
        public int Id { get; set; }

        // FK a la suscripción del usuario principal (padre)
        [ForeignKey("ParentSubscription")]
        public int ParentSubscriptionId { get; set; }

        // Navegación a la suscripción padre
        public virtual PlanSubscription ParentSubscription { get; set; }

        // FK al usuario hijo (cuenta invitada)
        [ForeignKey("ChildUser")]
        public int ChildUserId { get; set; }

        // Navegación al usuario hijo
        public virtual User ChildUser { get; set; }

        // Cuándo se conectó la cuenta hija al plan
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

        // Activa mientras la conexión esté vigente
        public bool IsActive { get; set; } = true;
    }
}
