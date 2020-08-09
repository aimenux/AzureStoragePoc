using System;
using Contracts.Extensions;
using Contracts.Ports.CosmosDb;

namespace App.Extensions
{
    public static class Extensions
    {
        public static void PrintResponse(this ICosmosDbResponse response, string description)
        {
            ConsoleColor.Blue.WriteLine(description.Beautify());
            var totalRequestUnits = response.RequestUnits;
            var totalRequestUnitsKey = nameof(totalRequestUnits).Beautify();
            ConsoleColor.Yellow.WriteLine($"{totalRequestUnitsKey}: {totalRequestUnits} RU");
            foreach (var (key, value) in response.DynamicInformations)
            {
                if (value is ICosmosDbResponse cosmosDbResponse)
                {
                    ConsoleColor.Yellow.WriteLine($"{key.Beautify()}: {cosmosDbResponse.RequestUnits} RU");
                }
                else
                {
                    ConsoleColor.Yellow.WriteLine($"{key.Beautify()}: {value}");
                }
            }
            Console.WriteLine();
        }
    }
}
