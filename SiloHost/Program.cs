using Common;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SiloHost
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }
        private static async Task<int> RunMainAsync()
        {
            try
            {
                var host = await StartSilo();
                Console.WriteLine("Press Enter to terminate...");
                Console.ReadLine();

                await host.StopAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }
        private static async Task<ISiloHost> StartSilo()
        {
            var silo = new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = Constants.ClusterId;
                    options.ServiceId = Constants.ServiceId;
                })
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                // need to configure a grain storage called "PubSubStore" for using
                // streaming with ExplicitSubscribe pubsub type. Depends on your
                // application requirements, you can configure your silo with other
                // stream providers, which can provide other features, such as
                // persistence or recoverability. For more information, please see
                // http://dotnet.github.io/orleans/Documentation/streaming/streams_programming_APIs.html#fully-managed-and-reliable-streaming-pub-suba-namefully-managed-and-reliable-streaming-pub-suba
                .AddMemoryGrainStorage(Constants.OrleansMemoryProvider)
                .AddSimpleMessageStreamProvider(Constants.OrleansStreamProvider)
                .ConfigureLogging(logging => logging.AddConsole())
                .Build();

            await silo.StartAsync();
            return silo;
        }
    }
}
