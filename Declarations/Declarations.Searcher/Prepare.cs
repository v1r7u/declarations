using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Declarations.Searcher
{
    internal class Prepare
    {
        private readonly string[] translationFiles = new[]
        {
            @"D:\data\all_trans.csv",
            @"D:\data\bing-ru-cs.csv",
            @"D:\data\bing-uk-cs.csv",
            @"D:\data\bing-ru-en.csv",
            @"D:\data\bing-uk-en.csv"
        };

        private readonly ConcurrentDictionary<int, HashSet<string>> allTranslations = new ConcurrentDictionary<int, HashSet<string>>();
        private readonly ConcurrentBag<string> notDefaultNameTypes = new ConcurrentBag<string>();

        internal readonly ConcurrentDictionary<int, DeclarantEntity> declarants = new ConcurrentDictionary<int, DeclarantEntity>();

        internal Task LoadTranslations()
        {
            var tasks = translationFiles
                .Select(file => ProcessFile(file))
                .ToArray();

            return Task.WhenAll(tasks);
        }

        internal async Task LoadDeclarations()
        {
            using (var stream = File.OpenRead(@"D:\data\persons.csv"))
            using (var reader = new StreamReader(stream))
            {
                var line = await reader.ReadLineAsync();
                line = await reader.ReadLineAsync();
                while (!string.IsNullOrEmpty(line))
                {
                    var parts = line.Split(',');
                    var nameId = int.Parse(parts[3]);
                    var declId = parts[1];

                    declarants.TryAdd(nameId, new DeclarantEntity(declId));

                    line = await reader.ReadLineAsync();
                }
            }
        }

        internal async Task MapDeclarantsToTranslations()
        {
            using (var stream = File.OpenRead(@"D:\data\names.csv"))
            using (var reader = new StreamReader(stream))
            {
                var line = await reader.ReadLineAsync();
                line = await reader.ReadLineAsync();
                while (!string.IsNullOrEmpty(line))
                {
                    try
                    {
                        var parts = line.Split(',');
                        var nameId = int.Parse(parts[0]);
                        var namePartIds = parts.Last().Split(' ');

                        if (namePartIds.Length == 3 || namePartIds.Length == 2)
                        {
                            if (declarants.TryGetValue(nameId, out var declarant))
                            {
                                if (allTranslations.TryGetValue(int.Parse(namePartIds[0]), out var lastNames))
                                    declarant.LastNames = lastNames.ToArray();

                                if (allTranslations.TryGetValue(int.Parse(namePartIds[1]), out var firstNames))
                                    declarant.FirstNames = firstNames.ToArray();
                            }
                        }
                        else
                        {
                            notDefaultNameTypes.Add(line);
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $"Exception {ex.Message} of type {ex.GetType()} at {Environment.NewLine}{ex.StackTrace}";
                        Console.WriteLine($"[{DateTime.UtcNow}]: {errorMessage}");
                        notDefaultNameTypes.Add(line);
                    }

                    line = await reader.ReadLineAsync();
                }
            }
        }

        private async Task ProcessFile(string file)
        {
            using (var stream = File.OpenRead(file))
            using (var reader = new StreamReader(stream))
            {
                var line = await reader.ReadLineAsync();
                line = await reader.ReadLineAsync();
                while (!string.IsNullOrEmpty(line))
                {
                    var parts = line.Split(',');
                    var id = int.Parse(parts[0]);
                    for (var i = 2; i < parts.Length; i++)
                    {
                        var set = allTranslations.GetOrAdd(id, key => new HashSet<string>(new string[] { parts[1] }));
                        set.Add(parts[i]);
                    }

                    line = await reader.ReadLineAsync();
                }
            }
        }
    }
}
