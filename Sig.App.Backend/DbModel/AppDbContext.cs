using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Linq;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Transactions;
using System.Collections.Generic;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Entities.BackgroundJobs;

namespace Sig.App.Backend.DbModel
{
    public class AppDbContext : IdentityDbContext<
            AppUser,
            IdentityRole,
            string,
            IdentityUserClaim<string>,
            IdentityUserRole<string>,
            IdentityUserLogin<string>,
            IdentityRoleClaim<string>,
            IdentityUserToken<string>>,
            IDataProtectionKeyContext
    {
        public AppDbContext(DbContextOptions options) : base(options) { }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        public DbSet<UserProfile> UserProfiles { get; set; }

        public DbSet<Project> Projects { get; set; }

        public DbSet<Market> Markets { get; set; }

        public DbSet<ProjectMarket> ProjectMarkets { get; set; }

        public DbSet<OrganizationMarket> OrganizationMarkets { get; set; }

        public DbSet<Organization> Organizations { get; set; }

        public DbSet<Subscription> Subscriptions { get; set; }

        public DbSet<SubscriptionType> SubscriptionTypes { get; set; }

        public DbSet<Beneficiary> Beneficiaries { get; set; }

        public DbSet<SubscriptionBeneficiary> SubscriptionBeneficiaries { get; set; }

        public DbSet<Card> Cards { get; set; }

        public DbSet<Transaction> Transactions { get; set; }
        
        public DbSet<TransactionLog> TransactionLogs { get; set; }
        
        public DbSet<TransactionLogProductGroup> TransactionLogProductGroups { get; set; }

        public DbSet<BeneficiaryType> BeneficiaryTypes { get; set; }

        public DbSet<BudgetAllowance> BudgetAllowances { get; set; }

        public DbSet<ProductGroup> ProductGroups { get; set; }

        public DbSet<Fund> Funds { get; set; }

        public DbSet<PaymentTransactionProductGroup> PaymentTransactionProductGroups { get; set; }

        public DbSet<RefundTransactionProductGroup> RefundTransactionProductGroups { get; set; }

        public DbSet<PaymentFund> PaymentFunds { get; set; }

        public DbSet<PaymentTransactionAddingFundTransaction> PaymentTransactionAddingFundTransactions { get; set; }

        public DbSet<AddingFundToCardRun> AddingFundToCardRuns { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            Configure<AppUser>(_ =>
            {
                _.HasOne(x => x.Profile)
                    .WithOne(x => x.User)
                    .HasForeignKey<UserProfile>(x => x.UserId);

                _.Property(x => x.Id).ValueGeneratedOnAdd();
            });

            Configure<ProjectMarket>(_ => {
                _.HasKey(x => new { x.MarketId, x.ProjectId });

                _.HasOne(x => x.Project)
                    .WithMany(x => x.Markets)
                    .HasForeignKey(x => x.ProjectId);

                _.HasOne(x => x.Market)
                    .WithMany(x => x.Projects)
                    .HasForeignKey(x => x.MarketId);
            });

            Configure<OrganizationMarket>(_ => {
                _.HasKey(x => new { x.MarketId, x.OrganizationId});

                _.HasOne(x => x.Organization)
                    .WithMany(x => x.Markets)
                    .HasForeignKey(x => x.OrganizationId);

                _.HasOne(x => x.Market)
                    .WithMany(x => x.Organizations)
                    .HasForeignKey(x => x.MarketId);
            });

            Configure<Organization>(_ =>
            {
                _.HasOne(x => x.Project)
                    .WithMany(x => x.Organizations)
                    .HasForeignKey(x => x.ProjectId);
            });

            Configure<Subscription>(_ =>
            {
               _.HasOne(x => x.Project)
                    .WithMany(x => x.Subscriptions)
                    .HasForeignKey(x => x.ProjectId);
            });

            Configure<SubscriptionType>(_ =>
            {
                _.HasOne(x => x.Subscription)
                     .WithMany(x => x.Types)
                     .HasForeignKey(x => x.SubscriptionId);

                _.HasOne(x => x.ProductGroup)
                    .WithMany(x => x.Types)
                    .HasForeignKey(x => x.ProductGroupId);
            });

            Configure<BeneficiaryType>(_ =>
            {
                _.HasOne(x => x.Project)
                    .WithMany(x => x.BeneficiaryTypes)
                    .HasForeignKey(x => x.ProjectId);

                _.HasMany(x => x.Beneficiaries)
                    .WithOne(x => x.BeneficiaryType)
                    .HasForeignKey(x => x.BeneficiaryTypeId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            Configure<Beneficiary>(_ =>
            {
                _.HasOne(x => x.Organization)
                     .WithMany(x => x.Beneficiaries)
                     .HasForeignKey(x => x.OrganizationId);

                _.HasOne(x => x.Card)
                    .WithOne(x => x.Beneficiary)
                    .OnDelete(DeleteBehavior.Restrict);

                _.HasOne(x => x.BeneficiaryType)
                    .WithMany(x => x.Beneficiaries)
                    .HasForeignKey(x => x.BeneficiaryTypeId)
                    .OnDelete(DeleteBehavior.NoAction);

                _.Property(x => x.Id).ValueGeneratedOnAdd();
            });

            Configure<SubscriptionBeneficiary>(_ => {
                _.HasKey(x => new { x.BeneficiaryId, x.SubscriptionId });

                _.HasOne(x => x.Beneficiary)
                    .WithMany(x => x.Subscriptions)
                    .HasForeignKey(x => x.BeneficiaryId);

                _.HasOne(x => x.Subscription)
                    .WithMany(x => x.Beneficiaries)
                    .HasForeignKey(x => x.SubscriptionId);

                _.HasOne(x => x.BudgetAllowance)
                    .WithMany(x => x.Beneficiaries)
                    .HasForeignKey(x => x.BudgetAllowanceId);
            });

            Configure<Card>(_ =>
            {
                _.HasOne(x => x.Project)
                     .WithMany(x => x.Cards)
                     .HasForeignKey(x => x.ProjectId);

                _.HasMany(x => x.Transactions)
                    .WithOne(x => x.Card)
                    .OnDelete(DeleteBehavior.NoAction);

                _.HasMany(x => x.Funds)
                    .WithOne(x => x.Card)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            Configure<TransactionLog>(_ =>
            {
                _.HasMany(x => x.TransactionLogProductGroups).WithOne(x => x.TransactionLog);

                _.HasIndex(x => x.BeneficiaryId);
                _.HasIndex(x => x.ProjectId);
                _.HasIndex(x => x.MarketId);
                _.HasIndex(x => x.OrganizationId);
                _.HasIndex(x => x.SubscriptionId);
                _.HasIndex(x => x.BeneficiaryTypeId);
                _.HasIndex(x => x.TransactionInitiatorId);
                _.HasIndex(x => x.TransactionUniqueId);
            });
            
            Configure<TransactionLogProductGroup>(_ =>
            {
                _.HasIndex(x => x.ProductGroupId);
            });

            Configure<SubscriptionAddingFundTransaction>(_ => {
                _.HasOne(x => x.Beneficiary);

                _.HasOne(x => x.Organization);

                _.HasMany(x => x.Transactions);

                _.HasOne(x => x.SubscriptionType).WithMany().OnDelete(DeleteBehavior.NoAction);

                _.HasOne(x => x.ProductGroup).WithMany().OnDelete(DeleteBehavior.NoAction);
            });

            Configure<LoyaltyAddingFundTransaction>(_ => {
                _.HasOne(x => x.Beneficiary);

                _.HasMany(x => x.Transactions);

                _.HasOne(x => x.ProductGroup).WithMany().OnDelete(DeleteBehavior.NoAction);
            });

            Configure<ManuallyAddingFundTransaction>(_ => {
                _.HasOne(x => x.Beneficiary);

                _.HasOne(x => x.Organization);

                _.HasMany(x => x.Transactions);

                _.HasOne(x => x.Subscription).WithMany().OnDelete(DeleteBehavior.NoAction);

                _.HasOne(x => x.ProductGroup).WithMany().OnDelete(DeleteBehavior.NoAction);

                _.HasMany(x => x.AffectedNegativeFundTransactions).WithMany(x => x.ManuallNegativeFundTransactions);
            });

            Configure<ExpireFundTransaction>(_ => {
                _.HasOne(x => x.Beneficiary);

                _.HasOne(x => x.Organization);

                _.HasOne(x => x.ExpiredSubscription).WithMany().OnDelete(DeleteBehavior.NoAction);

                _.HasOne(x => x.ProductGroup).WithMany().OnDelete(DeleteBehavior.NoAction);

                _.HasOne(x => x.AddingFundTransaction).WithOne(x => x.ExpireFundTransaction).HasForeignKey<AddingFundTransaction>(x => x.ExpireFundTransactionId).OnDelete(DeleteBehavior.NoAction);
            });

            Configure<OffPlatformAddingFundTransaction>(_ => {
                _.HasOne(x => x.Beneficiary);

                _.HasOne(x => x.Organization);

                _.HasMany(x => x.Transactions);

                _.HasOne(x => x.ProductGroup).WithMany().OnDelete(DeleteBehavior.NoAction);
            });

            Configure<PaymentTransaction>(_ => {
                _.HasOne(x => x.Beneficiary);

                _.HasOne(x => x.Organization);

                _.HasMany(x => x.PaymentTransactionAddingFundTransactions)
                    .WithOne(x => x.PaymentTransaction)
                    .OnDelete(DeleteBehavior.NoAction);

                _.HasMany(x => x.Transactions)
                    .WithMany(x => x.Transactions)
                    .UsingEntity<Dictionary<string, object>>(
                        j => j.HasOne<AddingFundTransaction>().WithMany().OnDelete(DeleteBehavior.Cascade),
                        j => j.HasOne<PaymentTransaction>().WithMany().OnDelete(DeleteBehavior.ClientCascade));
            });

            Configure<AddingFundTransaction>(_ =>
            {
                _.HasMany(x => x.PaymentTransactionAddingFundTransactions)
                    .WithOne(x => x.AddingFundTransaction)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            Configure<RefundTransaction>(_ => {
                _.HasOne(x => x.Beneficiary);

                _.HasOne(x => x.Organization);

                _.HasMany(x => x.RefundByProductGroups).WithOne(x => x.RefundTransaction).OnDelete(DeleteBehavior.NoAction);

                _.HasOne(x => x.InitialTransaction)
                    .WithMany(x => x.RefundTransactions)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            Configure<RefundTransactionProductGroup>(_ => {
                _.HasOne(x => x.ProductGroup).WithMany().OnDelete(DeleteBehavior.NoAction);
            });

            Configure<BudgetAllowance>(_ =>
            {
                _.HasOne(x => x.Organization)
                    .WithMany(x => x.BudgetAllowances)
                    .HasForeignKey(x => x.OrganizationId);

                _.HasOne(x => x.Subscription)
                    .WithMany(x => x.BudgetAllowances)
                    .HasForeignKey(x => x.SubscriptionId)
                    .OnDelete(DeleteBehavior.NoAction);

                _.HasMany(x => x.Beneficiaries)
                    .WithOne(x => x.BudgetAllowance)
                    .HasForeignKey(x => x.BudgetAllowanceId);
            });

            Configure<ProductGroup>(_ =>
            {
                _.HasOne(x => x.Project)
                    .WithMany(x => x.ProductGroups)
                    .HasForeignKey(x => x.ProjectId);

                _.HasMany(x => x.Types)
                    .WithOne(x => x.ProductGroup)
                    .HasForeignKey(x => x.ProductGroupId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            Configure<Fund>(_ =>
            {
                _.HasOne(x => x.Card)
                    .WithMany(x => x.Funds)
                    .HasForeignKey(x => x.CardId);

                _.HasOne(x => x.ProductGroup)
                    .WithMany(x => x.Funds)
                    .HasForeignKey(x => x.ProductGroupId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            Configure<PaymentFund>(_ =>
            {
                _.HasOne(x => x.ProductGroup)
                    .WithMany()
                    .HasForeignKey(x => x.ProductGroupId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            void Configure<TEntity>(Action<EntityTypeBuilder<TEntity>> action) where TEntity : class => action(builder.Entity<TEntity>());
        }

        public void RejectChanges(bool detachAll)
        {
            // https://stackoverflow.com/a/22098063
            foreach (var entry in ChangeTracker.Entries().ToList())
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified; //Revert changes made to deleted entity.
                        entry.State = detachAll ? EntityState.Detached : EntityState.Unchanged;
                        break;
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                    default:
                        if (detachAll) entry.State = EntityState.Detached;
                        break;
                }
            }
        }
    }
}
