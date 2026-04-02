using QRCoder;
using System.Drawing;

namespace TINH_FINAL_2256.Services
{
    /// <summary>
    /// D?ch v? QR Code
    /// </summary>
    public interface IQRCodeService
    {
        byte[] GenerateQRCode(string text, int pixelsPerModule = 20);
        string GenerateQRCodeBase64(string text, int pixelsPerModule = 20);
    }

    public class QRCodeService : IQRCodeService
    {
        private readonly ILogger<QRCodeService> _logger;

        public QRCodeService(ILogger<QRCodeService> logger)
        {
            _logger = logger;
        }

        public byte[] GenerateQRCode(string text, int pixelsPerModule = 20)
        {
            try
            {
                using (var qrGenerator = new QRCodeGenerator())
                {
                    using (var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q))
                    {
                        using (var qrCode = new PngByteQRCode(qrCodeData))
                        {
                            var qrCodeImage = qrCode.GetGraphic(pixelsPerModule);
                            return qrCodeImage;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating QR code: {ex.Message}");
                throw;
            }
        }

        public string GenerateQRCodeBase64(string text, int pixelsPerModule = 20)
        {
            try
            {
                var qrCodeBytes = GenerateQRCode(text, pixelsPerModule);
                return Convert.ToBase64String(qrCodeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating QR code Base64: {ex.Message}");
                throw;
            }
        }
    }
}
