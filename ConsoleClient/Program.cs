using Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;

namespace ConsoleClient
{
    public class Program
    {
        private const int RetryDelaySec = 4;
        private const int MaxRetry = 5;
        private static int retryCount = 0;

        public static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }
        public static async Task<int> RunMainAsync()
        {
            try
            {
                using (var client = await CreateOrleansClient())
                {
                    await DoClientWork(client);
                    Console.ReadKey();
                    return 0;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
                return 1;
            }
        }
        private static async Task<IClusterClient> CreateOrleansClient()
        {
            retryCount = 0;
            var client = new ClientBuilder()
                                .UseLocalhostClustering()
                                .Configure<ClusterOptions>(options =>
                                {
                                    options.ClusterId = "dev";
                                    options.ServiceId = "HelloApp";
                                })
                                .ConfigureLogging(logging => logging.AddConsole())
                                .Build();
            await client.Connect(RetryFilter);
            Console.WriteLine("Client successfully connect to silo host");
            return client;

            async Task<bool> RetryFilter(Exception exception)
            {
                if (exception.GetType() != typeof(SiloUnavailableException))
                {
                    Console.WriteLine($"Cluster client failed to connect to cluster with unexpected error.  Exception: {exception}");
                    return false;
                }
                retryCount++;
                Console.WriteLine($"Cluster client attempt {retryCount} of {MaxRetry} failed to connect to cluster.  Exception: {exception}");
                if (retryCount > MaxRetry)
                {
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(RetryDelaySec));
                return true;
            }
        }
        private static async Task DoClientWork(IClusterClient client)
        {
            Console.WriteLine("Hello, what should I call you?");
            var name = Console.ReadLine();

            if (string.IsNullOrEmpty(name))
            {
                name = "anon";
            }

            // example of calling grains from the initialized client
            var grain = client.GetGrain<IHelloGrain>(Guid.NewGuid());

            var response = await grain.HelloWorld(name);
            Console.WriteLine($"\n\n{response}\n\n");
        }
    }
}
