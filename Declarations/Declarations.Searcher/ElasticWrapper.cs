using Elasticsearch.Net;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Declarations.Searcher
{
    public class ElasticWrapper
    {
        private readonly Uri elasticUri = new Uri("http://localhost:9200");

        public async Task UploadBulk(IEnumerable<DeclarantEntity> declarants)
        {
            var pool = new SingleNodeConnectionPool(elasticUri);
            var connectionSettings = new ConnectionSettings(pool);

            var client = new ElasticClient(connectionSettings);

            var bulk = new List<DeclarantEntity>();
            foreach(var decl in declarants)
            {
                bulk.Add(decl);

                if (bulk.Count == 10000)
                {
                    await SendBulk(client, bulk);
                }
            }

            if (bulk.Count > 0)
            {
                await SendBulk(client, bulk);
            }
        }

        public async Task<ElasticResult<DeclarantEntity>[]> Search(string firstName, string lastName, int fuzzDistance)
        {
            var pool = new SingleNodeConnectionPool(elasticUri);
            var connectionSettings = new ConnectionSettings(pool);

            var client = new ElasticClient(connectionSettings);
            
            var searchResults = await client
                .SearchAsync<DeclarantEntity>(search => search
                    .Index("declarants")
                    .MinScore(10.0)
                    .Size(200)
                    .Query(q => q
                        .Bool(b => b
                            .Must(s =>
                                s.Match(m => m
                                    .Query(firstName)
                                    .Field(f => f.FirstNames)
                                    .Fuzziness(Fuzziness.EditDistance(fuzzDistance))),
                                s => s.Match(m => m
                                    .Query(lastName)
                                    .Field(f => f.LastNames)
                                    .Fuzziness(Fuzziness.EditDistance(fuzzDistance)))))));

            return searchResults
                .Hits
                .Select(i => new ElasticResult<DeclarantEntity>(i.Source, i.Score))
                .ToArray();
        }

        private static async Task SendBulk(ElasticClient client, List<DeclarantEntity> bulk)
        {
            try
            {
                var response = await client.IndexManyAsync(bulk, "declarants");
            }
            catch (Exception ex)
            {
                var errorMessage = $"Exception {ex.Message} of type {ex.GetType()} at {Environment.NewLine}{ex.StackTrace}";
                Console.WriteLine($"[{DateTime.UtcNow}]: {errorMessage}");
            }
            bulk.Clear();
        }
    }
}
