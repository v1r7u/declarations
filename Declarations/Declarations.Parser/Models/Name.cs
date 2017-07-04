namespace Declarations.Parser.Models
{
    public class Name
    {
        public Name(int id, int[] partIds)
        {
            Id = id;
            Parts = partIds;
        }

        public int Id { get; }

        public int[] Parts { get; }
    }
}
