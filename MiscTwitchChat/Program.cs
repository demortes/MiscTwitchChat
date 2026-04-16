using Microsoft.AspNetCore.Builder;
using MiscTwitchChat;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, app.Environment);

app.Run();
