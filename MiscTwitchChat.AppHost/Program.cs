var builder = DistributedApplication.CreateBuilder(args);

var mysql = builder.AddMySql("mysql")
                   .WithLifetime(ContainerLifetime.Persistent).WithDataVolume().WithPhpMyAdmin();

var mysqldb = mysql.AddDatabase("DefaultConnection");

builder.AddProject<Projects.DiscordBot>("discordbot").WithReference(mysqldb).WaitFor(mysqldb);

builder.AddProject<Projects.MiscTwitchChat>("misctwitchchat").WithReference(mysqldb).WaitFor(mysqldb);

builder.AddProject<Projects.TwitchActivityBot>("twitchactivitybot").WithReference(mysqldb).WaitFor(mysqldb);

builder.AddProject<Projects.MiscTwitchChat_MigrationService>("migrationservice").WithReference(mysqldb).WaitFor(mysqldb);

builder.Build().Run();
