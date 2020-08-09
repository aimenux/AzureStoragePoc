using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using App.Configuration;
using App.Extensions;
using BlobStorageLib;
using Contracts.Builders;
using Contracts.Extensions;
using Contracts.Ports.Configuration;
using Contracts.Ports.CosmosDb;
using Contracts.Ports.SearchService;
using CosmosSdkLib;
using LegacyStorageLib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SearchSdkLib;
using SearchStorageLib;
using SoftBlobStorageLib;
using SoftSearchStorageLib;

namespace App
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            const int size = 1000;

            var environment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "DEV";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            services.Configure<Settings>(configuration.GetSection(nameof(Settings)));
            services.AddSingleton<IContractsBuilder>(_ => new ContractsBuilder(size));
            services.AddSingleton<ISettings>(provider => provider.GetService<IOptions<Settings>>().Value);
            services.AddSingleton(provider => provider.GetService<IOptions<Settings>>().Value.BlobSettings);
            services.AddSingleton(provider => provider.GetService<IOptions<Settings>>().Value.CosmosSettings);
            services.AddSingleton(provider => provider.GetService<IOptions<Settings>>().Value.SearchSettings);
            services.AddSingleton(typeof(ISearchClient<>), typeof(SearchClient<>));
            services.AddSingleton(typeof(ICosmosDbClient), typeof(CosmosDbClient));
            services.AddSingleton(typeof(ICosmosDbClient<>), typeof(CosmosDbClient<>));

            services.AddSingleton<ICosmosDbStorage, LegacyCosmosDbStorage>();
            services.AddSingleton<ICosmosDbStorage, SearchCosmosDbStorage>();
            services.AddSingleton<ICosmosDbStorage, BlobCosmosDbStorage>();
            services.AddSingleton<ICosmosDbStorage, SoftBlobCosmosDbStorage>();
            services.AddSingleton<ICosmosDbStorage, SoftSearchCosmosDbStorage>();

            var serviceProvider = services.BuildServiceProvider();
            var cosmosDbClient = serviceProvider.GetService<ICosmosDbClient>();
            var contractsBuilder = serviceProvider.GetService<IContractsBuilder>();
            var cosmosDbStorages = serviceProvider.GetServices<ICosmosDbStorage>();
            
            var throughput = await cosmosDbClient.ReadThroughputAsync();

            ConsoleColor.Cyan.WriteLine($"Running strategies with '{size}' products [Throughput is configured to '{throughput}' RU]\n");

            foreach (var cosmosDbStorage in cosmosDbStorages)
            {
                try
                {
                    await RunStrategy(cosmosDbStorage, contractsBuilder);
                }
                catch (Exception ex)
                {
                    ConsoleColor.Red.WriteLine($"{ex.Message}\n");
                }
            }

            ConsoleColor.Gray.WriteLine("Press any key to exit !");
            Console.ReadKey();
        }

        private static async Task RunStrategy(ICosmosDbStorage cosmosDbStorage, IContractsBuilder contractsBuilder)
        {
            ConsoleColor.Green.WriteLine($"Strategy '{cosmosDbStorage.GetType().Name}'\n");
            
            var transactionId = Guid.NewGuid().ToString();
            var data = contractsBuilder.BuildDataContracts(transactionId);
            var payload = contractsBuilder.BuildPayloadContracts(transactionId);

            var timer = new Stopwatch();
            timer.Start();

            var saveResponse = await cosmosDbStorage.SaveAsync(payload, data);
            saveResponse.PrintResponse(nameof(saveResponse));

            var orderId = data.Response.Orders.First().OrderId;
            var getResponse = await cosmosDbStorage.GetAsync(orderId);
            getResponse.PrintResponse(nameof(getResponse));

            timer.Stop();

            ConsoleColor.Gray.WriteLine($"ElapsedTime: {timer.ElapsedMilliseconds} ms\n");
        }
    }
}
