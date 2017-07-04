namespace Declarations.Searcher
{
    public class DeclarantEntity
    {
        public DeclarantEntity(int internalId, string declId)
        {
            InternalId = internalId;
            DeclarationId = declId;
        }

        public int InternalId { get; set; }

        public string DeclarationId { get; set; }

        public string[] FirstNames { get; set; }

        public string[] LastNames { get; set; }
    }
}
