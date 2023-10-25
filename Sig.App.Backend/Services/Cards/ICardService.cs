using Sig.App.Backend.PdfTemplates;
using System.Collections.Generic;
using System.IO;

namespace Sig.App.Backend.Services.Cards
{
    public interface ICardService
    {
        Stream GenerateCardPrintFile(List<CardItem> items);
    }
}
