using System.IO;
using System.Threading.Tasks;

namespace Sig.App.Backend.Services.HtmlToPdf
{
    public interface IHtmlToPdfConverter
    {
        Task<string> Convert(string html);
        Task<MemoryStream> DownloadFile(string url);
    }
}