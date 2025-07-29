using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatBay.Model
{
    public class Role : IdentityRole<int>
    {
        // Roles de usuario ADMIN ARTIST USER
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
