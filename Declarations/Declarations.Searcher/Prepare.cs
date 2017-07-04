using Declarations.Parser.Models;
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
        private readonly string translationFiles = @"D:\data\2\all_trans.csv";

        private readonly ConcurrentDictionary<int, HashSet<string>> allTranslations = new ConcurrentDictionary<int, HashSet<string>>();
        private readonly ConcurrentBag<string> notDefaultNameTypes = new ConcurrentBag<string>();

        internal readonly ConcurrentDictionary<int, DeclarantEntity> declarants = new ConcurrentDictionary<int, DeclarantEntity>();
        internal readonly ConcurrentDictionary<int, Person> persons = new ConcurrentDictionary<int, Person>();

        internal Task LoadTranslations()
        {
            return ProcessFile(translationFiles);
        }

        internal async Task LoadDeclarations()
        {
            using (var stream = File.OpenRead(@"D:\data\2\persons.csv"))
            using (var reader = new StreamReader(stream))
            {
                var line = await reader.ReadLineAsync();
                line = await reader.ReadLineAsync();
                while (!string.IsNullOrEmpty(line))
                {
                    try
                    {
                        var parts = line.Split(',');
                        var nameId = int.Parse(parts.Last());
                        var id = int.Parse(parts.First());
                        var declId = parts[1];

                        declarants.TryAdd(nameId, new DeclarantEntity(id, declId));
                        persons.TryAdd(id, new Person(declId, parts[2], parts[3], parts[4], parts[5]));
                    }
                    catch(Exception ex)
                    {
                        var errorMessage = $"Exception {ex.Message} of type {ex.GetType()} at {Environment.NewLine}{ex.StackTrace}";
                        Console.WriteLine($"[{DateTime.UtcNow}]: {errorMessage}");
                    }

                    line = await reader.ReadLineAsync();
                }
            }
        }

        internal async Task MapDeclarantsToTranslations()
        {
            using (var stream = File.OpenRead(@"D:\data\2\names.csv"))
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
                    var parts = line
                        .Split(',')
                        .ToArray();

                    var id = int.Parse(parts[0]);
                    for (var i = 2; i < parts.Length; i++)
                    {
                        var set = allTranslations.GetOrAdd(id, key => new HashSet<string>(new string[] { RemoveNonAlphanumeric(parts[1]) }));
                        set.Add(RemoveNonAlphanumeric(parts[i]));
                    }

                    line = await reader.ReadLineAsync();
                }
            }
        }

        private string RemoveNonAlphanumeric(string str)
        {
            var result = str.Where(c => char.IsLetter(c)).ToArray();

            return new string(result);
        }
    }
}
