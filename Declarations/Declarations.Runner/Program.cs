using Declarations.Parser;
using Declarations.Translator.Translators;
using System;
using System.Configuration;
using System.IO;

namespace Declarations.Runner
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine($"Started at {DateTime.UtcNow}");

            //var path = @"C:\Users\igork\Downloads\full_export.json.bz2";

            //Parse.FromFile(path, Encoding.UTF8);

            var oneRequestLength = int.Parse(ConfigurationManager.AppSettings["Translator.OneRequestMaxLength"]);
            var keysPath = ConfigurationManager.AppSettings["Translator.KeysFilePath"];

            var keys = File.ReadAllLines(keysPath);

            try
            {
                AzureTranslator
                    .New()
                    .From("uk")
                    .To("en")
                    .Output("bing-uk-en.csv")
                    .RequestLength(oneRequestLength)
                    .Keys(keys)
                    .Translate()
                    .Wait();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Exception {ex.Message} of type {ex.GetType()} at {Environment.NewLine}{ex.StackTrace}";
                Console.WriteLine($"[{DateTime.UtcNow}]: {errorMessage}");
            }

            Console.WriteLine($"[{DateTime.UtcNow}]: Press ENTER to exit");
            Console.ReadLine();
        }
    }
}
