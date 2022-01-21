using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiscTwitchChat.Helpers;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Extensions.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Prometheus;

namespace MiscTwitchChat
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
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
            services.AddApplicationInsightsTelemetry(
                Configuration.GetValue<string>("ApplicationInsights:InstrumentationKey"));

            //Load CAH Cards JSON and add to singleton.
            using(StreamReader file = File.OpenText("cah_cards.json"))
            {
                var serializer = new JsonSerializer();
                var cards = (CAH_cards)serializer.Deserialize(file, typeof(CAH_cards));
                services.AddSingleton(cards);
            }

            using(StreamReader file = File.OpenText("stjudefacts.json"))
            {
                var serializer = new JsonSerializer();
                var facts = (StJude)serializer.Deserialize(file, typeof(StJude));
                services.AddSingleton(facts);
            }

            services.AddMvc(config =>
                config.Filters.Add(new ActionFilter(new LoggerFactory())));
            services.AddSingleton(Configuration);

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Demortes' Random Chat Bot APIs", Version = "v1" });
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
        // ReSharper disable once UnusedMember.Global
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMetricServer();
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
            app.UseHttpMetrics();
            app.UseForwardedHeaders();

        }
    }
}
