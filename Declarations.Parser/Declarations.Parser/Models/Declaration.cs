using System.Collections.Generic;

namespace Declarations.Parser.Models
{
    public class Declaration
    {
        public Declaration(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public List<Person> Persons { get; } = new List<Person>();
    }
}
