using Common;
using Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;
using WebClient.Hubs;

namespace WebClient
{
    public class Startup
    {
        private int attempt = 0;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddSignalR();
            services.AddDbContext<GameContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            });
            services.AddSingleton<IClusterClient>(CreateClusterClient);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapHub<GameHub>("/gameHubs");
            });
        }
        private IClusterClient CreateClusterClient(IServiceProvider serviceProvider)
        {
            attempt = 0;
            var client = new ClientBuilder()
                              .Configure<ClusterOptions>(options =>
                              {
                                  options.ClusterId = Constants.ClusterId;
                                  options.ServiceId = Constants.ServiceId;
                              })
                              .UseLocalhostClustering()
                              .AddSimpleMessageStreamProvider(Constants.OrleansStreamProvider)
                              .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IGameGrain).Assembly).WithReferences())
                              .Build();

            client.Connect(RetryFilter).Wait();
            return client;

            async Task<bool> RetryFilter(Exception exception)
            {
                ; if (exception.GetType() != typeof(SiloUnavailableException))
                {
                    Console.WriteLine($"Cluster client failed to connect to cluster with unexpected error.  Exception: {exception}");
                    return false;
                }
                attempt++;
                Console.WriteLine($"Cluster client attempt {attempt} of {Constants.MaxRetry} failed to connect to cluster.  Exception: {exception}");
                if (attempt > Constants.MaxRetry)
                {
                    return false;
                }
                await Task.Delay(TimeSpan.FromSeconds(Constants.RetryDelaySec));
                return true;
            }
        }
    }
}
