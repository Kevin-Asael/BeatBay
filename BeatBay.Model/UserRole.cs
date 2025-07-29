using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatBay.Model
{
    public class UserRole : IdentityUserRole<int>
    {
        // Relación entre Usuario y Rol
        public virtual User? User { get; set; }
        public virtual Role? Role { get; set; }
    }
}
