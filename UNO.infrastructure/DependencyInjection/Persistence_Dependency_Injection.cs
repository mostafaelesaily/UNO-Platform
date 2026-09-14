using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using UNO.infrastructure.Data;

namespace UNO.infrastructure.DependencyInjection
{
    public static class Persistence_Dependency_Injection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services ,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("UNO_DB");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DB_ConnectionString' not found in configuration or environment variables.");
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            return services;
        }
    }
}
