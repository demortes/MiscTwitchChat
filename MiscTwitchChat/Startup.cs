using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using MiscTwitchChat.Health;
using MiscTwitchChat.Helpers;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using IPNetwork = System.Net.IPNetwork;

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

        /// <summary>
        /// MySQL server version assumed when "Database:ServerVersion" is not configured. Currently the MySQL LTS release.
        /// </summary>
        private static readonly Version DefaultServerVersion = new(8, 4, 0);

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
            var serverVersion = ResolveServerVersion(Configuration);
            services.AddDbContext<MiscTwitchDbContext>(o =>
                o.UseMySql(connectionString, serverVersion));

            // The API is only useful when it can reach MySQL, so readiness hangs off the database.
            services.AddHealthChecks()
                .AddDbContextConnectionCheck<MiscTwitchDbContext>("mysql", HealthEndpoint.ReadyTag);

            RegisterCardsAgainstHumanity(services);
            RegisterStJudeFacts(services);

            services.AddScoped<ActionFilter>();
            services.AddMvc(config =>
                config.Filters.Add<ActionFilter>());
            services.AddSingleton(Configuration);

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Demortes' Random Chat Bot APIs",
                    Version = "v1",
                    Description = "A collection of APIs for Demortes' Twitch chat bot.",
                    Contact = new OpenApiContact
                    {
                        Name = "Demortes",
                        Email = "webmaster@demortes.com",
                        Url = new Uri("https://demortes.com")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });
            });

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                // Clear defaults (loopback only) and trust Cloudflare's published IP ranges.
                // https://www.cloudflare.com/ips/
                options.KnownNetworks.Clear();
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();

                // IPv4
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("173.245.48.0"),  20));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("103.21.244.0"),  22));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("103.22.200.0"),  22));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("103.31.4.0"),    22));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("141.101.64.0"),  18));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("108.162.192.0"), 18));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("190.93.240.0"),  20));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("188.114.96.0"),  20));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("197.234.240.0"), 22));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("198.41.128.0"),  17));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("162.158.0.0"),   15));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("104.16.0.0"),    13));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("104.24.0.0"),    14));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("172.64.0.0"),    13));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("131.0.72.0"),    22));

                // IPv6
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("2400:cb00::"),   32));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("2606:4700::"),   32));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("2803:f800::"),   32));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("2405:b500::"),   32));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("2405:8100::"),   32));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("2a06:98c0::"),   29));
                options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("2c0f:f248::"),   32));
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
            // Must be first so RemoteIpAddress is resolved before any other middleware reads it.
            app.UseForwardedHeaders();

            // Ahead of UseHttpsRedirection: the container's health check probes over plain HTTP and a
            // redirect would look like success to curl.
            app.UseHealthEndpoints();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Demortes' Random Chat Bot APIs");
                c.InjectStylesheet("/css/custom.css");
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseRouting();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        /// <summary>
        /// Resolves the MySQL server version the EF Core provider targets for feature detection.
        /// </summary>
        /// <remarks>
        /// The version is pinned from the "Database:ServerVersion" configuration value (for example "8.4") rather than
        /// probed with <see cref="ServerVersion.AutoDetect(string)"/>. AutoDetect opens a throwaway connection to read
        /// the version; inside the <c>AddDbContext</c> options lambda that ran once per scope, costing an extra
        /// connection and round trip on every request. Pinning also keeps the application bootable when MySQL is
        /// unreachable, so the endpoints that do not touch the database keep serving.
        /// </remarks>
        /// <param name="configuration">Configuration to read the pinned server version from.</param>
        /// <returns>The configured server version, or <see cref="DefaultServerVersion"/> when none is configured.</returns>
        /// <exception cref="InvalidOperationException">The configured value is not a parsable version.</exception>
        private static ServerVersion ResolveServerVersion(IConfiguration configuration)
        {
            var configured = configuration["Database:ServerVersion"];
            if (string.IsNullOrWhiteSpace(configured))
            {
                return new MySqlServerVersion(DefaultServerVersion);
            }

            if (!Version.TryParse(configured, out var parsed))
            {
                throw new InvalidOperationException(
                    $"Configuration value 'Database:ServerVersion' ('{configured}') is not a valid version. Use a value such as '8.4'.");
            }

            return new MySqlServerVersion(Normalize(parsed));
        }

        /// <summary>
        /// Expands a version to three components so provider feature gates compare as expected.
        /// </summary>
        /// <remarks>
        /// <see cref="Version"/> leaves omitted components at -1, and -1 sorts below 0. That makes
        /// <c>new Version(8, 4)</c> compare as *less than* <c>new Version(8, 4, 0)</c>, so a two-component
        /// value silently fails any provider feature gated at exactly that release.
        /// </remarks>
        private static Version Normalize(Version version) =>
            version.Build >= 0 ? version : new Version(version.Major, version.Minor, 0);

        /// <summary>
        /// Loads "cah_cards.json", deserializes it into a CAH_cards instance, and registers that instance as a singleton in the provided service collection.
        /// </summary>
        /// <param name="services">The service collection to which the deserialized CAH_cards instance will be added as a singleton.</param>
        private static void RegisterCardsAgainstHumanity(IServiceCollection services)
        {
            try
            {
                using StreamReader file = File.OpenText("cah_cards.json");
                var serializer = new JsonSerializer();
                var cards = (CAH_cards)serializer.Deserialize(file, typeof(CAH_cards));
                
                if (cards == null)
                {
                    throw new InvalidOperationException("Failed to deserialize CAH cards from cah_cards.json");
                }
                
                services.AddSingleton(cards);
            }
            catch (Exception ex) when (ex is FileNotFoundException or IOException or JsonException)
            {
                throw new InvalidOperationException("Failed to load CAH cards data. Ensure cah_cards.json exists and is valid JSON.", ex);
            }
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
