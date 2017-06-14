using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Declarations.Parser.Storage
{
    internal class FileStorage
    {
        internal static async Task SavePersons(Encoding encoding)
        {
            var personsFile = File.Create("persons.csv");

            var headers = encoding.GetBytes($"id,declaration-id,relation,name-ids{Environment.NewLine}");
            await personsFile.WriteAsync(headers, 0, headers.Length);

            foreach (var pers in Parse.persons)
            {
                var str = $"{pers.Id},{pers.DeclarationId},{pers.Relation},{string.Join(",", pers.Names.Select(i => i.Id))}{Environment.NewLine}";
                var bytes = encoding.GetBytes(str);
                await personsFile.WriteAsync(bytes, 0, bytes.Length);
            }

            personsFile.Close();
        }

        internal static async Task SaveNames(Encoding encoding)
        {
            var namesFile = File.Create("names.csv");

            var headers = encoding.GetBytes($"id,raw,name-part-ids{Environment.NewLine}");
            await namesFile.WriteAsync(headers, 0, headers.Length);

            foreach (var name in Parse.names)
            {
                var str = $"{name.Id},{name.Raw},{string.Join(" ", name.Parts)}{Environment.NewLine}";
                var bytes = encoding.GetBytes(str);
                await namesFile.WriteAsync(bytes, 0, bytes.Length);
            }

            namesFile.Close();
        }

        internal static async Task SaveNameParts(Encoding encoding)
        {
            var namePartsFile = File.Create("nameParts.csv");

            var headers = encoding.GetBytes($"id,value{Environment.NewLine}");
            await namePartsFile.WriteAsync(headers, 0, headers.Length);

            foreach (var part in Parse.nameParts.Values)
            {
                var str = $"{part.Id},{part.Value}{Environment.NewLine}";
                var bytes = encoding.GetBytes(str);
                await namePartsFile.WriteAsync(bytes, 0, bytes.Length);
            }

            namePartsFile.Close();
        }

        internal static async Task SaveErrors(Encoding encoding)
        {
            var errorsFile = File.Create("errors.csv");
            foreach (var err in Parse.errors)
            {
                var str = $"{err}{Environment.NewLine}";
                var bytes = encoding.GetBytes(str);
                await errorsFile.WriteAsync(bytes, 0, bytes.Length);
            }

            errorsFile.Close();
        }
    }
}
