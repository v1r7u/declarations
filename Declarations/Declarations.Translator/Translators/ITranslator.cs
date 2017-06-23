using System.Threading.Tasks;

namespace Declarations.Translator.Translators
{
    public interface ITranslator
    {
        ITranslator Input(string inputPath);
        ITranslator Output(string inputPath);

        ITranslator From(string from);
        ITranslator To(string to);

        ITranslator Keys(string[] keys);
        ITranslator RequestLength(int length);

        Task Translate();
    }
}
