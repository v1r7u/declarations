using System;
using System.Collections.Generic;

namespace Declarations.Parser.Models
{
    public class Name : IEquatable<Name>
    {
        public int Id { get; set; }

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        public string Raw { get; set; }

        public List<Person> Persons { get; } = new List<Person>();

        public override int GetHashCode()
        {
            return FirstName.GetHashCode() ^ MiddleName.GetHashCode() ^ LastName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || (obj as Name) == null)
                return false;

            return this.Equals(obj as Name);
        }

        public bool Equals(Name other)
        {
            return string.Compare(FirstName, other.FirstName, StringComparison.OrdinalIgnoreCase) == 0 &&
                   string.Compare(MiddleName, other.MiddleName, StringComparison.OrdinalIgnoreCase) == 0 &&
                   string.Compare(LastName, other.LastName, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Raw)
                ? $"{LastName} {FirstName} {MiddleName}"
                : Raw;
        }
    }
}
