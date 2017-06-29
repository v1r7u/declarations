using System;
using System.Collections.Generic;
using System.IO;

namespace Declarations.Searcher
{
    public class OwnersProvider
    {
        private const string _path = "./../../test.csv";

        public IEnumerable<(string lastName, string firstName, string id)> GetIterator()
        {
            using (var stream = File.OpenRead(_path))
            using (var reader = new StreamReader(stream))
            {
                var line = reader.ReadLine();

                do
                {
                    var id = string.Empty;
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
                            id = parts[0];
                            lastName = nameParts[0];
                            firstName = nameParts[1];
                        }
                    }
                    catch(Exception ex)
                    {
                        var errorMessage = $"Exception {ex.Message} of type {ex.GetType()} at {Environment.NewLine}{ex.StackTrace}";
                        Console.WriteLine($"[{DateTime.UtcNow}]: {errorMessage}");
                    }

                    if (!string.IsNullOrEmpty(id))
                    {
                        yield return (lastName, firstName, id);
                    }
                }
                while (!string.IsNullOrEmpty(line));
            }
        }
    }
}
