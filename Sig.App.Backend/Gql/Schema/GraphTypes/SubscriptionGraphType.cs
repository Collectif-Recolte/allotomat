﻿using GraphQL.Conventions;
using GraphQL.DataLoader;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using System.Collections.Generic;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class SubscriptionGraphType
    {
        private readonly Subscription subscription;
        public Id Id => subscription.GetIdentifier();
        public NonNull<string> Name => subscription.Name;
        public SubscriptionMonthlyPaymentMoment MonthlyPaymentMoment => subscription.MonthlyPaymentMoment;
        public bool IsFundsAccumulable => subscription.IsFundsAccumulable;

        public SubscriptionGraphType(Subscription subscription)
        {
            this.subscription = subscription;
        }

        public IDataLoaderResult<ProjectGraphType> Project(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProject(subscription.ProjectId);
        }

        public OffsetDateTime StartDate()
        {
            return subscription.StartDate.FromUtcToOffsetDateTime();
        }

        public OffsetDateTime EndDate()
        {
            return subscription.EndDate.FromUtcToOffsetDateTime();
        }

        public OffsetDateTime? FundsExpirationDate()
        {
            if (subscription.FundsExpirationDate.HasValue)
            {
                return subscription.FundsExpirationDate.Value.FromUtcToOffsetDateTime();
            }

            return null;
        }

        public IDataLoaderResult<IEnumerable<SubscriptionTypeGraphType>> Types(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadSubscriptionSubscriptionTypes(Id.LongIdentifierForType<Subscription>());
        }

        public IDataLoaderResult<IEnumerable<BudgetAllowanceGraphType>> BudgetAllowances(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadSubscriptionBudgetAllowance(Id.LongIdentifierForType<Subscription>());
        }

        public IDataLoaderResult<bool> HaveAnyBeneficiaries(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadSubscriptionHaveAnyBeneficiaries(Id.LongIdentifierForType<Subscription>());
        }

        public OffsetDateTime GetLastDateToAssignBeneficiary()
        {
            return subscription.GetLastDateToAssignBeneficiary().FromUtcToOffsetDateTime();
        }
    }
}
