using Sig.App.Backend.Extensions;
using GraphQL.Conventions;
using Hangfire;
using Hangfire.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using StackifyLib;
using System;
using System.Net;
using System.Net.Mail;
using System.Text.Json.Serialization;
using GraphQL.Server.Ui.GraphiQL;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Hosting;
using Sig.App.Backend.Authorization;
using Sig.App.Backend.Authorization.Requirements;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DataSeeders;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.Hangfire;
using Sig.App.Backend.Plugins.Identity;
using Sig.App.Backend.Plugins.ImageSharp;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Services.Crypto;
using Sig.App.Backend.Services.Files;
using Sig.App.Backend.Services.Mailer;
using Sig.App.Backend.Services.Permission;
using Sig.App.Backend.Services.Razor;
using Sig.App.Backend.Services.System;
using SixLabors.ImageSharp.Web.DependencyInjection;
using Sig.App.Backend.Services.QRCode;
using Sig.App.Backend.BackgroundJobs;
using Sig.App.Backend.Services.Beneficiaries;
using Sig.App.Backend.Services.HtmlToPdf;
using Sig.App.Backend.Services.Reports;
using Sig.App.Backend.Services.Cards;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Http;
using Sig.App.Backend.Plugins.BudgetAllowances;

namespace Sig.App.Backend
{
    public class Startup
    {
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration configuration;

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            this.environment = environment;
            this.configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // Telemetry
            services.AddApplicationInsightsTelemetry();
            
            // Entity Framework
            services.AddDbContextPool<AppDbContext>(ConfigureDbContext);

            // MVC
            services.AddMvc()
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            
            // SPA Services

            services.AddSpaStaticFiles(options =>
            {
                options.RootPath = "ClientApp";
            });

            // Identity, Authentication, Authorization

            services.Configure<IdentityOptions>(configuration.GetSection("identity"));
            services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
            {
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.Parse(configuration["cookie:ExpireTimeSpan"]);
            });
            services.Configure<SecurityStampValidatorOptions>(x =>
            {
                x.ValidationInterval = TimeSpan.Parse(configuration["cookie:SecurityStampValidationInterval"]);
            });

            services.AddDataProtection()
                .PersistKeysToDbContext<AppDbContext>();

            services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, AppUserClaimsPrincipalFactory>();


            services
                .AddIdentity<AppUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders()
                .AddTokenProvider<LongLivedTokenProvider>(TokenProviders.EmailInvites);

            services
                .AddAuthentication(ConfigureAuthentication)
                // JWT Bearer est ajouté pour nous permettre, dans ConfigureAuthentication, d'utiliser un
                // DefaultChallengeScheme qui ne génère pas de redirection vers un écran de login. Cette redirection
                // est plutôt gérée au niveau du frontend. Les appels non-authentifiés vont plutôt lancer une erreur
                // 401 - Unauthorized.
                .AddJwtBearer();

            services.AddAuthorization(ConfigureAuthorization);

            AddAuthorizationHandlers(services);

            // QRCode

            services.Configure<QRCodeOptions>(configuration.GetSection("qrcode"));
            services.AddSingleton<IQRCodeService, QRCodeService>();
            services.AddSingleton<ICardService, CardService>();

            // GraphQL

            // Ça cause un warning lors du build, mais on doit quand même créer l'instance du authorization policy
            // provider à ce stade pour que la création de l'engin GraphQL fonctionne 
#pragma warning disable ASP0000
            using (var serviceProvider = services.BuildServiceProvider())
            {
                AnnotatePolicyAttribute.AuthorizationPolicyProvider =
                    serviceProvider.GetService<IAuthorizationPolicyProvider>();
            }
#pragma warning restore ASP0000

            services.AddScoped<IAppUserContext, AppUserContext>();
            services.AddScoped<IUserContext>(s => s.GetRequiredService<IAppUserContext>());
            services.AddScoped<IDependencyInjector, DependencyInjector>();
            services.AddScoped<ScopedDependencyInjectorFactory>();
            services.AddScoped<DataLoader>();
            services.AddScoped<BudgetAllowanceLogFactory>();
            services.AddSingleton(GraphQLEngineFactory.Create());

