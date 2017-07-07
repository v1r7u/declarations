using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Declarations.Searcher
{
    public class OwnersProvider
    {
        private const string _path = "D:/data/test.csv";

        public IEnumerable<(string lastName, string firstName, string fullLine)> GetIterator()
        {
            using (var stream = File.OpenRead(_path))
            using (var reader = new StreamReader(stream))
            {
                var line = reader.ReadLine();

                do
                {
                    var fullLine = string.Empty;
                    var firstName = string.Empty;
                    var lastName = string.Empty;

                    var lower = line.ToLowerInvariant();
                    if (!line.StartsWith(",") && !lower.Contains("r.o.") && !lower.Contains("a.s."))
                    {
                        var parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        var name = parts[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                        var nameParts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (nameParts.Length == 2)
                        {
                            lastName = nameParts[0].StartsWith("\"") ? nameParts[0].Substring(1) : nameParts[0];
                            firstName = nameParts[1];
                            yield return (lastName, firstName, line);
                        }
                        else if (nameParts.Length == 3)
                        {
                            if (nameParts[2].Contains("."))
                            {
                                lastName = nameParts[0].StartsWith("\"") ? nameParts[0].Substring(1) : nameParts[0];
                                firstName = nameParts[1];

                                yield return (lastName, firstName, line);
                            }
                            else
                            {
                                lastName = nameParts[0].StartsWith("\"") ? nameParts[0].Substring(1) : nameParts[0];
                                firstName = nameParts[2];

                                yield return (lastName, firstName, line);
                            }
                        }
                        else if (name.StartsWith("\"SJM", StringComparison.InvariantCultureIgnoreCase) ||
                                 name.StartsWith("\"MCP", StringComparison.InvariantCultureIgnoreCase))
                        {
                            yield return (nameParts[1], nameParts[2], line);

                            int i = 0;
                            for (i = 3; i < nameParts.Length; i++)
                            {
                                if (nameParts[i] == "a")
                                    break;
                            }

                            yield return (nameParts[i + 1], nameParts[i + 2], line);
                        }
                        else
                        {
                            Console.WriteLine($"WOW! something new! {line}");
                        }
                    }

                    line = reader.ReadLine();
                }
                while (!string.IsNullOrEmpty(line));
            }
        }
    }
}
