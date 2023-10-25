using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sig.App.Backend.Services.Mailer
{
    public abstract class EmailModel
    {
        public string BaseUrl { get; set; }
        public string To { get; set; }
        public virtual string TemplateName => Regex.Replace(GetType().Name, "Email$", "");
        public abstract string Subject { get; }
        public IList<EmailAttachmentModel> Attachments { get; set; }

        protected EmailModel(string to)
        {
            To = to;
        }
    }
}