            // MediatR

            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(typeof(Program).Assembly);
            });
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ConcurrencyHandlingBehavior<,>));

            // FluentEmail

            services
                .AddFluentEmail(configuration["Mailer:FromEmail"], configuration["Mailer:FromName"])
                .AddSmtpSender(CreateSmtpClient);
            
            // ImageSharp

            services.AddImageSharp()
                .ClearProviders().AddProvider<AppImageProvider>()
                .SetCache<AppImageCache>();
            
            // Crypto
            
            services.Configure<HmacSignatureOptions>(configuration.GetSection("hmac"));
            services.AddSingleton<ISignatureService, HmacSignatureService>();
            
            // File system
            
            services.AddFileManager(configuration.GetSection("files"));
            services.AddScoped<ImageUrlProvider>();
            services.AddScoped<FileUrlProvider>();

            // Hangfire

            services.AddHangfire(x => x.UseSqlServerStorage(configuration.GetConnectionString("AppDbContext")));
            services.AddHangfireServer();

            // Report

            services.AddScoped<IReportService, ReportService>();

            // Permissions

            services.AddTransient<PermissionService>();

            // Data seeder

            if (environment.IsProduction())
                services.AddTransient<IDataSeeder, ProdDataSeeder>();
            else
                services.AddTransient<IDataSeeder, DevDataSeeder>();

            // App services

            services.AddHttpClient();
            services.AddLazyCache();

            services.AddSingleton<IClock>(NodaTime.SystemClock.Instance);

            services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
            
            services.AddScoped<IBeneficiaryService, BeneficiaryService>();

            services.AddScoped<IRazorRenderer, RazorRenderer>();
            AddDecoratedMailer(services);

            services.AddHttpClient<Api2PdfClient>(client => {
                client.BaseAddress = new Uri(configuration["Api2Pdf:BaseAddress"]);
                client.DefaultRequestHeaders.Add("Authorization", configuration["Api2Pdf:ApiKey"]);
            });
            services.AddScoped<IHtmlToPdfConverter, Api2PdfConverter>();

            services.AddSingleton<EmailBlacklistCache>();
            services.AddScoped<IEmailBlacklistService, EmailBlacklistService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IDataSeeder dataSeeder, AppDbContext db, ILoggerFactory loggerFactory)
        {
            if (configuration.GetValue<bool>("Stackify:Enabled"))
            {
                app.ConfigureStackifyLogging((IConfigurationRoot)configuration);
            }

            db.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));
            db.Database.Migrate();

            dataSeeder.Seed().GetAwaiter().GetResult();

            if (environment.IsDevelopment() || environment.IsEndToEnd())
            {
                app.UseWhen(
                    ctx => ctx.Request.Path == "/graphql" && ctx.Request.Method == "GET",
                    b => b.UseMiddleware<GraphiQLMiddleware>(new GraphiQLOptions()));
            }

            if (environment.IsProduction())
            {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseAuthentication();
            app.UseImageSharp();

            DashboardOptions hangfireDashboardOptions;
            if (environment.IsProduction())
            {
                hangfireDashboardOptions = new DashboardOptions {
                    Authorization = new[] {
                        new AnyAuthorizationFilter(
                            new LocalRequestsOnlyAuthorizationFilter(),
                            new IpWhitelistAuthorizationFilter(
                                configuration["hangfire:ipWhitelist"],
                                loggerFactory.CreateLogger<IpWhitelistAuthorizationFilter>()
                            )
                        )
                    }
                };
            }
            else
            {
                hangfireDashboardOptions = new DashboardOptions
                {
                    Authorization = new[] {
                        new AnyAuthorizationFilter(
                            new AllowEveryoneAuthorizationFilter()
                        )
                    }
                };
            }
            
            app.UseHangfireDashboard(options: hangfireDashboardOptions);  

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(builder =>
            {
                builder.MapControllers();
            });

            app.UseSpaStaticFiles();
            app.UseSpa(spa =>
            {
                if (environment.IsDevelopment())
                {
                    spa.UseProxyToSpaDevelopmentServer(configuration["FrontendDevServer"]);
                }
            });

            AddingFundToCard.RegisterJob(configuration);
            SendMonthlyBalanceReport.RegisterJob(configuration);
            ExpireFundsFromCard.RegisterJob(configuration);
            DeactivateOffPlatformBeneficiary.RegisterJob(configuration);
            RefreshCardBalance.RegisterJob(configuration);
            DeleteUser.RegisterJob(configuration);
            DeleteBeneficiary.RegisterJob(configuration);
            ExpireSubscription.RegisterJob(configuration);
            SendMonthlyCardBalanceReport.RegisterJob(configuration);
        }

        private void ConfigureDbContext(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(
                configuration.GetConnectionString("AppDbContext"),
                sqlOptions => { sqlOptions.EnableRetryOnFailure(); });
        }

        private void ConfigureAuthentication(AuthenticationOptions options)
        {
            options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }

        private void ConfigureAuthorization(AuthorizationOptions options)
        {
            options.AddPolicy(AuthorizationPolicies.LoggedIn, policy => { policy.RequireAuthenticatedUser(); });
            options.AddPolicy(AuthorizationPolicies.IsPCAAdmin, policy => { policy.RequireClaim(AppClaimTypes.UserType, UserType.PCAAdmin.ToString()); });
            options.AddPolicy(AuthorizationPolicies.IsProjectManager, policy => { policy.RequireClaim(AppClaimTypes.UserType, UserType.ProjectManager.ToString()); });

            options.AddPolicy(AuthorizationPolicies.ManageUser, policy => { policy.AddRequirements(new CanManageUserRequirement()); });
        }

        private void AddAuthorizationHandlers(IServiceCollection services)
        {
            services.Scan(scan =>
            {
                scan.FromCallingAssembly()
                    .AddClasses(classes => classes.AssignableTo<IAuthorizationHandler>())
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime();
            });
        }

        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient(
                configuration.GetValue<string>("Mailer:SmtpHost"),
                configuration.GetValue<int>("Mailer:SmtpPort"))
            {
                Credentials = new NetworkCredential(
                    configuration.GetValue<string>("Mailer:SmtpUsername"),
                    configuration.GetValue<string>("Mailer:SmtpPassword")),
                EnableSsl = configuration.GetValue<bool>("Mailer:SmtpSsl")
            };
        }

        private static void AddDecoratedMailer(IServiceCollection services)
        {
            services.AddScoped<FluentMailer>();

            services.AddScoped(x => new RetryingMailer(
                 x.GetService<FluentMailer>(),
                 x.GetService<ILogger<RetryingMailer>>())
             );

            services.AddScoped<IMailer>(x => new BlacklistCheckingMailer(
                x.GetService<RetryingMailer>(),
                x.GetService<IEmailBlacklistService>(),
                x.GetService<ILogger<BlacklistCheckingMailer>>())
            );
        }
    }
}
