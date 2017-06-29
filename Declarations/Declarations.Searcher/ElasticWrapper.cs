using Elasticsearch.Net;
using Nest;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Declarations.Searcher
{
    public class ElasticWrapper
    {
        public async Task UploadBulk(IEnumerable<DeclarantEntity> declarants)
        {
            var pool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
            var connectionSettings = new ConnectionSettings(pool);

            var client = new ElasticClient(connectionSettings);

            var bulk = new List<DeclarantEntity>();
            foreach(var decl in declarants)
            {
                bulk.Add(decl);

                if (bulk.Count == 1000)
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
    }
}
