namespace Declarations.Searcher
{
    public class DeclarantEntity
    {
        public DeclarantEntity(string declId)
        {
            DeclarationId = declId;
        }

        public string DeclarationId { get; set; }

        public string[] FirstNames { get; set; }

        public string[] LastNames { get; set; }
    }
}
