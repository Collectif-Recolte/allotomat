using FluentEmail.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Threading.Tasks;
using Sig.App.Backend.Services.Razor;
using FluentEmail.Core.Models;
using System.Linq;

namespace Sig.App.Backend.Services.Mailer
{
    public class FluentMailer : IMailer
    {
        private readonly IRazorRenderer razorRenderer;
        private readonly IFluentEmailFactory emailFactory;
        private readonly IConfiguration config;
        private readonly ILogger<FluentMailer> logger;

        public FluentMailer(IRazorRenderer razorRenderer, IFluentEmailFactory emailFactory, IConfiguration config, ILogger<FluentMailer> logger)
        {
            this.razorRenderer = razorRenderer;
            this.emailFactory = emailFactory;
            this.config = config;
            this.logger = logger;
        }

        public async Task Send<T>(T model) where T : EmailModel
        {
            logger.LogDebug($"Sending email template {model.TemplateName} to {model.To}.");

            if (string.IsNullOrWhiteSpace(model.BaseUrl))
                model.BaseUrl = config["Mailer:BaseUrl"];

            // NOTE: Pour le moment on utilise toujours fr-CA
            var body = await razorRenderer.RenderViewToStringAsync($"/EmailTemplates/{model.TemplateName}.cshtml", model, CultureInfo.GetCultureInfo("fr-CA"));

            var email = emailFactory.Create()
                .To(model.To)
                .Subject(model.Subject)
                .Body(body, isHtml: true);

            if (model.Attachments != null && model.Attachments.Any())
            {
                foreach (var att in model.Attachments)
                {
                    email.Attach(new Attachment
                    {
                        Filename = att.Filename,
                        ContentType = att.ContentType,
                        Data = att.Data
                    });
                }
            }

            await email.SendAsync();
        }
    }
}
