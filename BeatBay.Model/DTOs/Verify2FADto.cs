using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatBay.DTOs
{
    public class Verify2FADto
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
