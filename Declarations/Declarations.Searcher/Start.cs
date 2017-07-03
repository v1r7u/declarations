using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Declarations.Searcher
{
    public class Start
    {
        private readonly Prepare _preparation = new Prepare();
        private readonly ElasticWrapper _elastic = new ElasticWrapper();
        private readonly OwnersProvider _owners = new OwnersProvider();

        private Start()
        {
        }

        public static Start New()
        {
            return new Start();
        }

        public async Task<Start> PrepareData()
        {
            Console.WriteLine($"[{DateTime.UtcNow}]: Preparation is started");

            await Task
                .WhenAll(
                    _preparation.LoadTranslations(),
                    _preparation.LoadDeclarations());

            Console.WriteLine($"[{DateTime.UtcNow}]: Preparation is finished");

            await _preparation.MapDeclarantsToTranslations();

            Console.WriteLine($"[{DateTime.UtcNow}]: Mapping is finished");

            _elastic.UploadBulk(_preparation.declarants.Values).Wait();

            Console.WriteLine($"[{DateTime.UtcNow}]: Uploading is finished");

            return this;
        }

        private List<ElasticResult<DeclarantEntity>> rawResults = new List<ElasticResult<DeclarantEntity>>();
        private List<Result> results = new List<Result>();
        
        public async Task<Start> SearchAll()
        {
            var sw = new Stopwatch();
            sw.Start();

            var ownersCount = 0;
            foreach (var owner in _owners.GetIterator())
            {
                ownersCount++;
                var o1 = owner;
                var docs = await _elastic.Search(o1.firstName, o1.lastName);

                if (docs.Any())
                    results.Add(new Result(o1.fullLine, docs.Select(i => new KeyValuePair<string, double?>(i.Result.DeclarationId, i.Score)).ToArray()));
            }

            sw.Stop();

            Console.WriteLine($"Processing {ownersCount} took {sw.Elapsed}. Found {results.Count()} matches");

            foreach (var group in results.GroupBy(i => i.DeclarationScores.Count()).OrderBy(g => g.Count()))
            {
                var resultsInGroup = group.Count();
                var matchesPerRecord = group.First().DeclarationScores.Count();
                Console.WriteLine($"{resultsInGroup} searches with {matchesPerRecord} matches. ");
                foreach(var match in group)
                {
                    var min = match.DeclarationScores.Min(i => i.Value);
                    var max = match.DeclarationScores.Max(i => i.Value);
                    var avg = match.DeclarationScores.Average(i => i.Value);

                    Console.WriteLine($"   *   Min:{min:00.0000}; Avg:{avg:00.0000} Max:{max:00.0000}; for {match.FullLine}");
                }
            }


            return this;
        }

        private class Result
        {
            internal Result(string fullLine, KeyValuePair<string, double?>[] declarationScores)
            {
                FullLine = fullLine;
                DeclarationScores = declarationScores;
            }

            internal string FullLine { get; }

            internal KeyValuePair<string, double?>[] DeclarationScores { get; }
        }

    }
}
