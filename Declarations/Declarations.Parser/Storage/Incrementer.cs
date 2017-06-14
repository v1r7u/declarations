using System.Collections.Concurrent;

namespace Declarations.Parser.Storage
{
    public class Incrementer
    {
        private ConcurrentDictionary<string, object> _lockers = new ConcurrentDictionary<string, object>();
        private ConcurrentDictionary<string, int> _identifiers = new ConcurrentDictionary<string, int>();

        internal int Increment(string key)
        {
            var locker = _lockers.GetOrAdd(key, k => new object());
            var id = _identifiers.GetOrAdd(key, 0);

            lock(locker)
            {
                return id++;
            }

        }
    }
}
