using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SiloHost
{
    public class Program
    {
        public static IConfiguration Configuration { get; set; }
        public static Task Main(string[] args)
        {
            var environmentName = GetEnvironment();
            var configurationBuilder = CreateConfigurationBuilder(environmentName);
            Configuration = configurationBuilder.Build();

            var host = StartSilo();
            return host.RunConsoleAsync();
        }
        private static IHostBuilder StartSilo()
        {
            var invariant = Configuration["Invariant"];
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var name = Dns.GetHostName(); // get container id
            var host = new HostBuilder()
                .UseOrleans((context, siloBuilder) =>
                {
                    siloBuilder.Configure<ProcessExitHandlingOptions>(options =>
                    {
                        // https://github.com/dotnet/orleans/issues/5552#issuecomment-486938815
                        options.FastKillOnProcessExit = false;
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = Constants.ClusterId;
                        options.ServiceId = Constants.ServiceId;
                    })
                    .UseAdoNetClustering(options =>
                    {
                        options.Invariant = invariant;
                        options.ConnectionString = connectionString;
                    })
                    .AddAdoNetGrainStorage(Constants.OrleansDataStorageProvider, options =>
                    {
                        options.Invariant = invariant;
                        options.ConnectionString = connectionString;
                        options.UseJsonFormat = true;
                    })
                    .UseAdoNetReminderService(options =>
                    {
                        options.Invariant = invariant;
                        options.ConnectionString = connectionString;
                    })
                    .UseInMemoryReminderService()
                    .Configure<EndpointOptions>(options =>
                    {
                        var name = Dns.GetHostName(); // get container id
                        var ip = Dns.GetHostEntry(name).AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
                        // Port to use for Silo-to-Silo
                        options.SiloPort = 11111;
                        // Port to use for the gateway
                        options.GatewayPort = 30000;
                        // IP Address to advertise in the cluster
                        options.AdvertisedIPAddress = ip;
                        // The socket used for silo-to-silo will bind to this endpoint
                        options.GatewayListeningEndpoint = new IPEndPoint(IPAddress.Any, 40000);
                        // The socket used by the gateway will bind to this endpoint
                        options.SiloListeningEndpoint = new IPEndPoint(IPAddress.Any, 50000);

                    })
                    //.Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                    // need to configure a grain storage called "PubSubStore" for using
                    // streaming with ExplicitSubscribe pubsub type. Depends on your
                    // application requirements, you can configure your silo with other
                    // stream providers, which can provide other features, such as
                    // persistence or recoverability. For more information, please see
                    // http://dotnet.github.io/orleans/Documentation/streaming/streams_programming_APIs.html#fully-managed-and-reliable-streaming-pub-suba-namefully-managed-and-reliable-streaming-pub-suba
                    .AddMemoryGrainStorage(Constants.OrleansMemoryProvider)
                    .AddSimpleMessageStreamProvider(Constants.OrleansStreamProvider);
                })
                .ConfigureLogging(logging => logging.AddConsole());

            return host;
        }
        private static string GetEnvironment()
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.IsNullOrEmpty(environmentName))
            {
                Console.WriteLine("Production");
                return "Production";
            }
            Console.WriteLine(environmentName);
            return environmentName;
        }
        private static IConfigurationBuilder CreateConfigurationBuilder(string environmentName)
        {
            var config = new ConfigurationBuilder()
                             .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                             .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                             .AddEnvironmentVariables();
            return config;
        }
    }
}
