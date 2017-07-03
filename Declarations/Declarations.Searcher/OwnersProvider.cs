using System;
using System.Collections.Generic;
using System.IO;

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

                    line = reader.ReadLine();
                    try
                    {
                        var parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        var name = parts[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[0];

                        var nameParts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (nameParts.Length == 2)
                        {
                            fullLine = line;
                            lastName = nameParts[0].StartsWith("\"") ? nameParts[0].Substring(1) : nameParts[0];
                            firstName = nameParts[1];
                        }
                    }
                    catch(Exception ex)
                    {
                        var errorMessage = $"Exception {ex.Message} of type {ex.GetType()} at {Environment.NewLine}{ex.StackTrace}";
                        Console.WriteLine($"[{DateTime.UtcNow}]: {errorMessage}");
                    }

                    if (!string.IsNullOrEmpty(fullLine))
                    {
                        yield return (lastName, firstName, fullLine);
                    }
                }
                while (!string.IsNullOrEmpty(line));
            }
        }
    }
}
