using Sig.App.Backend.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Claims;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql
{
    public class AppUserContext : IAppUserContext
    {
        private readonly IServiceProvider services;
        
        private TService Get<TService>() => services.GetRequiredService<TService>();

        public ClaimsPrincipal CurrentUser => Get<IHttpContextAccessor>().HttpContext?.User;
        public string CurrentUserId => CurrentUser.GetUserId();
        
        public DataLoader DataLoader { get; }

        public AppUserContext(IServiceProvider services, DataLoader loader)

        {
            this.services = services;
            DataLoader = loader;
        }
    }
}