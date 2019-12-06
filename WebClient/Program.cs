using Common;
using Grains;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;
using System;
using System.Linq;
using System.Net;

namespace WebClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            CreateDbIfNotExists(host);
            host.Run();
        }

        public static void CreateDbIfNotExists(IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                try
                {
                    var context = services.GetRequiredService<GameContext>();
                    DBInitializer.Initialize(context);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred creating the DB.");
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                            .AddEnvironmentVariables("APPSETTING_");
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseOrleans((context, siloBuilder) =>
                {
                    IConfiguration Configuration = context.Configuration;
                    var invariant = Configuration["Invariant"];
                    var sqlConnectionString = Configuration.GetConnectionString("DefaultConnection");

                    siloBuilder.Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = Constants.ClusterId;
                        options.ServiceId = Constants.ServiceId;
                    })
                    .UseLocalhostClustering()
                    .AddAdoNetGrainStorage(Constants.OrleansDataStorageProvider, options =>
                    {
                        options.Invariant = invariant;
                        options.ConnectionString = sqlConnectionString;
                        options.UseJsonFormat = true;
                    })
                    .UseAdoNetReminderService(options =>
                    {
                        options.Invariant = invariant;
                        options.ConnectionString = sqlConnectionString;
                    })
                    .UseInMemoryReminderService()
                    // need to configure a grain storage called "PubSubStore" for using
                    // streaming with ExplicitSubscribe pubsub type. Depends on your
                    // application requirements, you can configure your silo with other
                    // stream providers, which can provide other features, such as
                    // persistence or recoverability. For more information, please see
                    // http://dotnet.github.io/orleans/Documentation/streaming/streams_programming_APIs.html#fully-managed-and-reliable-streaming-pub-suba-namefully-managed-and-reliable-streaming-pub-suba
                    .AddMemoryGrainStorage(Constants.OrleansMemoryProvider)
                    .AddSimpleMessageStreamProvider(Constants.OrleansStreamProvider)
                    .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(GameGrain).Assembly).WithReferences());
                })
                .UseSerilog((context, config) =>
                {
                    config.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext();
                });
    }
}
