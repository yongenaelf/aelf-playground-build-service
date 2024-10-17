using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Serialization;
using PlaygroundService.Grains;
using Custom;

public class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Use the custom configuration manager
            var maxFileSizeMB = int.Parse(CustomConfigurationManager.AppSetting["ClamAV:MaxFileSizeMB"]);
            int maxFileSize = maxFileSizeMB * 1024 * 1024;
            var siloPort = 11111;
            var gatewayPort = 30000;
            var host = new HostBuilder()
                .UseOrleans((ctx, siloBuilder) => siloBuilder
                    // .UseLocalhostClustering()
                    .UseZooKeeperClustering(options =>
                    {
                        options.ConnectionString = CustomConfigurationManager.AppSetting["Zookeeper:ConnectionString"];
                    })
                    .ConfigureEndpoints(siloPort, gatewayPort)
                    .ConfigureServices(services =>
                    {

                        services.AddSerializer(serializerBuilder =>
                        {
                            serializerBuilder.AddNewtonsoftJsonSerializer(
                                    isSupported: type => type.Namespace.StartsWith("Grains"))
                                .AddNewtonsoftJsonSerializer(
                                    isSupported: type => type.Namespace.StartsWith("Controllers"));
                        });
                    })
                    .Configure<ClientMessagingOptions>(opts =>
                    {
                        opts.ResponseTimeout = TimeSpan.FromMinutes(30);
                    })
                    .Configure<SiloMessagingOptions>(opts =>
                    {
                        opts.ResponseTimeout = TimeSpan.FromMinutes(30);
                    })
                )
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.Limits.MaxRequestBodySize = maxFileSize;
                    });
                })
                .ConfigureLogging(logging => logging.AddConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "yyyy-MM-dd hh:mm:ss";
                }))
                .Build();

            await host.StartAsync();
            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            Console.WriteLine("start fail, e: " + ex.Message);
        }
    }
}
