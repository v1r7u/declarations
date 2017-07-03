namespace Declarations.Searcher
{
    public class ElasticResult<T>
    {
        public ElasticResult(T result, double? score)
        {
            Result = result;

            Score = score;
        }

        public T Result { get; }

        public double? Score { get; }
    }
}
