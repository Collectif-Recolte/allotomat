using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sig.App.Backend.Services.HtmlToPdf
{
    public class Api2PdfConverter : IHtmlToPdfConverter, IDisposable
    {
        private readonly Api2PdfClient api2PdfClient;
        private readonly HttpClient httpClient;

        public Api2PdfConverter(Api2PdfClient api2PdfClient, IHttpClientFactory clientFactory)
        {
            this.api2PdfClient = api2PdfClient;

            httpClient = clientFactory.CreateClient();
        }

        public async Task<string> Convert(string html)
        {
            var apiResponse = await api2PdfClient.ConvertToPdf(html);

            return apiResponse.Pdf;
        }

        public async Task<MemoryStream> DownloadFile(string url)
        {
            var result = new MemoryStream();

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var pdfStream = await response.Content.ReadAsStreamAsync();
            await pdfStream.CopyToAsync(result);

            result.Position = 0;
            return result;
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}