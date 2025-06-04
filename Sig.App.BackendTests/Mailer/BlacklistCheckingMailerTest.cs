using System.Threading.Tasks;
using Moq;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Services.Mailer;
using Xunit;

namespace Sig.App.BackendTests.Mailer
{
    public class BlacklistCheckingMailerTests : TestBase
    {
        private readonly Mock<IMailer> innerMailer;
        private readonly BlacklistCheckingMailer mailer;

        public BlacklistCheckingMailerTests()
        {
            innerMailer = new Mock<IMailer>();
            mailer = new BlacklistCheckingMailer(innerMailer.Object,
                new EmailBlacklistService(DbContext, Clock, new EmailBlacklistCache()),
                Logger<BlacklistCheckingMailer>());

            DbContext.BlacklistedEmails.Add(new BlacklistedEmail
            {
                Email = "blacklisted@example.com"
            });
            DbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task DoesNotSendEmailToBlackListedAddress()
        {
            await mailer.Send(new TestEmail("blacklisted@example.com"));

            innerMailer.Verify(x => x.Send(It.IsAny<EmailModel>()), Times.Never);
        }

        [Fact]
        public async Task IgnoresCaseAndTrimsWhitespaceWhenCheckingBlacklist()
        {
            await mailer.Send(new TestEmail(" BlackListed@Example.com "));

            innerMailer.Verify(x => x.Send(It.IsAny<EmailModel>()), Times.Never);
        }

        [Fact]
        public async Task SendsEmailToEmailNotOnBlacklist()
        {
            await mailer.Send(new TestEmail("regular@example.com"));

            innerMailer.Verify(x => x.Send(It.IsAny<EmailModel>()), Times.Once);
        }

        public class TestEmail : EmailModel
        {
            public TestEmail(string to) : base(to)
            {
            }

            public override string Subject { get; }
        }
    }
}