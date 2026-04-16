using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiscTwitchChat.Helpers;
using Newtonsoft.Json;
using System;
using System.IO;

namespace MiscTwitchChat
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_Environment")}.json", true, true)
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>();
            Configuration = builder.Build();
        }

        private IConfiguration Configuration { get; }

        /// <summary>
        /// Configures and registers application services, middleware options, database context, Swagger/OpenAPI, and other framework integrations into the dependency injection container.
        /// </summary>
        /// <remarks>
        /// Configurations performed include cookie policy options, Entity Framework MySQL DbContext registration, singleton registrations for card and fact data, MVC with action filters, registering the application's <see cref="IConfiguration"/>, Swagger/OpenAPI document metadata, forwarded header handling, and MVC options (endpoint routing disabled).
        /// </remarks>
        /// <param name="services">The service collection to populate with application services and configuration.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.AddEntityFrameworkMySql();
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<MiscTwitchDbContext>(o =>
                o.UseMySql(Configuration.GetConnectionString("DefaultConnection"), serverVersion: ServerVersion.AutoDetect(connectionString)));

            RegisterCardsAgainstHumanity(services);
            RegisterStJudeFacts(services);

            services.AddMvc(config =>
                config.Filters.Add(new ActionFilter(new LoggerFactory())));
            services.AddSingleton(Configuration);

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Demortes' Random Chat Bot APIs",
                    Version = "v1",
                    Description = "A collection of APIs for Demortes' Twitch chat bot.",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "Demortes",
                        Email = "webmaster@demortes.com",
                        Url = new Uri("https://demortes.com")
                    },
                    License = new Microsoft.OpenApi.Models.OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });
            });

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
            services.Configure<MvcOptions>(options =>
            {
                options.EnableEndpointRouting = false;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// <summary>
        /// Configures the application's HTTP request pipeline and middleware.
        /// </summary>
        /// <remarks>
        /// Sets up environment-specific error handling and HSTS, serves Swagger and Swagger UI (with a custom stylesheet), enables HTTPS redirection, static files, cookie policy, forwarded header processing, and MVC routing.
        /// </remarks>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Demortes' Random Chat Bot APIs");
                c.InjectStylesheet("/css/custom.css");
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
            app.UseRouting();
            app.UseForwardedHeaders();

        }

        /// <summary>
        /// Loads "cah_cards.json", deserializes it into a CAH_cards instance, and registers that instance as a singleton in the provided service collection.
        /// </summary>
        /// <param name="services">The service collection to which the deserialized CAH_cards instance will be added as a singleton.</param>
        private static void RegisterCardsAgainstHumanity(IServiceCollection services)
        {
            using StreamReader file = File.OpenText("cah_cards.json");
            var serializer = new JsonSerializer();
            var cards = (CAH_cards)serializer.Deserialize(file, typeof(CAH_cards));
            services.AddSingleton(cards);
        }

        /// <summary>
        /// Loads St. Jude facts from the file "stjudefacts.json" and registers the resulting StJude object as a singleton service.
        /// </summary>
        private static void RegisterStJudeFacts(IServiceCollection services)
        {
            using StreamReader file = File.OpenText("stjudefacts.json");
            var serializer = new JsonSerializer();
            var facts = (StJude)serializer.Deserialize(file, typeof(StJude));
            services.AddSingleton(facts);
        }
    }
}
