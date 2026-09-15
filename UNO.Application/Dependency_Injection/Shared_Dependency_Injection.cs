using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using UNO.Application.Interfaces.Common;
using UNO.Application.Services.Common;
namespace UNO.Application.Dependency_Injection
{
    public static class Shared_Dependency_Injection
    {
        public static IServiceCollection AddSharedService (this IServiceCollection services)
        {
            services.AddScoped<IAccountService, AccountService>();
            return services;
        }
    }
}
