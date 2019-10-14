using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Coin.API.Services
{
    public class LazyConcurrentDataService<TKey, TValue>
    {
        public static LazyConcurrentDataService<TKey, TValue> Data { get { return lazy.Value; } }

        static readonly Lazy<LazyConcurrentDataService<TKey, TValue>> lazy = new Lazy<LazyConcurrentDataService<TKey, TValue>>(() => new LazyConcurrentDataService<TKey, TValue>() );

        private readonly ConcurrentDictionary<TKey, Lazy<TValue>> concurrentDictionary;

        public LazyConcurrentDataService()
        {
            concurrentDictionary = new ConcurrentDictionary<TKey, Lazy<TValue>>();
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            var lazyResult = concurrentDictionary.GetOrAdd(key, k => new Lazy<TValue>(() => valueFactory(k), LazyThreadSafetyMode.ExecutionAndPublication));
            return lazyResult.Value;
        }

        public bool TryGetValue(TKey key, out Lazy<TValue> value)
        {
            return concurrentDictionary.TryGetValue(key, out value);
        }

        public TValue TryGetValue(TKey key)
        {
            concurrentDictionary.TryGetValue(key, out Lazy<TValue> value);
            return value.Value;
        }

        public bool TryUpdate(TKey key, Lazy<TValue> newValue, Lazy<TValue> comparisonValue)
        {
            return concurrentDictionary.TryUpdate(key, newValue, comparisonValue);
        }

        public KeyValuePair<TKey, Lazy<TValue>>[] GetEnumerable()
        {
             return concurrentDictionary.ToArray();
        }
    }
}
