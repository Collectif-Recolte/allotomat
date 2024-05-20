using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.Helpers;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;

namespace Sig.App.Backend.DataSeeders;

public class DevDataSeeder : IDataSeeder
{
    private readonly AppDbContext db;
    private readonly UserManager<AppUser> userManager;
    private readonly ILogger<DevDataSeeder> logger;
    private readonly IClock clock;

    public DevDataSeeder(AppDbContext db, UserManager<AppUser> userManager, ILogger<DevDataSeeder> logger, IClock clock)
    {
        this.db = db;
        this.userManager = userManager;
        this.logger = logger;
        this.clock = clock;
    }

    public async Task Seed()
    {
        logger.LogInformation("Seeding dev data");
        if(await db.Users.Where(x => x.Type == UserType.PCAAdmin).AnyAsync())
        {
            logger.LogInformation("Seeding canceled: Tomat Admin already exist");
            return;
        }
        
        await SeedDevUsers();
        await SeedDevProjects();
        await SeedDevMarkets();
        await SeedDevOrganizations();
        await SeedDevProductGroups();
        await SeedDevSubscriptions();
        await SeedDevBudgetAllowance();
        await SeedDevBeneficiaries();
        await SeedDevCards();
        await SeedDevTransactions();
    }

    private async Task SeedDevUsers()
    {
        const string defaultPassword = "Abcd1234!!";

        // Create dev users
        await userManager.CreateOrUpdateAsync(db, "outils+admin1@sigmund.ca", "Admin1", "Example", UserType.PCAAdmin, defaultPassword);
        await userManager.CreateOrUpdateAsync(db, "outils+admin2@sigmund.ca", "Admin2", "Example", UserType.PCAAdmin, defaultPassword);

        await userManager.CreateOrUpdateAsync(db, "outils+project1@sigmund.ca", "Project1", "Example", UserType.ProjectManager, defaultPassword);
        await userManager.CreateOrUpdateAsync(db, "outils+project2@sigmund.ca", "Project2", "Example", UserType.ProjectManager, defaultPassword);

        await userManager.CreateOrUpdateAsync(db, "outils+group1@sigmund.ca", "Group1", "Example", UserType.OrganizationManager, defaultPassword);
        await userManager.CreateOrUpdateAsync(db, "outils+group2@sigmund.ca", "Group2", "Example", UserType.OrganizationManager, defaultPassword);

        await userManager.CreateOrUpdateAsync(db, "outils+merchant1@sigmund.ca", "Merchant1", "Example", UserType.Merchant, defaultPassword);
        await userManager.CreateOrUpdateAsync(db, "outils+merchant2@sigmund.ca", "Merchant2", "Example", UserType.Merchant, defaultPassword);
    }

