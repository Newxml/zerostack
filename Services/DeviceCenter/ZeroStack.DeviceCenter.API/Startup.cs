using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using ZeroStack.DeviceCenter.API.Constants;
using ZeroStack.DeviceCenter.Application;
using ZeroStack.DeviceCenter.Domain;
using ZeroStack.DeviceCenter.Domain.Repositories;
using ZeroStack.DeviceCenter.Infrastructure;

namespace ZeroStack.DeviceCenter.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDomainLayer().AddInfrastructureLayer(Configuration).AddApplicationLayer();

            services.AddTenantMiddleware();

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ZeroStack.DeviceCenter.API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseTenantMiddleware();

            using (IServiceScope serviceScope = app.ApplicationServices.CreateScope())
            {
                var dataSeedProviders = serviceScope.ServiceProvider.GetServices<IDataSeedProvider>();

                foreach (IDataSeedProvider dataSeedProvider in dataSeedProviders)
                {
                    dataSeedProvider.SeedAsync(serviceScope.ServiceProvider).Wait();
                }
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ZeroStack.DeviceCenter.API v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
