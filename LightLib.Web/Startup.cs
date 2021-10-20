using LightLib.Data;
using LightLib.Service.Assets;
using LightLib.Service.Branches;
using LightLib.Service.Checkout;
using LightLib.Service.Interfaces;
using LightLib.Service.Patrons;
using LightLib.Service.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace LightLib.Web {
    public class Startup {
        public Startup(IConfiguration config) {
            Configuration = config;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services) {
            services.AddMvc();

            services.AddSingleton(Configuration);

            var hostIp = Environment.GetEnvironmentVariable("hostname");
            if (string.IsNullOrWhiteSpace(hostIp)) { throw new Exception("Database host ip is not set in Environment variable"); }

            var port = Configuration.GetValue<string>("port");
            var userName = Configuration.GetValue<string>("username");
            var password = Configuration.GetValue<string>("password");
            var dbName = Configuration.GetValue<string>("databasename");

            var dbConnStr = $"Host={hostIp};Port={port};Username={userName};Password={password};Database={dbName}";

            //services.AddDbContext<LibraryDbContext>(options => options.UseNpgsql(dbConnStr));

            services.AddDbContext<LibraryDbContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("LibraryConnection")));

            services.AddAutoMapper(c => c.AddProfile<EntityMappingProfile>(), typeof(Startup));
            
            services.AddScoped<ICheckoutService, CheckoutService>();
            services.AddScoped<IHoldService, HoldService>();
            services.AddScoped<ILibraryAssetService, LibraryAssetService>();
            services.AddScoped<ILibraryBranchService, LibraryBranchService>();
            services.AddScoped<ILibraryCardService, LibraryCardService>();
            services.AddScoped<IPatronService, PatronService>();
            services.AddScoped<IStatusService, StatusService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseRouting();
            app.UseAuthentication();

            app.UseEndpoints(routes => {
                routes.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}