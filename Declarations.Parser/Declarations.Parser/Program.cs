using Declarations.Parser.Models;
using ICSharpCode.SharpZipLib.BZip2;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Declarations.Parser
{
    class Program
    {
        private static readonly ConcurrentDictionary<string, Declaration> declarations = new ConcurrentDictionary<string, Declaration>();
        private static int errors = 0;

        static void Main(string[] args)
        {
            Console.WriteLine($"Started at {DateTime.UtcNow}");
            Console.OutputEncoding = Encoding.UTF8;

            var input = File.OpenRead(@"C:\Users\igork\Downloads\full_export.json.bz2");
            var output = new BZip2InputStream(input);
            var streamReader = new StreamReader(output, Encoding.UTF8);

            var tasks = new List<Task>();
            var counter = 0;
            string line = streamReader.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                tasks.Add(ProcessOneLine(errors, line));

                line = streamReader.ReadLine();
                counter++;

                if (tasks.Count == 10000)
                {
                    Task.WaitAll(tasks.ToArray());
                    tasks.Clear();
                }
            }

            Console.WriteLine($"{counter} records were processed. {errors} was caught");
            Console.WriteLine($"Ended at {DateTime.UtcNow}");
        }

        private static async Task ProcessOneLine(int errors, string line)
        {
            await Task.Yield();

            try
            {
                var jobject = JObject.Parse(line);

                var infocard = jobject["infocard"];
                var step1 = jobject["unified_source"]["step_1"] ?? jobject["unified_source"]["data"]["step_1"]; ;

                var id = infocard["id"].ToString();
                var decl = new Declaration(id);
                decl.Persons.Add(
                    new Person
                    {
                        Id = 0,
                        LastName1 = infocard["last_name"].ToString(),
                        FirstName1 = infocard["first_name"].ToString(),
                        MiddleName1 = infocard["patronymic"].ToString(),
                        LastName2 = step1["lastname"].ToString(),
                        FirstName2 = step1["firstname"].ToString(),
                        MiddleName2 = step1["middlename"].ToString()
                    });

                declarations.TryAdd(id, decl);

                //var step2 = jobject["unified_source"]["step_2"];
                //var relatedEntities = jobject["related_entities"];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception {ex.Message} of type {ex.GetType()} at {Environment.NewLine}{ex.StackTrace}");
                Trace.WriteLine($"Exception {ex.Message} of type {ex.GetType()} at {Environment.NewLine}{ex.StackTrace}");
                errors++;
            }
        }
    }
}
