using System;
using System.Linq;

namespace Contracts.Builders
{
    public static class RandomBuilderHelper
    {
        private static readonly Random Random = new Random(Guid.NewGuid().GetHashCode());

        public static string RandomGuid()
        {
            return Guid.NewGuid().ToString();
        }

        public static double RandomRate()
        {
            return Random.NextDouble();
        }

        public static DateTime RandomDate()
        {
            var days = -Random.Next(10, 30);
            var months = -Random.Next(10, 30);
            var years = -Random.Next(10, 30);
            return DateTime.Now.AddDays(days).AddMonths(months).AddYears(years);
        }

        public static int RandomNumber(int min = 1, int max = 100)
        {
            return Random.Next(min, max);
        }

        public static string RandomEmail()
        {
            return $"{RandomString()}@poc.com";
        }

        public static string RandomString(int length = 10)
        {
            const string chars = @"ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        public static T RandomFrom<T>(params T[] objects)
        {
            if (objects == null) return default;
            var index = Random.Next(objects.Length);
            return objects[index];
        }
    }
}
