using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Projects;
using Sig.App.Backend.Requests.Commands.Mutations.Subscriptions;
using Sig.App.Backend.Services.Mailer;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Projects
{
    public class DeleteProjectTest : TestBase
    {
        private readonly IRequestHandler<DeleteProject.Input> handler;
        private Mock<IMailer> mailer;
        private readonly Project project;
        private readonly BeneficiaryType beneficiaryType;
        private readonly ProductGroup productGroup;

        public DeleteProjectTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            beneficiaryType = new BeneficiaryType()
            {
                Keys = "type1",
                Name = "Type 1",
                Project = project
            };
            DbContext.BeneficiaryTypes.Add(beneficiaryType);

            productGroup = new ProductGroup()
            {
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1,
                Project = project
            };
            DbContext.ProductGroups.Add(productGroup);

            DbContext.SaveChanges();

            mailer = new Mock<IMailer>();
            handler = new DeleteProject(NullLogger<DeleteProject>.Instance, DbContext, UserManager, mailer.Object, Mediator, Clock);
        }

        [Fact]
        public async Task DeleteTheProject()
        {
            var input = new DeleteProject.Input()
            {
                ProjectId = project.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var projectcount = await DbContext.Projects.CountAsync();
            projectcount.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfProjectNotFound()
        {
            var input = new DeleteProject.Input()
            {
                ProjectId = Id.New<Project>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteProject.ProjectNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfProjectHaveActiveSubscriptionNotFound()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            var nextMonthLastDay = today.AddMonths(1).AddDays(-1);

            var inputSubscription = new CreateSubscriptionInProject.Input()
            {
                ProjectId = project.GetIdentifier(),
                Name = "Subscription 1",
                StartDate = new LocalDate(today.Year, today.Month, 1),
                EndDate = new LocalDate(nextMonthLastDay.Year, nextMonthLastDay.Month, nextMonthLastDay.Day),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<CreateSubscriptionInProject.SubscriptionTypeInput>()
                {
                    new CreateSubscriptionInProject.SubscriptionTypeInput()
                    {
                        ProductGroupId = productGroup.GetIdentifier(),
                        Amount = 25,
                        BeneficiaryTypeId = beneficiaryType.GetIdentifier()
                    }
                }
            };

            await new CreateSubscriptionInProject(NullLogger<CreateSubscriptionInProject>.Instance, DbContext).Handle(inputSubscription, CancellationToken.None);

            var input = new DeleteProject.Input()
            {
                ProjectId = project.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteProject.ProjectCantHaveActiveSubscription>();
        }
    }
}
