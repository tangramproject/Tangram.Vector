using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SwimProtocol.Collections
{
    public class ConcurrentDictionaryEx<TKey, TValue>
    {
        private readonly object _lock = new object();
        private ConcurrentDictionary<TKey, (TValue, DateTime)> _dic;
        public int Capacity { get; set; }
        public int Count { get; set; }

        public ConcurrentDictionaryEx(int capacity, int concurrencyLevel = 2)
        {
            Capacity = capacity;
            _dic = new ConcurrentDictionary<TKey, (TValue, DateTime)>(concurrencyLevel, capacity);
        }

        public bool TryAdd(TKey key, TValue value)
        {
            lock (_lock)
            {
                if (Count < Capacity && _dic.TryAdd(key, (value, DateTime.UtcNow)))
                {
                    Count++;
                    return true;
                }

                //  Replace old record on add.
                if (Count >= Capacity)
                {
                    var item_to_remove = _dic.ToList().OrderBy(x => x.Value.Item2).FirstOrDefault().Key;

                    (TValue, DateTime) v;

                    if (_dic.TryRemove(key, out v))
                    {
                        Count--;

                        if (_dic.TryAdd(key, (value, DateTime.UtcNow)))
                        {
                            Count++;
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            lock (_lock)
            {
                (TValue, DateTime) v;

                if (_dic.TryRemove(key, out v))
                {
                    Count--;

                    value = v.Item1;
                    return true;
                }

                value = default(TValue);
                return false;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_lock)
            {
                (TValue, DateTime) v;
                var success = _dic.TryGetValue(key, out v);

                if (success)
                {
                    value = v.Item1;
                }
                else
                {
                    value = default(TValue);
                }

                return success;
            }
        }
    }
}
