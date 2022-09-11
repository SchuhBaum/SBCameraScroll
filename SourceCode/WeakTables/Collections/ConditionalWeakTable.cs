#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

#nullable disable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WeakTables.Collections
{
    /// <summary>
    /// Holds weak handles to instances of <typeparamref name="TKey"/> to simulate fields of type <typeparamref name="TValue"/>.
    /// <para/> Due to the restrictions of the .NET Framework 3.5 CLR, if a value contains a reference to its key in any way, it will never be garbage collected.
    /// </summary>
    /// <typeparam name="TKey">The type to which the field is attached.</typeparam>
    /// <typeparam name="TValue">The field's type. This must be a reference type.</typeparam>
    public sealed class ConditionalWeakTable<TKey, TValue> where TKey : notnull where TValue : class
    {
        private struct Entry
        {
            public DependentHandle depHnd;
            public int hashCode;
            public int next;
        }

        private struct DependentHandle
        {
            private readonly GCHandle primary;
            private readonly TValue secondary;

            public DependentHandle(object primary, TValue secondary)
            {
                this.primary = GCHandle.Alloc(primary, GCHandleType.Weak);
                this.secondary = secondary;
            }

            public bool IsAllocated => primary.IsAllocated;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public object GetPrimary() => primary.Target;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public void GetPrimaryAndSecondary(out object primary, out TValue secondary)
            {
                primary = this.primary.Target;
                secondary = this.secondary;
            }

            public void Free()
            {
                if (IsAllocated) {
                    primary.Free();
                }
            }
        }

        private int[] _buckets = new int[0];
        private Entry[] _entries = new Entry[0];
        private int _freeList = -1;
        private const int _initialCapacity = 5;
        private readonly object _lock = new object();
        private bool _invalid;

        public ConditionalWeakTable()
        {
            Resize();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key is null)
                throw new ArgumentNullException("key");

            lock (_lock) {
                VerifyIntegrity();
                return TryGetValueWorker(key, out value);
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (key is null)
                throw new ArgumentNullException("key");

            lock (_lock) {
                VerifyIntegrity();
                _invalid = true;

                int entryIndex = FindEntry(key);
                if (entryIndex != -1) {
                    _invalid = false;
                    throw new InvalidOperationException("Duplicate key");
                }

                CreateEntry(key, value);
                _invalid = false;
            }
        }

        public bool Remove(TKey key)
        {
            if (key is null)
                throw new ArgumentNullException();

            lock (_lock) {
                VerifyIntegrity();
                _invalid = true;

                int hashCode = RuntimeHelpers.GetHashCode(key) & int.MaxValue;
                int bucket = hashCode % _buckets.Length;
                int last = -1;
                for (int entriesIndex = _buckets[bucket]; entriesIndex != -1; entriesIndex = _entries[entriesIndex].next) {
                    if (_entries[entriesIndex].hashCode == hashCode && ReferenceEquals(_entries[entriesIndex].depHnd.GetPrimary(), key)) {
                        if (last == -1)
                            _buckets[bucket] = _entries[entriesIndex].next;
                        else
                            _entries[last].next = _entries[entriesIndex].next;

                        _entries[entriesIndex].depHnd.Free();
                        _entries[entriesIndex].next = _freeList;

                        _freeList = entriesIndex;

                        _invalid = false;
                        return true;

                    }
                    last = entriesIndex;
                }
                _invalid = false;
                return false;
            }
        }

        public TValue GetValue(TKey key, CreateValueCallback createValueCallback)
        {
            if (createValueCallback is null)
                throw new ArgumentNullException("createValueCallback");

            if (TryGetValue(key, out TValue existingValue))
                return existingValue;

            TValue newValue = createValueCallback(key);

            lock (_lock) {
                VerifyIntegrity();
                _invalid = true;

                if (TryGetValueWorker(key, out existingValue)) {
                    _invalid = false;
                    return existingValue;
                } else {
                    CreateEntry(key, newValue);
                    _invalid = false;
                    return newValue;
                }
            }
        }

        public delegate TValue CreateValueCallback(TKey key);

        internal TKey FindEquivalentKeyUnsafe(TKey key, out TValue value)
        {
            lock (_lock)
                for (int bucket = 0; bucket < _buckets.Length; ++bucket)
                    for (int entriesIndex = _buckets[bucket]; entriesIndex != -1; entriesIndex = _entries[entriesIndex].next) {
                        _entries[entriesIndex].depHnd.GetPrimaryAndSecondary(out var thisKey, out var thisValue);
                        if (Equals(thisKey, key)) {
                            value = thisValue;
                            return (TKey)thisKey;
                        }
                    }

            value = default;
            return default;
        }

        internal ICollection<TKey> Keys {
            get {
                List<TKey> list = new List<TKey>();
                lock (_lock)
                    for (int bucket = 0; bucket < _buckets.Length; ++bucket)
                        for (int entriesIndex = _buckets[bucket]; entriesIndex != -1; entriesIndex = _entries[entriesIndex].next) {
                            TKey thisKey = (TKey)_entries[entriesIndex].depHnd.GetPrimary();
                            if (thisKey is object)
                                list.Add(thisKey);
                        }

                return list;
            }
        }

        internal ICollection<TValue> Values {
            get {
                List<TValue> list = new List<TValue>();
                lock (_lock)
                    for (int bucket = 0; bucket < _buckets.Length; ++bucket)
                        for (int entriesIndex = _buckets[bucket]; entriesIndex != -1; entriesIndex = _entries[entriesIndex].next) {
                            _entries[entriesIndex].depHnd.GetPrimaryAndSecondary(out var primary, out var secondary);

                            if (primary is object)
                                list.Add(secondary);
                        }

                return list;
            }
        }

        public void Clear()
        {
            lock (_lock) {
                for (int bucketIndex = 0; bucketIndex < _buckets.Length; bucketIndex++)
                    _buckets[bucketIndex] = -1;

                int entriesIndex;
                for (entriesIndex = 0; entriesIndex < _entries.Length; entriesIndex++) {
                    if (_entries[entriesIndex].depHnd.IsAllocated)
                        _entries[entriesIndex].depHnd.Free();

                    _entries[entriesIndex].next = entriesIndex - 1;
                }

                _freeList = entriesIndex - 1;
            }
        }

        private bool TryGetValueWorker(TKey key, out TValue value)
        {
            int entryIndex = FindEntry(key);
            if (entryIndex != -1) {
                _entries[entryIndex].depHnd.GetPrimaryAndSecondary(out var primary, out var secondary);
                if (primary is object) {
                    value = secondary;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private void CreateEntry(TKey key, TValue value)
        {
            if (_freeList == -1)
                Resize();

            int hashCode = RuntimeHelpers.GetHashCode(key) & int.MaxValue;
            int bucket = hashCode % _buckets.Length;

            int newEntry = _freeList;
            _freeList = _entries[newEntry].next;

            _entries[newEntry].hashCode = hashCode;
            _entries[newEntry].depHnd = new DependentHandle(key, value);
            _entries[newEntry].next = _buckets[bucket];

            _buckets[bucket] = newEntry;

        }

        private void Resize()
        {
            int newSize = _buckets.Length;

            bool hasExpiredEntries = false;
            int entriesIndex;
            for (entriesIndex = 0; entriesIndex < _entries.Length; entriesIndex++)
                if (_entries[entriesIndex].depHnd.IsAllocated && _entries[entriesIndex].depHnd.GetPrimary() is null) {
                    hasExpiredEntries = true;
                    break;
                }

            if (!hasExpiredEntries)
                newSize = GetPrime(_buckets.Length == 0 ? _initialCapacity + 1 : _buckets.Length * 2);

            int newFreeList = -1;
            int[] newBuckets = new int[newSize];
            for (int bucketIndex = 0; bucketIndex < newSize; bucketIndex++)
                newBuckets[bucketIndex] = -1;
            Entry[] newEntries = new Entry[newSize];

            for (entriesIndex = 0; entriesIndex < _entries.Length; entriesIndex++) {
                DependentHandle depHnd = _entries[entriesIndex].depHnd;
                if (depHnd.IsAllocated && depHnd.GetPrimary() is object) {
                    int bucket = _entries[entriesIndex].hashCode % newSize;
                    newEntries[entriesIndex].depHnd = depHnd;
                    newEntries[entriesIndex].hashCode = _entries[entriesIndex].hashCode;
                    newEntries[entriesIndex].next = newBuckets[bucket];
                    newBuckets[bucket] = entriesIndex;
                } else {
                    _entries[entriesIndex].depHnd.Free();
                    newEntries[entriesIndex].depHnd = new DependentHandle();
                    newEntries[entriesIndex].next = newFreeList;
                    newFreeList = entriesIndex;
                }
            }

            while (entriesIndex != newEntries.Length) {
                newEntries[entriesIndex].depHnd = new DependentHandle();
                newEntries[entriesIndex].next = newFreeList;
                newFreeList = entriesIndex;
                entriesIndex++;
            }

            _buckets = newBuckets;
            _entries = newEntries;
            _freeList = newFreeList;
        }

        private int FindEntry(TKey key)
        {
            int hashCode = RuntimeHelpers.GetHashCode(key) & int.MaxValue;
            for (int entriesIndex = _buckets[hashCode % _buckets.Length]; entriesIndex != -1; entriesIndex = _entries[entriesIndex].next)
                if (_entries[entriesIndex].hashCode == hashCode && ReferenceEquals(_entries[entriesIndex].depHnd.GetPrimary(), key))
                    return entriesIndex;
            return -1;
        }

        private void VerifyIntegrity()
        {
            if (_invalid)
                throw new InvalidOperationException("CollectionCorrupted");
        }

        ~ConditionalWeakTable()
        {
            if (Environment.HasShutdownStarted)
                return;

            if (_lock != null)
                lock (_lock) {
                    if (_invalid)
                        return;
                    Entry[] entries = _entries;

                    _invalid = true;
                    _entries = null;
                    _buckets = null;

                    for (int entriesIndex = 0; entriesIndex < entries.Length; entriesIndex++)
                        entries[entriesIndex].depHnd.Free();
                }
        }

        private static readonly int[] primes = {
            3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
            1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
            17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
            187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
            1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369};

        private static int GetPrime(int min)
        {
            if (min < 0)
                throw new ArgumentException("min");

            for (int i = 0; i < primes.Length; i++) {
                int prime = primes[i];
                if (prime >= min)
                    return prime;
            }

            //outside of our predefined table. 
            //compute the hard way. 
            for (int i = min | 1; i < int.MaxValue; i += 2)
                if (IsPrime(i) && (i - 1) % 101 != 0)
                    return i;
            return min;
        }

        private static bool IsPrime(int candidate)
        {
            if ((candidate & 1) != 0) {
                int limit = (int)Math.Sqrt(candidate);
                for (int divisor = 3; divisor <= limit; divisor += 2)
                    if (candidate % divisor == 0)
                        return false;
                return true;
            }
            return candidate == 2;
        }
    }
}
