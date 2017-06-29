using System;
using System.Threading.Tasks;

namespace Declarations.Searcher
{
    public class Start
    {
        private readonly Prepare _preparation = new Prepare();
        private readonly ElasticWrapper _loader = new ElasticWrapper();

        private Start()
        {
        }

        public static Start New()
        {
            return new Start();
        }

        public async Task<Start> PrepareData()
        {
            Console.WriteLine($"[{DateTime.UtcNow}]: Preparation is started");

            await Task
                .WhenAll(
                    _preparation.LoadTranslations(),
                    _preparation.LoadDeclarations());

            Console.WriteLine($"[{DateTime.UtcNow}]: Preparation is finished");

            await _preparation.MapDeclarantsToTranslations();

            Console.WriteLine($"[{DateTime.UtcNow}]: Mapping is finished");

            _loader.UploadBulk(_preparation.declarants.Values).Wait();

            Console.WriteLine($"[{DateTime.UtcNow}]: Uploading is finished");

            return this;
        }
    }
}
