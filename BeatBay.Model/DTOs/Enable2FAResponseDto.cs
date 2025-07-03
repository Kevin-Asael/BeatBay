using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatBay.DTOs
{
    public class Enable2FAResponseDto
    {
        public string QrCodeUrl { get; set; }
        public string ManualEntryKey { get; set; }
    }
}
