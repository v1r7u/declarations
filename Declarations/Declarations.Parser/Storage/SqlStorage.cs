using Dapper;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Declarations.Parser.Storage
{
    internal class SqlStorage
    {
        private const string connectionString = @"Data Source=.;Initial Catalog=Declarations;Integrated Security=True";

        internal static async Task SavePersons(Encoding encoding)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var batchSize = 20000;
                var counter = 0;
                var length = Parse.persons.Count;

                while (counter < length)
                {
                    var personsBatch = Parse.persons
                        .Skip(counter)
                        .Take(batchSize)
                        .Select(pers => new { id = pers.Id, decl = pers.DeclarationId, rel = pers.Relation })
                        .ToArray();

                    var result = await connection.ExecuteAsync(
                        @"insert into Person([Id], [DeclarationId], [Relation]) values (@id, @decl, @rel)",
                        personsBatch
                      );

                    counter += result;
                }
            }
        }

        internal static async Task SaveNames(Encoding encoding)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var batchSize = 10000;
                var counter = 0;
                var length = Parse.names.Count;

                while (counter < length)
                {
                    var namesBatch = Parse.names
                        .Skip(counter)
                        .Take(batchSize)
                        .Select(name => new { id = name.Id, raw = name.Raw, parts = string.Join(" ", name.Parts) })
                        .ToArray();

                    var result = await connection.ExecuteAsync(
                        @"insert into [dbo].[Name]([Id], [Raw], [Parts]) values (@id, @raw, @parts)",
                        namesBatch
                      );

                    counter += result;
                }
            }
        }

        internal static async Task SaveNameParts(Encoding encoding)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var batchSize = 10000;
                var counter = 0;
                var length = Parse.nameParts.Count;

                while (counter < length)
                {
                    var namePartsBatch = Parse.nameParts.Values
                        .Skip(counter)
                        .Take(batchSize)
                        .Select(part => new { id = part.Id, val = part.Value })
                        .ToArray();

                    var result = await connection.ExecuteAsync(
                        @"insert into [dbo].[NamePart]([Id], [Value]) values (@id, @val)",
                        namePartsBatch
                      );

                    counter += result;
                }
            }
        }
    }
}
