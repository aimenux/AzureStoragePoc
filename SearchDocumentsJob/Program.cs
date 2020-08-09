using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Contracts.Ports.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SearchDocumentsJob.Builders;
using SearchDocumentsJob.Models;
using SearchSdkLib;
using static Bullseye.Targets;

namespace SearchDocumentsJob
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;

            var environment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "DEV";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            services.Configure<SearchSettings>(configuration.GetSection(nameof(SearchSettings)));
            services.AddSingleton<ISearchModelBuilder>(provider =>
            {
                var nbrOrders = configuration.GetValue<int>("Size:NbrOrdersPerTransaction");
                return new TransactionIdSearchModelBuilder(nbrOrders);
            });
            services.AddSingleton<SearchClient<TransactionIdSearchIndex>>();
            services.AddSingleton<ISearchSettings>(provider =>
            {
                var options = provider.GetService<IOptions<SearchSettings>>();
                return options.Value;
            });

            var serviceProvider = services.BuildServiceProvider();

            var searchClient = serviceProvider.GetService<SearchClient<TransactionIdSearchIndex>>();
            var searchModelBuilder = serviceProvider.GetService<ISearchModelBuilder>();

            Target(nameof(Targets.QueryTopFive), async () =>
            {
                var parameters = new SearchClientParameters { Top = 5 };
                var documents = await searchClient.GetAsync<TransactionIdSearchModel>("*", parameters);
                Console.WriteLine($"Found '{documents.Count}' with top 5");
                foreach (var document in documents)
                {
                    var nbrOrders = document.OrderIds.Count;
                    var transactionId = document.TransactionId;
                    Console.WriteLine($"Document with TransactionId '{transactionId}' has NbrOrders '{nbrOrders}'");
                }
            });

            Target(nameof(Targets.IndexSize), async () =>
            {
                double size = await searchClient.SizeAsync();
                var mb = Math.Round(size / 1000000, 2);
                var gb = Math.Round(mb / 1000, 2);
                var indexName = configuration.GetValue<string>("SearchSettings:IndexName");
                Console.WriteLine($"Size index '{indexName}' : ~ ({mb} megabytes) ~ ({gb} gigabytes)");
            });

            Target(nameof(Targets.CountDocuments), async () =>
            {
                var count = await searchClient.CountAsync();
                Console.WriteLine($"Found '{count}' documents");
            });

            Target(nameof(Targets.FindDocuments), async () =>
            {
                var orderId = configuration.GetValue<string>("Operations:OrderId");

                if (!string.IsNullOrWhiteSpace(orderId))
                {
                    var parameters = new SearchClientParameters
                    {
                        Filter = $"OrderIds/any(orderId: orderId eq '{orderId}')"
                    };

                    var documents = await searchClient.GetAsync<TransactionIdSearchModel>("*", parameters);
                    Console.WriteLine($"Found '{documents.Count}' document(s) for OrderId '{orderId}'");
                    if (documents.Count == 1)
                    {
                        var document = documents.Single();
                        var nbrOrders = document.OrderIds.Count;
                        Console.WriteLine($"Document with transactionId '{document.TransactionId}' has NbrOrders '{nbrOrders}'");
                    }
                }

                var transactionId = configuration.GetValue<string>("Operations:TransactionId");

                if (!string.IsNullOrWhiteSpace(transactionId))
                {
                    var parameters = new SearchClientParameters
                    {
                        Filter = $"TransactionId eq '{transactionId}'"
                    };

                    var documents = await searchClient.GetAsync<TransactionIdSearchModel>("*", parameters);
                    Console.WriteLine($"Found '{documents.Count}' document(s) for TransactionId '{transactionId}'");
                    if (documents.Count == 1)
                    {
                        var document = documents.Single();
                        var nbrOrders = document.OrderIds.Count;
                        Console.WriteLine($"Document with transactionId '{document.TransactionId}' has NbrOrders '{nbrOrders}'");
                    }
                }
            });

            Target(nameof(Targets.SequentialUploadDocuments), async () =>
            {
                var size = configuration.GetValue<int>("Size:SequentialUpload");

                Console.WriteLine($"Sequential uploading in progress for '{size}' documents");

                const int slice = 32_000;
                var batchs = Enumerable.Range(0, size)
                    .Batch(slice)
                    .ToList();

                var batchNumber = 0;
                foreach (var batch in batchs)
                {
                    Console.WriteLine($"Uploading batch {++batchNumber}/{batchs.Count}");
                    var models = batch
                        .Select(_ => searchModelBuilder.BuildSearchModel())
                        .ToList();
                    await searchClient.SaveAsync(models);
                }
            });

            Target(nameof(Targets.ConcurrentUploadDocuments), async () =>
            {
                var size = configuration.GetValue<int>("Size:ConcurrentUpload");

                Console.WriteLine($"Concurrent uploading in progress for '{size}' documents");

                var models = Enumerable.Range(0, size)
                    .Select(_ => searchModelBuilder.BuildSearchModel())
                    .ToList();

                var tasks = models
                    .Select(x => searchClient.SaveAsync(x))
                    .ToList();

                await Task.WhenAll(tasks);
            });

            Target(nameof(Targets.DeleteDocuments), async () =>
            {
                var orderId = configuration.GetValue<string>("Operations:OrderId");

                if (!string.IsNullOrWhiteSpace(orderId))
                {
                    var parameters = new SearchClientParameters
                    {
                        Filter = $"OrderIds/any(orderId: orderId eq '{orderId}')"
                    };

                    var documents = await searchClient.GetAsync<TransactionIdSearchModel>("*", parameters);
                    if (documents.Any())
                    {
                        var transactionIds = documents.Select(x => x.TransactionId).ToList();
                        await searchClient.DeleteDocumentsAsync(nameof(TransactionIdSearchModel.TransactionId), transactionIds);
                        Console.WriteLine($"Deleted '{documents.Count}' document(s) for OrderId '{orderId}'");
                    }
                }

                var transactionId = configuration.GetValue<string>("Operations:TransactionId");

                if (!string.IsNullOrWhiteSpace(transactionId))
                {
                    var parameters = new SearchClientParameters
                    {
                        Filter = $"TransactionId eq '{transactionId}'"
                    };

                    var documents = await searchClient.GetAsync<TransactionIdSearchModel>("*", parameters);
                    if (documents.Any())
                    {
                        var transactionIds = documents.Select(x => x.TransactionId).ToList();
                        await searchClient.DeleteDocumentsAsync(nameof(TransactionIdSearchModel.TransactionId), transactionIds);
                        Console.WriteLine($"Deleted '{documents.Count}' document(s) for TransactionId '{transactionId}'");
                    }
                }
            });

            Target(nameof(Targets.UpdateDocuments), async () =>
            {
                var orderId = configuration.GetValue<string>("Operations:OrderId");

                if (!string.IsNullOrWhiteSpace(orderId))
                {
                    var parameters = new SearchClientParameters
                    {
                        Filter = $"OrderIds/any(orderId: orderId eq '{orderId}')"
                    };

                    var documents = await searchClient.GetAsync<TransactionIdSearchModel>("*", parameters);
                    if (documents.Any())
                    {
                        var newDocuments = documents
                            .Select(x => new TransactionIdSearchModel
                            {
                                TransactionId = x.TransactionId,
                                TransactionDate = x.TransactionDate,
                                OrderIds = x.OrderIds.Take(1).ToList()
                            }).ToList();
                        await searchClient.UpdateAsync(newDocuments);
                        Console.WriteLine($"Updated '{newDocuments.Count}' document(s) for OrderId '{orderId}'");
                    }
                }

                var transactionId = configuration.GetValue<string>("Operations:TransactionId");

                if (!string.IsNullOrWhiteSpace(transactionId))
                {
                    var parameters = new SearchClientParameters
                    {
                        Filter = $"TransactionId eq '{transactionId}'"
                    };

                    var documents = await searchClient.GetAsync<TransactionIdSearchModel>("*", parameters);
                    if (documents.Any())
                    {
                        var newDocuments = documents
                            .Select(x => new TransactionIdSearchModel
                            {
                                TransactionId = x.TransactionId,
                                TransactionDate = x.TransactionDate,
                                OrderIds = x.OrderIds.Take(1).ToList()
                            }).ToList();
                        await searchClient.UpdateAsync(newDocuments);
                        Console.WriteLine($"Updated '{newDocuments.Count}' document(s) for TransactionId '{transactionId}'");
                    }
                }
            });

            Target(nameof(Targets.DeleteIndexAndDocuments), async () =>
            {
                await searchClient.DeleteIndexAndDocumentsAsync();
            });

            Target(nameof(Targets.Default), DependsOn(nameof(Targets.CountDocuments)));

            await RunTargetsWithoutExitingAsync(args);
        }
    }
}
