using Declarations.Parser.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

        public async Task<Start> LoadDeclarations()
        {
            await _preparation.LoadDeclarations();

            return this;
        }

        private List<ElasticResult<DeclarantEntity>> rawResults = new List<ElasticResult<DeclarantEntity>>();
        private List<Result> results = new List<Result>();
        
        public async Task<Start> SearchAll(int fuzzDistance)
        {
            var sw = new Stopwatch();
            sw.Start();

            var ownersCount = 0;
            foreach (var owner in _owners.GetIterator())
            {
                ownersCount++;
                var docs = await _elastic.Search(owner.firstName, owner.lastName, fuzzDistance);

                if (docs.Any())
                    results.Add(new Result(owner.fullLine, docs.Select(i => (i.Result, i.Score)).ToArray()));
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
                    var min = match.DeclarationScores.Min(i => i.score);
                    var max = match.DeclarationScores.Max(i => i.score);
                    var avg = match.DeclarationScores.Average(i => i.score);

                    Console.WriteLine($"   *   Min:{min:00.0000}; Avg:{avg:00.0000} Max:{max:00.0000}; for {match.FullLine}");
                }
            }

            return this;
        }

        private const string urlFormat = @"https://declarations.com.ua/declaration/{0}";

        public void SaveSearchResults(string csvPath)
        {
            var sb = new StringBuilder();
            foreach (var row in results)
            {
                var suspicionPersons = row.DeclarationScores
                    .OrderByDescending(i => i.score)
                    .Select(i => i.declaration)
                    .GroupBy(i => i, new DeclarantComparer());
                foreach (var person in suspicionPersons)
                {
                    var decl = person.Key;
                    var declarationUrls = string.Join(",", person.Select(i => string.Format(urlFormat, i.DeclarationId)));
                    sb.AppendLine($"{row.FullLine},{decl.Relation},{decl.OriginalFullName},{decl.WorkPlace},{decl.Position},{declarationUrls}");
                }
            }

            File.WriteAllText(csvPath, sb.ToString());
        }

        private class Result
        {
            internal Result(string fullLine, (DeclarantEntity declaration, double? score)[] declarationScores)
            {
                FullLine = fullLine;
                DeclarationScores = declarationScores;
            }

            internal string FullLine { get; }

            internal (DeclarantEntity declaration, double? score)[] DeclarationScores { get; }
        }

        private class DeclarantComparer : IEqualityComparer<DeclarantEntity>
        {
            public bool Equals(DeclarantEntity x, DeclarantEntity y)
            {
                if (string.IsNullOrWhiteSpace(x.OriginalFullName) ||
                    string.IsNullOrWhiteSpace(x.WorkPlace) ||
                    string.IsNullOrWhiteSpace(x.Position) ||
                    string.IsNullOrWhiteSpace(x.Relation))
                    return false;

                return string.Compare(x.OriginalFullName, y.OriginalFullName, StringComparison.CurrentCultureIgnoreCase) == 0 &&
                        string.Compare(x.WorkPlace, y.WorkPlace, StringComparison.CurrentCultureIgnoreCase) == 0 &&
                        string.Compare(x.Position, y.Position, StringComparison.CurrentCultureIgnoreCase) == 0 &&
                        string.Compare(x.Relation, y.Relation, StringComparison.CurrentCultureIgnoreCase) == 0;
            }

            public int GetHashCode(DeclarantEntity obj)
            {
                return obj.OriginalFullName.GetHashCode() ^ 
                       obj.WorkPlace.GetHashCode() ^ 
                       obj.Position.GetHashCode() ^ 
                       obj.Relation.GetHashCode();
            }
        }
    }
}
