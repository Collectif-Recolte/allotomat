using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;

namespace Sig.App.Backend.DataSeeders;

public class ProdDataSeeder : IDataSeeder
{
    private readonly AppDbContext db;
    private readonly UserManager<AppUser> userManager;
    private readonly IConfiguration configuration;
    private readonly ILogger<ProdDataSeeder> logger;

    public ProdDataSeeder(
        AppDbContext db, 
        UserManager<AppUser> userManager, 
        IConfiguration configuration, 
        ILogger<ProdDataSeeder> logger)
    {
        this.db = db;
        this.userManager = userManager;
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task Seed()
    {
        logger.LogInformation("Seeding prod data");
        
        await SeedDefaultAdmin();
    }

    private async Task SeedDefaultAdmin()
    {
        var email = configuration["defaultAdmin:email"];
        var password = configuration["defaultAdmin:password"];
        var firstName = configuration["defaultAdmin:firstName"];
        var lastName = configuration["defaultAdmin:lastName"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        await userManager.CreateOrUpdateAsync(db, email, firstName, lastName, UserType.PCAAdmin, password, allowUpdatePassword: false);
    }
}