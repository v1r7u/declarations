using Declarations.Parser;
using System;
using System.Text;

namespace Declarations.Runner
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine($"Started at {DateTime.UtcNow}");

            var path = @"C:\Users\igork\Downloads\full_export.json.bz2";

            Parse.FromFile(path, Encoding.UTF8);

            Console.WriteLine($"[{DateTime.UtcNow}]: Press ENTER to exit");
            Console.ReadLine();
        }
    }
}
