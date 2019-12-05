using Common;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleClient
{
    public class Program
    {
        private const int RetryDelaySec = 4;
        private const string ChannelName = "BroadcastDemo";
        private const int MaxRetry = 5;
        private static int retryCount = 0;
        private static IConfiguration Configuration { get; set; }
        public static int Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
                            .AddEnvironmentVariables();
            Configuration = builder.Build();
            return RunMain();
        }
        public static int RunMain()
        {
            try
            {
                DoGamePlayerWork();
                Console.ReadKey();
                return 0;
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
            var invariant = Configuration.GetSection("Invariant").GetValue<string>("DefaultDatabase");
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var client = new ClientBuilder()
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
                                .AddSimpleMessageStreamProvider(Constants.OrleansStreamProvider)
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
        private static async Task DoStatefulWork(IClusterClient client)
        {
            var grain1 = client.GetGrain<IVisitTrackerGrain>("aaaaa@gmail.com");
            var grain2 = client.GetGrain<IVisitTrackerGrain>("bbbbb@gmail.com");
            await PrettyPrintGrainVisits(grain1);
            await PrettyPrintGrainVisits(grain2);
            PrintSeparatorThing();

            Console.WriteLine("Some people are visiting!");
            await grain1.Visit();
            await grain1.Visit();
            await grain2.Visit();
            PrintSeparatorThing();

            await PrettyPrintGrainVisits(grain1);
            await PrettyPrintGrainVisits(grain2);
            PrintSeparatorThing();

            Console.Write("Visiting even more!");
            for (int i = 0; i < 5; i++)
            {
                await grain1.Visit();
            }
            PrintSeparatorThing();

            await PrettyPrintGrainVisits(grain1);
            await PrettyPrintGrainVisits(grain2);
        }
        private static async Task PrettyPrintGrainVisits(IVisitTrackerGrain grain)
        {
            Console.WriteLine($"{grain.GetPrimaryKeyString()} has visited {await grain.GetNumberOfVisits()} times");
        }
        private static void PrintSeparatorThing()
        {
            Console.WriteLine($"{Environment.NewLine}-----{Environment.NewLine}");
        }
        private static async Task DoChannelWork(IClusterClient client)
        {
            Console.WriteLine("Your Name?");
            var name = Console.ReadLine();

            await JoinChannel(client, name);

            Console.WriteLine("Type '<any text>' to send a message");
            Console.WriteLine("Type '/exit' to exit client.");
            string input;
            do
            {
                input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input)) continue;

                if (!input.StartsWith("/exit"))
                {
                    await SendMessage(client, name, input);
                }
            } while (input != "/exit");
        }
        private static async Task JoinChannel(IClusterClient client, string userName)
        {
            var channel = client.GetGrain<IChannelGrain>(ChannelName);
            var channelId = await channel.Join(userName);
            var stream = client.GetStreamProvider(Constants.OrleansStreamProvider)
                            .GetStream<string>(channelId, Constants.OrleansStreamNameSpace);

            await stream.SubscribeAsync((message, token) =>
            {
                Console.WriteLine(message);
                return Task.CompletedTask;
            });
        }
        private static async Task SendMessage(IClusterClient client, string userName, string message)
        {
            var room = client.GetGrain<IChannelGrain>(ChannelName);
            await room.Broadcast(userName, message);
        }
        private static void DoGamePlayerWork()
        {
            var options = new DbContextOptionsBuilder<GameContext>()
                            .UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
                            .Options;
            using (var context = new GameContext(options))
            {
                var games = context.Games.ToList();
                var players = context.Players.ToList();
                var points = new[] { 1, 2, 4, 10 };
                var RNM = new Random();

                while (!Console.KeyAvailable)
                {
                    var game = games[RNM.Next(games.Count())];
                    var player = players[RNM.Next(players.Count())];
                    var point = points[RNM.Next(points.Length)];

                    Console.WriteLine(game.Name);
                    Console.WriteLine(player.Name);
                    Console.WriteLine(point);

                    PostAddPoint(game.Id, player.Id, point);
                    Thread.Sleep(200);
                }
            }
            Console.WriteLine("Stop simulate.");

            void PostAddPoint(Guid gameId, Guid playerId, int point)
            {
                string hostUrl = "https://leaderboard-demo-api.azurewebsites.net/";
                //string hostUrl = "https://localhost:44309";
                string url = $"{hostUrl}/api/Player/Point";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                NameValueCollection postParams = System.Web.HttpUtility.ParseQueryString(string.Empty);
                postParams.Add("gameId", gameId.ToString());
                postParams.Add("playerId", playerId.ToString());
                postParams.Add("point", point.ToString());

                byte[] byteArray = Encoding.UTF8.GetBytes(postParams.ToString());
                using (Stream reqStream = request.GetRequestStream())
                {
                    reqStream.Write(byteArray, 0, byteArray.Length);
                }

                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        var res = sr.ReadToEnd();
                        Console.WriteLine(res);
                    }
                }
            }
        }
    }
}
