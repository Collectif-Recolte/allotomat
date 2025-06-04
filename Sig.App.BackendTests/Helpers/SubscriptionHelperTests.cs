using FluentAssertions;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Helpers;
using System;
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

            var result = subscription.GetPaymentRemaining(Clock);
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

            var result = subscription.GetPaymentRemaining(Clock);
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

            var result = subscription.GetPaymentRemaining(Clock);
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

            var result = subscription.GetPaymentRemaining(Clock);
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

            var result = subscription.GetPaymentRemaining(Clock);
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

            var result = subscription.GetPaymentRemaining(Clock);
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

            var result = subscription.GetPaymentRemaining(Clock);
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

            var result = subscription.GetPaymentRemaining(Clock);
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

            var result = subscription.GetPaymentRemaining(Clock);
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

            var result = subscription.GetPaymentRemaining(Clock);
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

            var result = subscription.GetPaymentRemaining(Clock);
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

            var result = subscription.GetPaymentRemaining(Clock);
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

            var result = subscription.GetPaymentRemaining(Clock);
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

            var result = subscription.GetPaymentRemaining(Clock);
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

            var result = subscription.GetPaymentRemaining(Clock);
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

            var result = subscription.GetPaymentRemaining(Clock);
            result.Should().Be(0);
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

            var result = subscription.GetPaymentRemaining(Clock);
            result.Should().Be(8);
        }
    }
}