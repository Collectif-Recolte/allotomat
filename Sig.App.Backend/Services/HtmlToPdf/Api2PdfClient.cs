using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace Sig.App.Backend.Services.HtmlToPdf
{
    public class Api2PdfClient
    {
        private readonly HttpClient client;

        public Api2PdfClient(HttpClient client)
        {
            this.client = client;
        }

        public async Task<PdfResponse> ConvertToPdf(string html)
        {
            var content = CreateContent(CreateApiRequest(html));
            
            var response = await client.PostAsync("/wkhtmltopdf/html", content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<PdfResponse>();
        }

        public async Task DeleteFile(string responseId)
        {
            await client.DeleteAsync($"/pdf/{responseId}");
        }

        private static WkhtmlRequest CreateApiRequest(string html)
        {
            return new WkhtmlRequest {
                Html = html,
                Options = new WkhtmlOptions {
                    Dpi = 300,
                    PageHeight = "60mm",
                    PageWidth = "92mm",
                    MarginBottom = "0mm",
                    MarginTop = "0mm",
                    MarginLeft = "0mm",
                    MarginRight = "0mm"
                }
            };
        }

        private static HttpContent CreateContent<TRequest>(TRequest request)
        {
            return new ObjectContent(
                typeof(TRequest),
                request,
                new JsonMediaTypeFormatter {
                    SerializerSettings = {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }
                });
        }

        private class WkhtmlRequest
        {
            public string Html { get; set; }
            public bool InlinePdf { get; set; }
            public string FileName { get; set; }
            public WkhtmlOptions Options { get; set; }
        }

        private class WkhtmlOptions
        {
            public int Dpi { get; set; }
            public string PageHeight { get; set; }
            public string PageWidth { get; set; }
            public string MarginBottom { get; set; }
            public string MarginTop { get; set; }
            public string MarginLeft { get; set; }
            public string MarginRight { get; set; }
        }

        public class PdfResponse
        {
            public string Pdf { get; set; }
            public float MbIn { get; set; }
            public float MbOut { get; set; }
            public float Cost { get; set; }
            public bool Success { get; set; }
            public string ResponseId { get; set; }
        }
    }
}