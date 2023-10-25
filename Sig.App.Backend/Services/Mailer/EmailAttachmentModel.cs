using System.IO;

namespace Sig.App.Backend.Services.Mailer
{
    public class EmailAttachmentModel
    {
        public EmailAttachmentModel(string filename, string contentType, Stream data)
        {
            Filename = filename;
            ContentType = contentType;
            Data = data;
        }

        public string Filename { get; set; }
        public string ContentType { get; set; }
        public Stream Data { get; set; }
    }
}
