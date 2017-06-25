using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Declarations.Translator.Translators
{
    public class YandexTranslator : ITranslator
    {

        private static readonly HttpClient http = new HttpClient();

        private readonly ConcurrentBag<TranslationPair> allTranslations = new ConcurrentBag<TranslationPair>();
        private readonly ConcurrentBag<string> errors = new ConcurrentBag<string>();

        private YandexTranslator()
        {
            http.DefaultRequestHeaders.Clear();
        }

        public static ITranslator New()
        {
            return new YandexTranslator();
        }

        private string inputPath = "nameParts.csv";
        public ITranslator Input(string inputPath)
        {
            this.inputPath = inputPath;
            return this;
        }

        private string outputPath = "translations.csv";
        public ITranslator Output(string inputPath)
        {
            this.outputPath = inputPath;
            return this;
        }

        private string from;
        public ITranslator From(string from)
        {
            this.from = from;
            return this;
        }

        private string to;
        public ITranslator To(string to)
        {
            this.to = to;
            return this;
        }

        private string[] keys;
        public ITranslator Keys(string[] keys)
        {
            this.keys = keys;
            return this;
        }

        private int oneRequestLength;
        public ITranslator RequestLength(int length)
        {
            oneRequestLength = length;
            return this;
        }

        public async Task Translate()
        {
            Console.WriteLine($"[{DateTime.UtcNow}]: Translation started from {from} to {to}.");
            var input = File.OpenRead(inputPath);
            var streamReader = new StreamReader(input);

            var orderedList = new List<TranslationPair>();
            var sb = new StringBuilder();
            string line = streamReader.ReadLine();
            line = streamReader.ReadLine();

            while (!string.IsNullOrEmpty(line))
            {
                var parts = line.Split(',');
                var id = int.Parse(parts[0]);
                var name = parts[1];

                if (sb.Length + name.Length == oneRequestLength)
                {
                    orderedList.Add(new TranslationPair(id, name));
                    sb.Append(name);

                    ProcessOneRequest(sb.ToString(), orderedList.ToArray()).Wait();

                    sb.Clear();
                    orderedList.Clear();
                }
                else if (sb.Length + name.Length + 2 > oneRequestLength)
                {
                    ProcessOneRequest(sb.ToString(), orderedList.ToArray()).Wait();

                    sb.Clear();
                    orderedList.Clear();

                    orderedList.Add(new TranslationPair(id, name));
                    sb.AppendLine(name);
                }
                else
                {
                    orderedList.Add(new TranslationPair(id, name));
                    sb.AppendLine(name);
                }

                line = streamReader.ReadLine();

            }

            if (sb.Length > 0)
            {
                ProcessOneRequest(sb.ToString(), orderedList.ToArray()).Wait();
                orderedList.Clear();
                sb.Clear();
            }

            Console.WriteLine($"[{DateTime.UtcNow}]: Saving {allTranslations.Count} to the file {outputPath}");

            sb.AppendLine($"id,original-text,bing-translation-from-{from}-to-{to}");
            foreach (var trns in allTranslations)
                sb.AppendLine($"{trns.Id},{trns.Original},{trns.Translation}");

            File.WriteAllText(outputPath, sb.ToString());
            Console.WriteLine($"[{DateTime.UtcNow}]: {allTranslations.Count} translations were saved");

            var errorsFileName = $"bing-translation-from-{from}-to-{to}-ERRORS.csv";
            File.WriteAllLines(errorsFileName, errors.ToArray());
            Console.WriteLine($"[{DateTime.UtcNow}]: {errors.Count} errors were saved to {errorsFileName}");
        }

        private int requests = 0;
        private byte currentTokenIndex = 0;
        private async Task ProcessOneRequest(string line, TranslationPair[] translations)
        {
            if (++requests % 10 == 0)
            {
                Console.WriteLine($"[{DateTime.UtcNow}]: {requests} was processed");
            }

            if (from == "ru")
            {
                line = new string(TranscryptLine(line).ToArray());
            }

            var beginningIndex = currentTokenIndex;
            var succeeded = false;

            do
            {
                try
                {
                    var text = HttpUtility.UrlEncode(line);
                    var key = keys[currentTokenIndex];
                    var uri = new Uri("https://translate.yandex.net/api/v1.5/tr/translate?key=" + $"{key}&text={text}&lang={from}-{to}");

                    using (var request = new HttpRequestMessage())
                    {
                        request.Method = HttpMethod.Get;
                        request.RequestUri = uri;
                        var response = await http.SendAsync(request);
                        response.EnsureSuccessStatusCode();
                        var responseContent = await response.Content.ReadAsStringAsync();

                        var startAt = responseContent.IndexOf("<text>") + 6;
                        var length = responseContent.IndexOf("</text>") - startAt;

                        var translation = responseContent.Substring(startAt, length);
                        //DataContractSerializer dcs = new DataContractSerializer(Type.GetType("System.String"));
                        //string translation = (string)dcs.ReadObject(stream);

                        var lines = translation.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        for (var i = 0; i < lines.Length; i++)
                        {
                            translations[i].Translation = lines[i].Trim();
                        }

                        succeeded = true;
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Exception {ex.Message} of type {ex.GetType()} at {Environment.NewLine}{ex.StackTrace}";
                    Console.WriteLine($"[{DateTime.UtcNow}]: {errorMessage}");
                    errors.Add($"\"{line}\" was failed with:{errors}");
                }

                if (!succeeded)
                {
                    Console.WriteLine($"[{DateTime.UtcNow}]: Switching to the next key with index {++currentTokenIndex}");

                    currentTokenIndex = (byte)(currentTokenIndex == keys.Length
                        ? 0
                        : currentTokenIndex);
                }
            }
            while (!succeeded && beginningIndex != currentTokenIndex);

            foreach (var trans in translations)
                allTranslations.Add(trans);
        }

        private IEnumerable<char> TranscryptLine(string line)
        {
            foreach (var c in line)
            {
                switch (c)
                {
                    case 'и':
                        yield return 'ы';
                        break;
                    case 'И':
                        yield return 'Ы';
                        break;
                    case 'ї':
                    case 'і':
                        yield return 'и';
                        break;
                    case 'Ї':
                    case 'І':
                        yield return 'И';
                        break;
                    case 'є':
                        yield return 'е';
                        break;
                    case 'Є':
                        yield return 'Е';
                        break;
                    default:
                        yield return c;
                        break;
                }
            }
        }
    }
}
