namespace Sig.App.Backend.Services.QRCode
{
    public interface IQRCodeService
    {
        string GenerateQRCode(string message);
        string ReadQRCode(string qrCode);
        string GenerateQrCodeImage(string qrCode);
    }
}
