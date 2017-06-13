using Declarations.Parser.Models;
using ICSharpCode.SharpZipLib.BZip2;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Declarations.Parser
{
    class Program
    {
        private static int personId = 0;
        private static readonly ConcurrentBag<Person> persons = new ConcurrentBag<Person>();
        private static readonly ConcurrentDictionary<string, Name> names = new ConcurrentDictionary<string, Name>();
        private static readonly ConcurrentBag<string> errors = new ConcurrentBag<string>();

        static void Main(string[] args)
        {
            Console.WriteLine($"Started at {DateTime.UtcNow}");
            Console.OutputEncoding = Encoding.UTF8;

            var input = File.OpenRead(@"C:\Users\igork\Downloads\full_export.json.bz2");
            var output = new BZip2InputStream(input);
            var streamReader = new StreamReader(output, Encoding.UTF8);

            var counter = 0;
            string line = streamReader.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                ProcessOneLine(line);

                line = streamReader.ReadLine();
                counter++;

                if (counter % 100_000 == 0)
                {
                    Console.WriteLine($"[{DateTime.UtcNow}]: {counter} records were processed. There are {persons.Count} with {names.Count} names");
                }
            }

            Console.WriteLine($"{counter} records were processed. There are {persons.Count} with {names.Count} names. {errors.Count} was caught");
            Console.WriteLine($"Ended at {DateTime.UtcNow}");
            //Console.WriteLine("Press ENTER to save results");
            //Console.ReadLine();

            Task.WaitAll(SavePersons(), SaveNames(), SaveErrors());
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }

        private static async Task SavePersons()
        {
            var personsFile = File.Create("persons.csv");
            foreach (var pers in persons)
            {
                var str = $"{pers.Id},{pers.DeclarationId},{string.Join(",", pers.Names.Select(i=> i.Id))}{Environment.NewLine}";
                var bytes = Encoding.UTF8.GetBytes(str);
                await personsFile.WriteAsync(bytes, 0, bytes.Length);
            }

            personsFile.Close();
        }

        private static async Task SaveNames()
        {
            var namesFile = File.Create("names.csv");
            foreach (var name in names.Values)
            {
                var str = $"{name.Id},{name.LastName},{name.FirstName},{name.MiddleName},{name.Raw}{Environment.NewLine}";
                var bytes = Encoding.UTF8.GetBytes(str);
                await namesFile.WriteAsync(bytes, 0, bytes.Length);
            }

            namesFile.Close();
        }

        private static async Task SaveErrors()
        {
            var errorsFile = File.Create("errors.csv");
            foreach (var err in errors)
            {
                var str = $"{err}{Environment.NewLine}";
                var bytes = Encoding.UTF8.GetBytes(str);
                await errorsFile.WriteAsync(bytes, 0, bytes.Length);
            }

            errorsFile.Close();
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

                SavePerson(
                    declarationId,
                    infocard["last_name"].ToString(),
                    infocard["first_name"].ToString(),
                    infocard["patronymic"].ToString());

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

                            var m = Splitter(bio);
                            SavePerson(declarationId, m.lastname, m.firstname, m.middlename, bio);
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

                        SavePerson(
                            declarationId,
                            m1["lastname"].ToString(),
                            m1["firstname"].ToString(),
                            m1["middlename"].ToString());
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
                Trace.WriteLine($"Exception {ex.Message} of type {ex.GetType()} at {Environment.NewLine}{ex.StackTrace}");
                errors.Add(line);
            }
        }

        private static void SavePerson(string declarationId, string lastName, string firstName, string middleName, string rawData = null)
        {
            var id = personId++;
            var name = new Name
            {
                Id = id,
                LastName = lastName,
                FirstName = firstName,
                MiddleName = middleName,
                Raw = rawData
            };
            var fromDictionary = names.GetOrAdd(name.ToString(), name);
            persons.Add(new Person(id, declarationId, fromDictionary));
        }

        private static (string lastname, string firstname, string middlename) Splitter(string i)
        {
            var parts = i.Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
            {
                return (parts[0], string.Empty, string.Empty);
            }

            if (parts.Length == 2)
            {
                return (parts[0], parts[1], string.Empty);
            }

            return (parts[0], parts[1], parts[2]);
        }
    }
}
