﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using ZeroStack.DeviceCenter.Domain.Aggregates.ProductAggregate;
using ZeroStack.DeviceCenter.Domain.Repositories;
using ZeroStack.DeviceCenter.Infrastructure.Constants;
using ZeroStack.DeviceCenter.Infrastructure.EntityFrameworks;
using ZeroStack.DeviceCenter.Infrastructure.Repositories;

namespace ZeroStack.DeviceCenter.Infrastructure
{
    public static class DependencyRegistrar
    {
        public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddEntityFrameworkSqlServer();

            services.AddDbContextPool<DeviceCenterDbContext>((serviceProvider, optionsBuilder) =>
            {
                optionsBuilder.UseSqlServer(configuration.GetConnectionString(DbConstants.DefaultConnectionStringName), sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                });
                optionsBuilder.UseInternalServiceProvider(serviceProvider);
            });

            services.AddPooledDbContextFactory<DeviceCenterDbContext>((serviceProvider, optionsBuilder) =>
            {
                optionsBuilder.UseSqlServer(configuration.GetConnectionString(DbConstants.DefaultConnectionStringName), sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                });

                optionsBuilder.UseInternalServiceProvider(serviceProvider);
            });

            services.AddTransient(typeof(IRepository<>), typeof(DeviceCenterEfCoreRepository<>));
            services.AddTransient(typeof(IRepository<,>), typeof(DeviceCenterEfCoreRepository<,>));

            services.AddTransient<IProductRepository, ProductRepository>();

            return services;
        }
    }
}
