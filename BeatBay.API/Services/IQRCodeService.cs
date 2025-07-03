using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace BeatBay.Services
{
    public interface IQRCodeService
    {
        byte[] GenerateQRCode(string text);
        string GenerateQRCodeBase64(string text);
    }
}