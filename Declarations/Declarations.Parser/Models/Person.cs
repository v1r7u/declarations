using System.Collections.Generic;

namespace Declarations.Parser.Models
{
    public class Person
    {
        public Person(int id, string declarationId, string relation, string originName, string work, string position, Name name)
        {
            Id = id;
            DeclarationId = declarationId;
            Relation = relation;
            OriginalFullName = originName;
            WorkPlace = work;
            Position = position;

            Names = new List<Name>();
            Names.Add(name);
        }

        public int Id { get; }

        public string DeclarationId { get; }

        public string Relation { get; }

        public string OriginalFullName { get; set; }

        public string WorkPlace { get; set; }

        public string Position { get; set; }

        public List<Name> Names { get; }
    }
}
