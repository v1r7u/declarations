namespace Declarations.Searcher
{
    public class DeclarantEntity
    {
        public DeclarantEntity(string declId, string relation, string originName, string work, string position)
        {
            DeclarationId = declId;
            Relation = relation;
            OriginalFullName = originName;
            WorkPlace = work;
            Position = position;
        }

        public string[] FirstNames { get; set; }

        public string[] LastNames { get; set; }

        public string DeclarationId { get; set; }

        public string Relation { get; }

        public string OriginalFullName { get; set; }

        public string WorkPlace { get; set; }

        public string Position { get; set; }
    }
}
