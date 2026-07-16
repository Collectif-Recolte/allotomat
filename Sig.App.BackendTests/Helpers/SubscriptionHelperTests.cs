using FluentAssertions;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Helpers
{
    public class SubscriptionHelperTests : TestBase
    {
        [Fact]
        public void GetPaymentRemaining_FirstDay_FullYear()
        {
            Clock.Reset(Instant.FromUtc(2024, 12, 31, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(12);
        }

        [Fact]
        public void GetPaymentRemaining_FirstDay_MidYear()
        {
            Clock.Reset(Instant.FromUtc(2025, 6, 1, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(6);
        }

        [Fact]
        public void GetPaymentRemaining_FifteenthDay_MidYear()
        {
            Clock.Reset(Instant.FromUtc(2025, 6, 1, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(7);
        }

        [Fact]
        public void GetPaymentRemaining_FifteenthDay_StandardRange()
        {
            Clock.Reset(Instant.FromUtc(2025, 1, 9, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 10),
                EndDate = new DateTime(2025, 12, 10),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(11);
        }

        [Fact]
        public void GetPaymentRemaining_FirstAndFifteenth_StandardRange()
        {
            Clock.Reset(Instant.FromUtc(2025, 1, 9, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 10),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(23);
        }

        [Fact]
        public void GetPaymentRemaining_FirstAndFifteenth_WithMaxLimit()
        {
            Clock.Reset(Instant.FromUtc(2024, 12, 31, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = true,
                MaxNumberOfPayments = 20
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(20);
        }

        [Fact]
        public void GetPaymentRemaining_EndedSubscription_ReturnsZero()
        {
            Clock.Reset(Instant.FromUtc(2026, 1, 1, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(0);
        }

        [Fact]
        public void GetPaymentRemaining_FifteenthDay_NoFifteenthReached()
        {
            Clock.Reset(Instant.FromUtc(2024, 12, 31, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 14),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(0);
        }

        [Fact]
        public void GetPaymentRemaining_FifteenthDay_OneCycleOnly()
        {
            Clock.Reset(Instant.FromUtc(2025, 1, 13, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 14),
                EndDate = new DateTime(2025, 1, 16),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(1);
        }

        [Fact]
        public void GetPaymentRemaining_FirstDay_AfterStart()
        {
            Clock.Reset(Instant.FromUtc(2025, 6, 1, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(6);
        }

        [Fact]
        public void GetPaymentRemaining_FifteenthDay_AfterStart()
        {
            Clock.Reset(Instant.FromUtc(2025, 6, 1, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(7);
        }

        [Fact]
        public void GetPaymentRemaining_FirstAndFifteenth_AfterStart()
        {
            Clock.Reset(Instant.FromUtc(2025, 6, 1, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(13);
        }

        [Fact]
        public void GetPaymentRemaining_FifteenthDay_JustAfterFifteenth()
        {
            Clock.Reset(Instant.FromUtc(2025, 6, 16, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(6);
        }

        [Fact]
        public void GetPaymentRemaining_AfterSubscriptionEnd_ReturnsZero()
        {
            Clock.Reset(Instant.FromUtc(2026, 1, 1, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(0);
        }

        [Fact]
        public void GetPaymentRemaining_LastDaysOfSubscription_NoRemaining()
        {
            Clock.Reset(Instant.FromUtc(2025, 1, 30, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(0);
        }

        [Fact]
        public void GetPaymentRemaining_MaxPaymentZero()
        {
            Clock.Reset(Instant.FromUtc(2024, 12, 31, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = true,
                MaxNumberOfPayments = 0
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(0);
        }

        [Fact]
        public void GetPaymentRemaining_FirstAndFifteenth_StartingOnFifteenthBeforeStart()
        {
            Clock.Reset(Instant.FromUtc(2026, 5, 12, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2026, 6, 15),
                EndDate = new DateTime(2026, 9, 20),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(7);
        }

        [Fact]
        public void GetPaymentRemaining_FifteenthDay_StartingOnFifteenthBeforeStart()
        {
            Clock.Reset(Instant.FromUtc(2026, 5, 12, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2026, 6, 15),
                EndDate = new DateTime(2026, 9, 20),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(4);
        }

        [Fact]
        public void GetPaymentRemaining_FirstAndFifteenthDayOfTheMonth_JustBeforeStartButAndOfPreviousMonth()
        {
            Clock.Reset(Instant.FromUtc(2025, 5, 31, 0, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 6, 14),
                EndDate = new DateTime(2025, 10, 2),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var result = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            result.Should().Be(8);
        }

        [Fact]
        public void GetPaymentRemaining_FirstDay_PaymentDayBeforeFundJob_IncludesToday()
        {
            Clock.Reset(Instant.FromUtc(2025, 6, 1, 6, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: false).Should().Be(7);
            subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true).Should().Be(6);
        }

        [Fact]
        public void GetPaymentRemaining_FifteenthDay_PaymentDayBeforeFundJob_IncludesToday()
        {
            Clock.Reset(Instant.FromUtc(2025, 6, 15, 6, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: false).Should().Be(7);
            subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true).Should().Be(6);
        }

        [Fact]
        public void GetPaymentRemaining_FirstAndFifteenth_PaymentDayBeforeFundJob_IncludesToday()
        {
            Clock.Reset(Instant.FromUtc(2025, 6, 15, 6, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: false).Should().Be(13);
            subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true).Should().Be(12);
        }

        [Fact]
        public void GetPaymentRemaining_NonPaymentDay_FundJobFlagDoesNotChangeResult()
        {
            Clock.Reset(Instant.FromUtc(2025, 6, 10, 6, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            var before = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: false);
            var after = subscription.GetPaymentRemaining(Clock, todaysFundJobCompleted: true);
            before.Should().Be(after);
            before.Should().Be(13);
        }

        [Fact]
        public void IsTodaysFundJobCompleted_ReturnsFalseWhenRunMissingOnPaymentDay()
        {
            var today = new DateTime(2025, 6, 15, 6, 0, 0, DateTimeKind.Utc);
            var runs = Array.Empty<Sig.App.Backend.DbModel.Entities.BackgroundJobs.AddingFundToCardRun>();

            SubscriptionHelper.IsTodaysFundJobCompleted(
                SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                today,
                runs).Should().BeFalse();
        }

        [Fact]
        public void IsTodaysFundJobCompleted_ReturnsTrueWhenRunExistsOnPaymentDay()
        {
            var today = new DateTime(2025, 6, 15, 8, 0, 0, DateTimeKind.Utc);
            var runs = new[]
            {
                new Sig.App.Backend.DbModel.Entities.BackgroundJobs.AddingFundToCardRun
                {
                    Name = SubscriptionHelper.AddingFundToCardFifteenthDayOfTheMonthJobName,
                    Date = today
                }
            };

            SubscriptionHelper.IsTodaysFundJobCompleted(
                SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                today,
                runs).Should().BeTrue();
        }

        [Fact]
        public void IsTodaysFundJobCompleted_ReturnsTrueOnNonPaymentDay()
        {
            var today = new DateTime(2025, 6, 10, 6, 0, 0, DateTimeKind.Utc);

            SubscriptionHelper.IsTodaysFundJobCompleted(
                SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                today,
                Array.Empty<Sig.App.Backend.DbModel.Entities.BackgroundJobs.AddingFundToCardRun>()).Should().BeTrue();
        }

        [Fact]
        public async Task GetPaymentRemainingAsync_ResolvesFundJobStatusFromDb()
        {
            Clock.Reset(Instant.FromUtc(2025, 6, 15, 6, 0));

            var subscription = new Subscription
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false
            };

            (await subscription.GetPaymentRemainingAsync(DbContext, Clock)).Should().Be(7);

            DbContext.AddingFundToCardRuns.Add(new Sig.App.Backend.DbModel.Entities.BackgroundJobs.AddingFundToCardRun
            {
                Name = SubscriptionHelper.AddingFundToCardFifteenthDayOfTheMonthJobName,
                Date = new DateTime(2025, 6, 15, 8, 0, 0, DateTimeKind.Utc)
            });
            await DbContext.SaveChangesAsync();

            (await subscription.GetPaymentRemainingAsync(DbContext, Clock)).Should().Be(6);
        }

        [Theory]
        [InlineData(0, 2, 0)]
        [InlineData(6, 2, 3)]
        [InlineData(9, 3, 3)]
        [InlineData(5, 1, 5)]
        public void GetNumberOfPaymentsMade_DividesTransactionsByPaymentTypes(int transactionCount, int numberOfPaymentTypes, int expected)
        {
            SubscriptionHelper.GetNumberOfPaymentsMade(transactionCount, numberOfPaymentTypes).Should().Be(expected);
        }

        [Fact]
        public void GetNumberOfPaymentsMade_ZeroPaymentTypes_ReturnsZero()
        {
            SubscriptionHelper.GetNumberOfPaymentsMade(6, 0).Should().Be(0);
        }

        [Fact]
        public void GetNumberOfPaymentTypes_CountsTypesForBeneficiaryType()
        {
            var subscription = new Subscription
            {
                Types = new List<SubscriptionType>
                {
                    new SubscriptionType { Amount = 25, BeneficiaryTypeId = 1 },
                    new SubscriptionType { Amount = 25, BeneficiaryTypeId = 1 },
                    new SubscriptionType { Amount = 50, BeneficiaryTypeId = 2 },
                    new SubscriptionType { Amount = 100, BeneficiaryTypeId = null }
                }
            };

            subscription.GetNumberOfPaymentTypes(1).Should().Be(2);
            subscription.GetNumberOfPaymentTypes(2).Should().Be(1);
            subscription.GetNumberOfPaymentTypes(99).Should().Be(0);
        }

        [Fact]
        public void GetExplicitMaxNumberOfPayments_NullWhenNoOverrideAndNoSubscriptionMax()
        {
            var subscriptionBeneficiary = new SubscriptionBeneficiary
            {
                MaxNumberOfPaymentsOverride = null,
                Subscription = new Subscription { MaxNumberOfPayments = null }
            };

            subscriptionBeneficiary.GetExplicitMaxNumberOfPayments().Should().BeNull();
        }

        [Fact]
        public void GetExplicitMaxNumberOfPayments_UsesSubscriptionMaxWhenNoOverride()
        {
            var subscriptionBeneficiary = new SubscriptionBeneficiary
            {
                MaxNumberOfPaymentsOverride = null,
                Subscription = new Subscription { MaxNumberOfPayments = 5 }
            };

            subscriptionBeneficiary.GetExplicitMaxNumberOfPayments().Should().Be(5);
        }

        [Fact]
        public void GetExplicitMaxNumberOfPayments_OverrideTakesPrecedence()
        {
            var subscriptionBeneficiary = new SubscriptionBeneficiary
            {
                MaxNumberOfPaymentsOverride = 8,
                Subscription = new Subscription { MaxNumberOfPayments = 5 }
            };

            subscriptionBeneficiary.GetExplicitMaxNumberOfPayments().Should().Be(8);
        }
    }
}