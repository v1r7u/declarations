using System;

namespace Declarations.Parser.Models
{
    public class NamePart : IEquatable<NamePart>
    {
        public NamePart(int id, string value)
        {
            Id = id;
            Value = value;
        }

        public int Id { get; }

        public string Value { get; }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var other = obj as NamePart;

            if (other == null)
                return false;
            
            return this.Equals(other);
        }

        public bool Equals(NamePart other)
        {
            return Id == other.Id && string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