    private async Task SeedDevProjects()
    {
        if (db.Projects.Any(x => x.Name == "SeedDev - Programme 1"))
        {
            return;
        }

        var project = new Project()
        {
            Name = "SeedDev - Programme 1"
        };
        string[] managerEmails = { "outils+project1@sigmund.ca", "outils+project2@sigmund.ca" };
        await db.Projects.AddAsync(project);

        await db.SaveChangesAsync();

        foreach (var email in managerEmails)
        {
            var manager = await GetUserByEmailAndType(email, UserType.ProjectManager);
            if (manager != null)
            {
                var existingClaims = await userManager.GetClaimsAsync(manager);
                if (existingClaims.Any(c => c.Type == AppClaimTypes.ProjectManagerOf))
                    continue;

                await userManager.AddClaimAsync(manager, new Claim(AppClaimTypes.ProjectManagerOf, project.Id.ToString()));
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedDevMarkets()
    {
        if (db.Markets.Any(x => x.Name == "SeedDev - Commerce 1"))
        {
            return;
        }

        var project = db.Projects.First(x => x.Name == "SeedDev - Programme 1");
        
        var market = new Market()
        {
            Name = "SeedDev - Commerce 1",
            Projects = new List<ProjectMarket>()
        };
        string[] managerEmails = { "outils+merchant1@sigmund.ca", "outils+merchant2@sigmund.ca" };
        var marketResult = await db.Markets.AddAsync(market);
        market = marketResult.Entity;
        
        var projectMarket = new ProjectMarket() {Project = project, Market = market};
        db.ProjectMarkets.Add(projectMarket);

        await db.SaveChangesAsync();

        foreach (var email in managerEmails)
        {
            var manager = await GetUserByEmailAndType(email, UserType.Merchant);
            if (manager != null)
            {
                var existingClaims = await userManager.GetClaimsAsync(manager);
                if (existingClaims.Any(c => c.Type == AppClaimTypes.MarketManagerOf))
                    continue;

                await userManager.AddClaimAsync(manager, new Claim(AppClaimTypes.MarketManagerOf, market.Id.ToString()));
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedDevOrganizations()
    {
        if (db.Organizations.Any(x => x.Name == "SeedDev - Groupe 1"))
        {
            return;
        }

        var organization = new Organization()
        {
            Name = "SeedDev - Groupe 1",
            Project = db.Projects.First(x => x.Name == "SeedDev - Programme 1")
        };
        string[] managerEmails = { "outils+group1@sigmund.ca", "outils+group2@sigmund.ca" };
        await db.Organizations.AddAsync(organization);

        await db.SaveChangesAsync();

        foreach (var email in managerEmails)
        {
            var manager = await GetUserByEmailAndType(email, UserType.OrganizationManager);
            if (manager != null)
            {
                var existingClaims = await userManager.GetClaimsAsync(manager);
                if (existingClaims.Any(c => c.Type == AppClaimTypes.OrganizationManagerOf))
                    continue;

                await userManager.AddClaimAsync(manager, new Claim(AppClaimTypes.OrganizationManagerOf, organization.Id.ToString()));
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedDevProductGroups()
    {
        if (db.ProductGroups.Any(x => x.Name == "SeedDev - Groupe1"))
        {
            return;
        }

        var project = db.Projects.Include(x => x.BeneficiaryTypes).First(x => x.Name == "SeedDev - Programme 1");

        var productGroup = new ProductGroup()
        {
            Name = "SeedDev - Groupe1",
            Color = ProductGroupColor.Color_1,
            OrderOfAppearance = 1,
            Project = project
        };

        var productGroupLoyalty = new ProductGroup()
        {
            Name = ProductGroupType.LOYALTY,
            Color = ProductGroupColor.Color_0,
            OrderOfAppearance = -1,
            Project = project
        };

        await db.ProductGroups.AddAsync(productGroup);
        await db.ProductGroups.AddAsync(productGroupLoyalty);
        await db.SaveChangesAsync();
    }

    private async Task SeedDevSubscriptions()
    {
        if (db.Subscriptions.Any(x => x.Name == "SeedDev - Période 1"))
        {
            return;
        }
        
        var project = db.Projects.Include(x => x.BeneficiaryTypes).First(x => x.Name == "SeedDev - Programme 1");
        var productGroup = db.ProductGroups.First(x => x.Name == "SeedDev - Groupe1");

        var subscription = new Subscription()
        {
            Name = "SeedDev - Période 1",
            StartDate = DateTime.Today.AddMonths(-1),
            EndDate = DateTime.Today.AddMonths(2),
            FundsExpirationDate = DateTime.Today.AddMonths(3),
            MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth,
            Beneficiaries = new List<SubscriptionBeneficiary>(),
            Project = project,
            Types = new List<SubscriptionType>()
        };
        var subscriptionResult = await db.Subscriptions.AddAsync(subscription);
        subscription = subscriptionResult.Entity;

        project.BeneficiaryTypes.Add(new BeneficiaryType()
        {
            Name = "SeedDev - Type de bénéficiaire AB", Project = project, Keys = "a;b", Beneficiaries = new List<Beneficiary>()
        });

        await db.SaveChangesAsync();
        
        subscription.Types.Add(new SubscriptionType()
        {
            ProductGroup = productGroup,
            Subscription = subscription,
            BeneficiaryType = project.BeneficiaryTypes.First(),
            Amount = 70
        });

        await db.SaveChangesAsync();
    }

    private async Task SeedDevBudgetAllowance()
    {
        var organization = await db.Organizations.FirstAsync(x => x.Name == "SeedDev - Groupe 1");
        var subscription = await db.Subscriptions.FirstAsync(x => x.Name == "SeedDev - Période 1");

        if (db.BudgetAllowances.Where(x => x.OrganizationId == organization.Id && x.SubscriptionId == subscription.Id).Count() == 1)
        {
            return;
        }

        var budgetAllowance = new BudgetAllowance()
        {
            OriginalFund = 700,
            AvailableFund = 700,
            Organization = organization,
            Subscription = subscription
        };

        db.BudgetAllowances.Add(budgetAllowance);

        await db.SaveChangesAsync();
    }

    private async Task SeedDevBeneficiaries()
    {
        if (db.Beneficiaries.Any(x => x.Firstname == "Bénéficiaire"))
        {
            return;
        }

        var organization = db.Organizations.First(x => x.Name == "SeedDev - Groupe 1");
        var subscription = db.Subscriptions.Include(x => x.Types).First(x => x.Name == "SeedDev - Période 1");
        var beneficiary1 = new Beneficiary()
        {
            Firstname = "Bénéficiaire",
            Lastname = "1",
            BeneficiaryType = db.BeneficiaryTypes.First(x => x.Name == "SeedDev - Type de bénéficiaire AB"),
            Address = "123 avenue Test",
            Email = "beneficiary1@example.com",
            Notes = "",
            Organization = organization,
            Phone = "123 555-5555",
            Subscriptions = new List<SubscriptionBeneficiary>(),
            SortOrder = 0,
            ID1 = "0001",
            PostalCode = "A0A 0A0"
        };

        var beneficiary2 = new Beneficiary()
        {
            Firstname = "Bénéficiaire",
            Lastname = "2",
            BeneficiaryType = db.BeneficiaryTypes.First(x => x.Name == "SeedDev - Type de bénéficiaire AB"),
            Address = "123 avenue Test",
            Email = "beneficiary2@example.com",
            Notes = "",
            Organization = organization,
            Phone = "123 555-5555",
            Subscriptions = new List<SubscriptionBeneficiary>(),
            SortOrder = 1,
            ID1 = "0002",
            PostalCode = "A0A 0A0"
        };

        var beneficiary3 = new Beneficiary()
        {
            Firstname = "Bénéficiaire",
            Lastname = "3",
            BeneficiaryType = db.BeneficiaryTypes.First(x => x.Name == "SeedDev - Type de bénéficiaire AB"),
            Address = "123 avenue Test",
            Email = "beneficiary3@example.com",
            Notes = "",
            Organization = organization,
            Phone = "123 555-5555",
            Subscriptions = new List<SubscriptionBeneficiary>(),
            SortOrder = 2,
            ID1 = "0003",
            PostalCode = "A0A 0A0"
        };

        var beneficiary4 = new Beneficiary()
        {
            Firstname = "Bénéficiaire",
            Lastname = "4",
            BeneficiaryType = db.BeneficiaryTypes.First(x => x.Name == "SeedDev - Type de bénéficiaire AB"),
            Address = "123 avenue Test",
            Email = "beneficiary4@example.com",
            Notes = "",
            Organization = organization,
            Phone = "123 555-5555",
            Subscriptions = new List<SubscriptionBeneficiary>(),
            SortOrder = 3,
            ID1 = "0004",
            PostalCode = "A0A 0A0"
        };

        var beneficiaryResult = await db.Beneficiaries.AddAsync(beneficiary1);
        await db.Beneficiaries.AddAsync(beneficiary2);
        await db.Beneficiaries.AddAsync(beneficiary3);
        await db.Beneficiaries.AddAsync(beneficiary4);

        beneficiary1 = beneficiaryResult.Entity;

        var paymentRemaining = subscription.GetPaymentRemaining(clock);
        var subscriptionType = subscription.Types.Where(x => x.BeneficiaryTypeId == beneficiary1.BeneficiaryTypeId).FirstOrDefault();
        var budgetAllowance = await db.BudgetAllowances.FirstAsync(x => x.OrganizationId == organization.Id && x.SubscriptionId == subscription.Id);
        budgetAllowance.AvailableFund -= paymentRemaining * subscriptionType.Amount;

        var subscriptionBeneficiary = new SubscriptionBeneficiary() { Subscription = subscription, Beneficiary = beneficiary1, BeneficiaryType = beneficiary1.BeneficiaryType, BudgetAllowance = budgetAllowance };

        beneficiary1.Subscriptions.Add(subscriptionBeneficiary);
        organization.Beneficiaries.Add(beneficiary1);

        await db.SaveChangesAsync();
    }

    private async Task SeedDevCards()
    {
        var beneficiary = db.Beneficiaries.Include(x => x.Subscriptions).ThenInclude(x => x.Subscription)
            .Include(x => x.Organization).FirstOrDefault(x => x.Lastname == "1");
        var project = db.Projects.FirstOrDefault(x => x.Name == "SeedDev - Programme 1");
        var productGroup = db.ProductGroups.FirstOrDefault(x => x.Name == "SeedDev - Groupe1");

        if (beneficiary == null || project == null || db.Cards.Any(x => x.Beneficiary == beneficiary && x.ProjectId == project.Id))
        {
            return;
        }

        var card = new Card()
        {
            Beneficiary = beneficiary, 
            Project = project, 
            Status = CardStatus.Assigned, 
            Transactions = new List<Transaction>(),
            Funds = new List<Fund>(),
            ProgramCardId = 1,
            CardNumber = "0000-0000-0000-0001"
        };
        card.Funds.Add(new Fund()
        {
            Amount = 70,
            ProductGroup = productGroup,
            Card = card
        });

        var cardResult = await db.Cards.AddAsync(card);
        card = cardResult.Entity;

        var transaction = new SubscriptionAddingFundTransaction()
        {
            TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(),
            Amount = 70,
            Beneficiary = beneficiary,
            Organization = beneficiary.Organization,
            Status = FundTransactionStatus.Actived,
            AvailableFund = 70,
            SubscriptionType = db.SubscriptionTypes.First(),
            Transactions = new List<PaymentTransaction>(),
            ExpirationDate = DateTime.UtcNow.AddMonths(3),
            CreatedAtUtc = DateTime.UtcNow.AddMonths(-1),
            ProductGroup = productGroup
        };
        card.Transactions.Add(transaction);
        
        var transactionLogProductGroups = new List<TransactionLogProductGroup>()
        {
            new()
            {
                Amount = transaction.Amount,
                ProductGroupId = transaction.ProductGroupId,
                ProductGroupName = transaction.ProductGroup.Name
            }
        };
        
        db.TransactionLogs.Add(new TransactionLog()
        {
            TransactionUniqueId = transaction.TransactionUniqueId,
            Discriminator = TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog,
            TotalAmount = transaction.Amount,
            BeneficiaryId = beneficiary.Id,
            BeneficiaryFirstname = beneficiary.Firstname,
            BeneficiaryLastname = beneficiary.Lastname,
            BeneficiaryEmail = beneficiary.Email,
            BeneficiaryPhone = beneficiary.Phone,
            BeneficiaryID1 = beneficiary.ID1,
            BeneficiaryID2 = beneficiary.ID2,
            BeneficiaryIsOffPlatform = beneficiary is OffPlatformBeneficiary,
            BeneficiaryTypeId = beneficiary.BeneficiaryTypeId,
            CardProgramCardId = card.ProgramCardId,
            CardNumber = card.CardNumber,
            ProjectId = card.ProjectId,
            ProjectName = card.Project.Name,
            CreatedAtUtc = DateTime.UtcNow.AddMonths(-1),
            SubscriptionId = beneficiary.Subscriptions.First().SubscriptionId,
            SubscriptionName = beneficiary.Subscriptions.First().Subscription.Name,
            OrganizationId = beneficiary.OrganizationId,
            OrganizationName = beneficiary.Organization.Name,
            TransactionLogProductGroups = transactionLogProductGroups
        });
        
        await db.SaveChangesAsync();
    }

    private async Task SeedDevTransactions()
    {
        var beneficiary = db.Beneficiaries.Include(x => x.Subscriptions).ThenInclude(x => x.Subscription)
            .Include(x => x.Organization).FirstOrDefault(x => x.Firstname == "Bénéficiaire");
        var project = db.Projects.FirstOrDefault(x => x.Name == "SeedDev - Programme 1");
        var market = db.Markets.FirstOrDefault(x => x.Name == "SeedDev - Commerce 1");
        var productGroup = db.ProductGroups.FirstOrDefault(x => x.Name == "SeedDev - Groupe1");

        if (beneficiary == null || project == null || market == null)
        {
            return;
        }

        var card = db.Cards.Include(x => x.Transactions).FirstOrDefault(x => x.Beneficiary == beneficiary && x.Project == project);

        if (card == null)
        {
            return;
        }

        var addingFundsTransaction = card.Transactions.OfType<SubscriptionAddingFundTransaction>().FirstOrDefault();

        if (db.Transactions.OfType<PaymentTransaction>().Any(x => x.Card == card))
        {
            return;
        }

        var transaction1 = new PaymentTransaction() { TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(), Amount = 25.75m, CreatedAtUtc = DateTime.UtcNow.AddMonths(-1), Card = card, Beneficiary = beneficiary, Organization = beneficiary.Organization, Market = market, Transactions = new List<AddingFundTransaction>() };
        transaction1.TransactionByProductGroups = new List<PaymentTransactionProductGroup>() { new PaymentTransactionProductGroup() { Amount = 25.75m, ProductGroup = productGroup, PaymentTransaction = transaction1 } };
        addingFundsTransaction.AvailableFund -= transaction1.Amount;
        var fund = card.Funds.FirstOrDefault(x => x.ProductGroupId == productGroup.Id);
        fund.Amount -= transaction1.Amount;
        transaction1.Transactions.Add(addingFundsTransaction);
        card.Transactions.Add(transaction1);
        await db.Transactions.AddAsync(transaction1);

        var transactionLogProductGroups = new List<TransactionLogProductGroup>();
        foreach (var transactionProductGroup in transaction1.TransactionByProductGroups)
        {
            transactionLogProductGroups.Add(new TransactionLogProductGroup()
            {
                Amount = transactionProductGroup.Amount,
                ProductGroupId = transactionProductGroup.ProductGroupId,
                ProductGroupName = transactionProductGroup.ProductGroup.Name
            });
        }
        
        await db.TransactionLogs.AddAsync(new TransactionLog()
        {
            TransactionUniqueId = transaction1.TransactionUniqueId,
            Discriminator = TransactionLogDiscriminator.PaymentTransactionLog,
            TotalAmount = transaction1.Amount,
            BeneficiaryId = beneficiary.Id,
            BeneficiaryFirstname = beneficiary.Firstname,
            BeneficiaryLastname = beneficiary.Lastname,
            BeneficiaryEmail = beneficiary.Email,
            BeneficiaryPhone = beneficiary.Phone,
            BeneficiaryID1 = beneficiary.ID1,
            BeneficiaryID2 = beneficiary.ID2,
            BeneficiaryIsOffPlatform = beneficiary is OffPlatformBeneficiary,
            BeneficiaryTypeId = beneficiary.BeneficiaryTypeId,
            CardProgramCardId = card.ProgramCardId,
            CardNumber = card.CardNumber,
            ProjectId = card.ProjectId,
            ProjectName = card.Project.Name,
            CreatedAtUtc = DateTime.UtcNow.AddMonths(-1),
            SubscriptionId = beneficiary.Subscriptions.First().SubscriptionId,
            SubscriptionName = beneficiary.Subscriptions.First().Subscription.Name,
            OrganizationId = beneficiary.OrganizationId,
            OrganizationName = beneficiary.Organization.Name,
            MarketId = market.Id,
            MarketName = market.Name,
            TransactionLogProductGroups = transactionLogProductGroups
        });
        
        var transaction2 = new PaymentTransaction() { TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(), Amount = 32.33m, CreatedAtUtc = DateTime.UtcNow, Card = card, Beneficiary = beneficiary, Organization = beneficiary.Organization, Market = market, Transactions = new List<AddingFundTransaction>() };
        transaction2.TransactionByProductGroups = new List<PaymentTransactionProductGroup>() { new PaymentTransactionProductGroup() { Amount = 32.33m, ProductGroup = productGroup, PaymentTransaction = transaction1 } };
        addingFundsTransaction.AvailableFund -= transaction2.Amount;
        fund = card.Funds.FirstOrDefault(x => x.ProductGroupId == productGroup.Id);
        fund.Amount -= transaction2.Amount;
        transaction2.Transactions.Add(addingFundsTransaction);
        card.Transactions.Add(transaction2);
        await db.Transactions.AddAsync(transaction2);
        
        transactionLogProductGroups = new List<TransactionLogProductGroup>();
        foreach (var transactionProductGroup in transaction2.TransactionByProductGroups)
        {
            transactionLogProductGroups.Add(new TransactionLogProductGroup()
            {
                Amount = transactionProductGroup.Amount,
                ProductGroupId = transactionProductGroup.ProductGroupId,
                ProductGroupName = transactionProductGroup.ProductGroup.Name
            });
        }
        
        await db.TransactionLogs.AddAsync(new TransactionLog()
        {
            TransactionUniqueId = transaction2.TransactionUniqueId,
            Discriminator = TransactionLogDiscriminator.PaymentTransactionLog,
            TotalAmount = transaction2.Amount,
            BeneficiaryId = beneficiary.Id,
            BeneficiaryFirstname = beneficiary.Firstname,
            BeneficiaryLastname = beneficiary.Lastname,
            BeneficiaryEmail = beneficiary.Email,
            BeneficiaryPhone = beneficiary.Phone,
            BeneficiaryID1 = beneficiary.ID1,
            BeneficiaryID2 = beneficiary.ID2,
            BeneficiaryIsOffPlatform = beneficiary is OffPlatformBeneficiary,
            BeneficiaryTypeId = beneficiary.BeneficiaryTypeId,
            CardProgramCardId = card.ProgramCardId,
            CardNumber = card.CardNumber,
            ProjectId = card.ProjectId,
            ProjectName = card.Project.Name,
            CreatedAtUtc = DateTime.UtcNow.AddMonths(-1),
            SubscriptionId = beneficiary.Subscriptions.First().SubscriptionId,
            SubscriptionName = beneficiary.Subscriptions.First().Subscription.Name,
            OrganizationId = beneficiary.OrganizationId,
            OrganizationName = beneficiary.Organization.Name,
            MarketId = market.Id,
            MarketName = market.Name,
            TransactionLogProductGroups = transactionLogProductGroups
        });

        await db.SaveChangesAsync();
    }
    
    private async Task<AppUser> GetUserByEmailAndType(string email, UserType type)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);

        if (user != null)
        {
            if (user.Type == type)
            {
                return user;
            }
        }

        return null;
    }
}