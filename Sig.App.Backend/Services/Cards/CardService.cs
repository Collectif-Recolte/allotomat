using MediatR;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.PdfTemplates;
using System.Collections.Generic;
using System.IO;

namespace Sig.App.Backend.Services.Cards
{
    public class CardService : ICardService
    {
        private readonly IMediator mediator;

        public CardService(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public Stream GenerateCardPrintFile(List<CardItem> items)
        {
            var generator = new ExcelGenerator();
            generator.AddDataWorksheet("Cartes générées", items)
                .Column("QR Code", x => x.QrCode())
                .Column("Id", x => x.Id)
                .Column("Unique card Id", x => x.UniqueCardId);

            return generator.Render();
        }
    }
}
