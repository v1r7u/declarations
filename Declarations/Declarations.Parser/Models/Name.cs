using System;
using System.Linq;

namespace Declarations.Parser.Models
{
    public class Name : IEquatable<Name>
    {
        public int Id { get; set; }

        public string Raw { get; set; }

        public int[] Parts { get; set; }

        public override int GetHashCode()
        {
            return Parts
                .Select(i => i.GetHashCode())
                .Aggregate((a, b) => b ^ a);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || (obj as Name) == null)
                return false;

            return this.Equals(obj as Name);
        }

        public bool Equals(Name other)
        {
            if (other.Parts.Length != Parts.Length)
                return false;

            for (var i=0; i < Parts.Length; i++)
            {
                if (Parts[i] != other.Parts[i])
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Raw)
                ? string.Join(" ", Parts)
                : Raw;
        }
    }
}
