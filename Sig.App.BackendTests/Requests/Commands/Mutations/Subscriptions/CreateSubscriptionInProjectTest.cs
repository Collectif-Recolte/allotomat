using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Subscriptions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Subscriptions
{
    public class CreateSubscriptionInProjectTest : TestBase
    {
        private readonly CreateSubscriptionInProject handler;
        private readonly Project project;
        private readonly BeneficiaryType beneficiaryType;
        private readonly ProductGroup productGroup;

        public CreateSubscriptionInProjectTest()
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

            handler = new CreateSubscriptionInProject(NullLogger<CreateSubscriptionInProject>.Instance, DbContext);
        }

        [Fact]
        public async Task AddSubscriptionInProject()
        {
            var input = new CreateSubscriptionInProject.Input()
            {
                ProjectId = project.GetIdentifier(),
                Name = "Subscription 1",
                StartDate = new LocalDate(2022, 1, 1),
                EndDate = new LocalDate(2022, 3, 30),
                FundsExpirationDate = new LocalDate(2022, 4, 1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsFundsAccumulable = true,
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

            await handler.Handle(input, CancellationToken.None);

            var localProject = await DbContext.Projects.FirstAsync();
            var localSubscription = await DbContext.Subscriptions.FirstAsync();

            localProject.Subscriptions.Should().HaveCount(1);
            localSubscription.ProjectId.Should().Be(project.Id);
        }

        [Fact]
        public async Task ThrowsIfProjectNotFound()
        {
            var input = new CreateSubscriptionInProject.Input()
            {
                ProjectId = Id.New<Project>(123456),
                Name = "Subscription 1",
                StartDate = new LocalDate(2022, 1, 1),
                EndDate = new LocalDate(2022, 3, 30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<CreateSubscriptionInProject.SubscriptionTypeInput>()
                {
                    new CreateSubscriptionInProject.SubscriptionTypeInput()
                    {
                        Amount = 25
                    },
                    new CreateSubscriptionInProject.SubscriptionTypeInput()
                    {
                        Amount = 50
                    },
                    new CreateSubscriptionInProject.SubscriptionTypeInput()
                    {
                        Amount = 100
                    }
                }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateSubscriptionInProject.ProjectNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfStartDateAfterEndDate()
        {
            var input = new CreateSubscriptionInProject.Input()
            {
                ProjectId = project.GetIdentifier(),
                Name = "Subscription 1",
                StartDate = new LocalDate(2022, 3, 30),
                EndDate = new LocalDate(2022, 2, 1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<CreateSubscriptionInProject.SubscriptionTypeInput>()
                {
                    new CreateSubscriptionInProject.SubscriptionTypeInput()
                    {
                        Amount = 25
                    },
                    new CreateSubscriptionInProject.SubscriptionTypeInput()
                    {
                        Amount = 50
                    },
                    new CreateSubscriptionInProject.SubscriptionTypeInput()
                    {
                        Amount = 100
                    }
                }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateSubscriptionInProject.EndDateMustBeAfterStartDateException>();
        }

        [Fact]
        public async Task ThrowsIfMaxNumberOfPaymentsCantBeZero()
        {
            var input = new CreateSubscriptionInProject.Input()
            {
                ProjectId = project.GetIdentifier(),
                Name = "Subscription 1",
                StartDate = new LocalDate(2022, 3, 30),
                EndDate = new LocalDate(2022, 4, 1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<CreateSubscriptionInProject.SubscriptionTypeInput>()
                {
                    new CreateSubscriptionInProject.SubscriptionTypeInput()
                    {
                        Amount = 25
                    },
                    new CreateSubscriptionInProject.SubscriptionTypeInput()
                    {
                        Amount = 50
                    },
                    new CreateSubscriptionInProject.SubscriptionTypeInput()
                    {
                        Amount = 100
                    }
                },
                MaxNumberOfPayments = 0,
                IsSubscriptionPaymentBasedCardUsage = true
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateSubscriptionInProject.MaxNumberOfPaymentsCantBeZeroException>();
        }

        [Fact]
        public async Task ThrowsIfNumberDaysUntilFundsExpireCantBeZero()
        {
            var input = new CreateSubscriptionInProject.Input()
            {
                ProjectId = project.GetIdentifier(),
                Name = "Subscription 1",
                StartDate = new LocalDate(2022, 3, 30),
                EndDate = new LocalDate(2022, 6, 1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<CreateSubscriptionInProject.SubscriptionTypeInput>()
                {
                    new CreateSubscriptionInProject.SubscriptionTypeInput()
                    {
                        Amount = 25
                    },
                    new CreateSubscriptionInProject.SubscriptionTypeInput()
                    {
                        Amount = 50
                    },
                    new CreateSubscriptionInProject.SubscriptionTypeInput()
                    {
                        Amount = 100
                    }
                },
                TriggerFundExpiration = FundsExpirationTrigger.NumberOfDays,
                NumberDaysUntilFundsExpire = 0
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateSubscriptionInProject.NumberDaysUntilFundsExpireCantBeZeroException>();
        }
    }
}
