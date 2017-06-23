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
    public class AzureTranslator : ITranslator
    {
        private static readonly HttpClient http = new HttpClient();

        private readonly ConcurrentBag<TranslationPair> allTranslations = new ConcurrentBag<TranslationPair>();
        private readonly ConcurrentBag<string> errors = new ConcurrentBag<string>();
        
        private AzureAuthToken authTokenSource;
        private byte currentTokenIndex = 0;

        private AzureTranslator()
        {
            http.DefaultRequestHeaders.Clear();
        }

        public static ITranslator New()
        {
            return new AzureTranslator();
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
            authTokenSource = new AzureAuthToken(keys[currentTokenIndex]);
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

            //var tasks = new List<Task>();
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

            //Console.WriteLine($"[{DateTime.UtcNow}]: Composing new tasks finished");
            //await Task.WhenAll(tasks);

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
                string authToken = await GetToken();

                if (!string.IsNullOrEmpty(authToken))
                {
                    try
                    {
                        var text = HttpUtility.UrlEncode(line);
                        var uri = new Uri("https://api.microsofttranslator.com/v2/http.svc/Translate?text=" + $"{text}&from={from}&to={to}");

                        using (var request = new HttpRequestMessage())
                        {
                            request.Method = HttpMethod.Get;
                            request.RequestUri = uri;
                            request.Headers.TryAddWithoutValidation("Authorization", authToken);
                            var response = await http.SendAsync(request);
                            response.EnsureSuccessStatusCode();
                            var stream = await response.Content.ReadAsStreamAsync();

                            DataContractSerializer dcs = new DataContractSerializer(Type.GetType("System.String"));
                            string translation = (string)dcs.ReadObject(stream);

                            var lines = translation.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                            for (var i = 0; i < lines.Length; i++)
                            {
                                translations[i].Translation = lines[i];
                            }

                            succeeded = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $"Exception {ex.Message} of type {ex.GetType()} at {Environment.NewLine}{ex.StackTrace}";
                        Console.WriteLine($"[{DateTime.UtcNow}]: {errorMessage}");
                        errors.Add($"{line} was failed with:{errors}");
                    }
                }

                if (!succeeded)
                {
                    Console.WriteLine($"[{DateTime.UtcNow}]: Switching to the next key with index {++currentTokenIndex}");

                    currentTokenIndex = (byte)(currentTokenIndex == keys.Length
                        ? 0
                        : currentTokenIndex);
                    authTokenSource = new AzureAuthToken(keys[currentTokenIndex]);
                }
            }
            while (!succeeded && beginningIndex != currentTokenIndex);

            foreach (var trans in translations)
                allTranslations.Add(trans);
        }

        private async Task<string> GetToken()
        {
            try
            {
                return await authTokenSource.GetAccessTokenAsync();
            }
            catch (HttpRequestException)
            {
                if (authTokenSource.RequestStatusCode == HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine($"[{DateTime.UtcNow}]: Request to token service is not authorized (401). Check that the Azure subscription key is valid.");
                    return string.Empty;
                }
                if (authTokenSource.RequestStatusCode == HttpStatusCode.Forbidden)
                {
                    Console.WriteLine($"[{DateTime.UtcNow}]: Request to token service is not authorized (403). For accounts in the free-tier, check that the account quota is not exceeded.");
                    return string.Empty;
                }
                throw;
            }
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
