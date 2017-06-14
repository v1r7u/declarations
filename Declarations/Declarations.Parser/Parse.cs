using Declarations.Parser.Models;
using Declarations.Parser.Storage;
using ICSharpCode.SharpZipLib.BZip2;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Declarations.Parser
{
    public static class Parse
    {
        private static int personId = 0;
        private static int nameId = 0;
        private static int namePartId = 0;
        internal static readonly ConcurrentBag<Person> persons = new ConcurrentBag<Person>();
        internal static readonly ConcurrentBag<Name> names = new ConcurrentBag<Name>();
        internal static readonly ConcurrentDictionary<string, NamePart> nameParts = new ConcurrentDictionary<string, NamePart>();
        internal static readonly ConcurrentBag<string> errors = new ConcurrentBag<string>();

        public static void FromFile(string path, Encoding encoding)
        {
            Console.OutputEncoding = encoding;

            var input = File.OpenRead(path);
            var output = new BZip2InputStream(input);
            var streamReader = new StreamReader(output, encoding);

            var counter = 0;
            string line = streamReader.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                ProcessOneLine(line);

                line = streamReader.ReadLine();
                counter++;

                if (counter % 100_000 == 0)
                {
                    Console.WriteLine($"[{DateTime.UtcNow}]: {counter} records were processed. There are {persons.Count} with {names.Count} names and {nameParts.Count} name parts");
                }
            }

            Console.WriteLine($"{counter} records were processed. There are {persons.Count} with {names.Count} names and {nameParts.Count} name parts. {errors.Count} was caught");
            Console.WriteLine($"Ended at {DateTime.UtcNow}");

            Task.WaitAll(
                SqlStorage.SavePersons(encoding),
                SqlStorage.SaveNames(encoding),
                SqlStorage.SaveNameParts(encoding),
                FileStorage.SaveErrors(encoding));
        }

        private static async Task ProcessOneLine(string line)
        {
            await Task.Yield();

            JToken step2 = null;

            try
            {
                var jobject = JObject.Parse(line);
                var infocard = jobject["infocard"];
                var declarationId = infocard["id"].ToString();

                var lastName = infocard["last_name"].ToString();
                var firstName = infocard["first_name"].ToString();
                var middleName = infocard["patronymic"].ToString();

                var raw = $"{lastName} {firstName} {middleName}";
                var parts = Splitter(raw);
                SavePerson(declarationId,"owner", parts, raw);

                step2 = jobject["unified_source"]["step_2"];

                if (step2 == null)
                {
                    if (jobject["unified_source"]["data"] != null)
                    {
                        step2 = jobject["unified_source"]["data"]["step_2"];
                        foreach (var member in step2)
                        {
                            var m1 = member;
                            var bio = m1.First["bio_declomua"].ToString();

                            if (string.IsNullOrWhiteSpace(bio))
                            {
                                continue;
                            }

                            var relation = m1.First["subjectRelation"].ToString();

                            parts = Splitter(bio);
                            SavePerson(declarationId, relation, parts, bio);
                        }
                    }
                }
                else if (step2["empty"] != null)
                {

                }
                else
                {
                    foreach (var member in step2)
                    {
                        var m1 = member.First;
                        if (m1["lastname"] == null || m1["firstname"] == null || m1["middlename"] == null)
                        {
                            continue;
                        }

                        lastName = m1["lastname"].ToString();
                        firstName = m1["firstname"].ToString();
                        middleName = m1["middlename"].ToString();
                        var relation = m1["subjectRelation"].ToString();

                        raw = $"{lastName} {firstName} {middleName}";
                        parts = Splitter(raw);

                        SavePerson(declarationId, relation, parts, raw);
                    }
                }
            }
            catch (Exception ex)
            {
                if (step2 != null)
                {
                    Console.WriteLine(step2.ToString());
                }

                Console.WriteLine($"Exception {ex.Message} of type {ex.GetType()} at {Environment.NewLine}{ex.StackTrace}");
                errors.Add(line);
            }
        }

        private static void SavePerson(string declarationId, string relation, string[] parts, string rawData)
        {
            var partIds = parts
                .Select(i => nameParts.GetOrAdd(i, (key) => new NamePart(Interlocked.Increment(ref namePartId), key)))
                .ToArray();

            var name = new Name
            {
                Id = Interlocked.Increment(ref nameId),
                Raw = rawData,
                Parts = partIds.Select(i => i.Id).ToArray()
            };

            names.Add(name);
            
            persons.Add(new Person(Interlocked.Increment(ref personId), declarationId, relation, name));
        }

        private static string[] Splitter(string i)
        {
            var parts = i.Split(new[] { ' ', '.', '-' }, StringSplitOptions.RemoveEmptyEntries);
            
            return parts;
        }

        private static string[] Splitter(string lastName, string firstName, string middleName)
        {
            var parts1 = lastName.Split(new[] { ' ', '.', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var parts2 = firstName.Split(new[] { ' ', '.', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var parts3 = middleName.Split(new[] { ' ', '.', '-' }, StringSplitOptions.RemoveEmptyEntries);

            return parts1.Concat(parts2).Concat(parts3).ToArray();
        }
    }
}
