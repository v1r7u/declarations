using System.Collections.Generic;

namespace Declarations.Parser.Models
{
    public class Person
    {
        public Person(int id, string declarationId, string relation, Name name)
        {
            Id = id;
            DeclarationId = declarationId;

            Names = new List<Name>();
            Names.Add(name);
        }

        public int Id { get; }

        public string DeclarationId { get; }

        public string Relation { get; }

        public List<Name> Names { get; }
    }
}
