namespace Declarations.Translator
{
    public class TranslationPair
    {
        public TranslationPair(int id, string original)
        {
            Id = id;
            Original = original;
        }

        public int Id { get; set; }

        public string Original { get; set; }

        public string Translation { get; set; }
    }
}
