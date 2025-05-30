using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Sig.App.Backend.Services.Mailer;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Sig.App.Backend.Controllers
{
    [Route("sns")]
    public class AmazonSnsController : ControllerBase
    {
        private readonly ILogger<AmazonSnsController> logger;
        private readonly IEmailBlacklistService blacklist;
        private readonly IHttpClientFactory httpClientFactory;

        public AmazonSnsController(ILogger<AmazonSnsController> logger, IEmailBlacklistService blacklist, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger;
            this.blacklist = blacklist;
            this.httpClientFactory = httpClientFactory;
        }

        [HttpPost("bounce")]
        public async Task<IActionResult> HandleBounce()
        {
            var notification = await ReadFromRequestBody<SesNotification>();

            if (notification == null)
            {
                logger.LogError("HandleBounce called with no notification");
                return BadRequest();
            }

            if (!string.IsNullOrWhiteSpace(notification.SubscribeURL))
            {
                logger.LogInformation($"Received SNS subscription confirmation. Issuing HTTP GET to {notification.SubscribeURL}");
                await httpClientFactory.CreateClient().GetAsync(notification.SubscribeURL);
                return NoContent();
            }

            if (notification.Bounce != null)
            {
                await ProcessBounce(notification);
            }
            else if (notification.Complaint != null)
            {
                await ProcessComplaint(notification);
            }
            else
            {
                logger.LogError($"HandleBounce called with no bounce or complaint (notification type: {notification.NotificationType})");
                return BadRequest();
            }

            return NoContent();
        }

        private async Task<T> ReadFromRequestBody<T>()
        {
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var json = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        private async Task ProcessBounce(SesNotification notification)
        {
            var bounce = notification.Bounce;

            if (!bounce.IsPermanent)
            {
                logger.LogInformation($"Ignoring {bounce.BounceType} bounce report (not permanent)\n{bounce}");
                return;
            }

            foreach (var recipient in bounce.BouncedRecipients)
            {
                await blacklist.AddToBlacklist(recipient.EmailAddress,
                    $"Bounce - {bounce.BounceType} - {bounce.BounceSubType}", notification.Mail.Source,
                    notification.Mail.CommonHeaders.Date, notification.Mail.CommonHeaders.Subject);
            }

            logger.LogInformation($"Handled bounce report\n{bounce}");
        }

        private async Task ProcessComplaint(SesNotification notification)
        {
            var complaint = notification.Complaint;

            foreach (var recipient in complaint.ComplainedRecipients)
            {
                await blacklist.AddToBlacklist(recipient.EmailAddress, $"Complaint - {complaint.ComplaintFeedbackType}",
                    notification.Mail.Source, notification.Mail.CommonHeaders.Date,
                    notification.Mail.CommonHeaders.Subject);
            }

            logger.LogInformation($"Handled complaint report\n{complaint}");
        }


        public class SesNotification
        {
            public string SubscribeURL { get; set; }

            public string NotificationType { get; set; }

            public MailInfos Mail { get; set; }
            public BounceInfos Bounce { get; set; }
            public ComplaintInfos Complaint { get; set; }

            public class MailInfos
            {
                public string Source { get; set; }
                public Headers CommonHeaders { get; set; }

                public class Headers
                {
                    public DateTime Date { get; set; }
                    public String Subject { get; set; }
                }
            }

            public class BounceInfos
            {
                public string BounceType { get; set; }
                public string BounceSubType { get; set; }
                public BouncedRecipient[] BouncedRecipients { get; set; }
                public string Timestamp { get; set; }
                public string FeedbackId { get; set; }

                public string RemoteMtaIp { get; set; }
                public string ReportingMTA { get; set; }

                public bool IsPermanent => BounceType == "Permanent";

                public class BouncedRecipient
                {
                    public string EmailAddress { get; set; }

                    public string Status { get; set; }
                    public string Action { get; set; }
                    public string DiagnosticCode { get; set; }

                    public override string ToString() => $"{EmailAddress} - Status: {Status}, Action: {Action}, DiagnosticCode: {DiagnosticCode}";
                }

                public override string ToString()
                {
                    var sb = new StringBuilder();

                    sb.Append("Type: ");
                    sb.AppendLine(BounceType);
                    sb.Append("SubType: ");
                    sb.AppendLine(BounceSubType);
                    sb.Append("Timestamp: ");
                    sb.AppendLine(Timestamp);
                    sb.Append("FeedbackId: ");
                    sb.AppendLine(FeedbackId);
                    sb.Append("RemoteMtaIp: ");
                    sb.AppendLine(RemoteMtaIp);
                    sb.Append("ReportingMta: ");
                    sb.AppendLine(ReportingMTA);
                    sb.AppendLine("Recipients:");
                    foreach (var recipient in BouncedRecipients)
                    {
                        sb.AppendLine(recipient.ToString());
                    }

                    return sb.ToString();
                }
            }

            public class ComplaintInfos
            {
                public ComplainedRecipient[] ComplainedRecipients { get; set; }
                public string Timestamp { get; set; }
                public string FeedbackId { get; set; }

                public string UserAgent { get; set; }
                public string ComplaintFeedbackType { get; set; }
                public string ArrivalDate { get; set; }

                public class ComplainedRecipient
                {
                    public string EmailAddress { get; set; }

                    public override string ToString() => EmailAddress;
                }

                public override string ToString()
                {
                    var sb = new StringBuilder();

                    sb.Append("Timestamp: ");
                    sb.AppendLine(Timestamp);
                    sb.Append("FeedbackId: ");
                    sb.AppendLine(FeedbackId);
                    sb.Append("UserAgent: ");
                    sb.AppendLine(UserAgent);
                    sb.Append("ComplaintFeedbackType: ");
                    sb.AppendLine(ComplaintFeedbackType);
                    sb.Append("ArrivalDate: ");
                    sb.AppendLine(ArrivalDate);
                    sb.AppendLine("Recipients:");
                    foreach (var recipient in ComplainedRecipients)
                    {
                        sb.AppendLine(recipient.ToString());
                    }

                    return sb.ToString();
                }
            }
        }
    }
}