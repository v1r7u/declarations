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
                FileStorage.SavePersons(encoding),
                FileStorage.SaveNames(encoding),
                FileStorage.SaveNameParts(encoding),
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
                var workPlace = infocard["office"].ToString();
                var position = infocard["position"].ToString();
                var relation = "owner";

                var fullName = $"{lastName} {firstName} {middleName}";
                SavePerson(declarationId,relation, fullName, workPlace, position);

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

                            relation = m1.First["subjectRelation"].ToString();
                            
                            SavePerson(declarationId, relation, bio, workPlace, position);
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
                        relation = m1["subjectRelation"].ToString();

                        fullName = $"{lastName} {firstName} {middleName}";

                        SavePerson(declarationId, relation, fullName, workPlace, position);
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

        private static void SavePerson(string declarationId, string relation, string originName, string work = null, string position = null)
        {
            var parts = Splitter(originName);
            var partIds = parts
                .Select(i => nameParts.GetOrAdd(i.ToUpper(), (key) => new NamePart(Interlocked.Increment(ref namePartId), i)).Id)
                .ToArray();

            var name = new Name(Interlocked.Increment(ref nameId), partIds);
            names.Add(name);
            
            persons.Add(new Person(Interlocked.Increment(ref personId), declarationId, relation, originName, work, position, name));
        }

        private static string[] Splitter(string str)
        {
            var parts = str
                .Split(new[] { ' ', '.', '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.RemoveNonAlphanumeric())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
            
            return parts;
        }

        private static string RemoveNonAlphanumeric(this string str)
        {
            var result = str.Where(c => char.IsLetter(c)).ToArray();

            return new string(result);
        }
    }
}
