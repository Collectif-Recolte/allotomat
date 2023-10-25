using Sig.App.Backend.Services.QRCode;
using System.Collections.Generic;

namespace Sig.App.Backend.PdfTemplates
{
    public class CardModel
    {
        public string ProjectCardImageUrl { get; set; }
        public List<CardItem> Items { get; set; }
    }

    public class CardItem
    {
        private readonly IQRCodeService QrCodeService;

        public CardItem(IQRCodeService qrCodeService)
        {
            QrCodeService = qrCodeService;
        }

        public long Id { get; set; }
        public string IdGQL { get; set; }
        public string UniqueCardId { get; set; }

        public string QrCode()
        {
            return QrCodeService.GenerateQRCode(IdGQL);
        }

        public string QrCodeImage()
        {
            return QrCodeService.GenerateQrCodeImage(QrCode());
        }
    }
}
