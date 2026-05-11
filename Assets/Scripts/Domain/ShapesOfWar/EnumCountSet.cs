#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ShapesOfWar.Domain
{
    public sealed class EnumCountSet<TEnum> where TEnum : struct, Enum
    {
        private readonly Dictionary<TEnum, int> _counts;

        public EnumCountSet()
            : this(null)
        {
        }

        public EnumCountSet(IReadOnlyDictionary<TEnum, int>? counts)
        {
            _counts = Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .ToDictionary(value => value, _ => 0);

            if (counts == null)
            {
                return;
            }

            foreach (KeyValuePair<TEnum, int> count in counts)
            {
                Set(count.Key, count.Value);
            }
        }

        public int Get(TEnum key)
        {
            return _counts[key];
        }

        public IReadOnlyDictionary<TEnum, int> AsReadOnlyDictionary()
        {
            return new ReadOnlyDictionary<TEnum, int>(new Dictionary<TEnum, int>(_counts));
        }

        internal void Set(TEnum key, int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Counts cannot be negative.");
            }

            _counts[key] = value;
        }

        internal void Add(TEnum key, int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Added counts cannot be negative.");
            }

            Set(key, Get(key) + value);
        }

        internal bool TrySpend(TEnum key, int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Spent counts cannot be negative.");
            }

            if (Get(key) < value)
            {
                return false;
            }

            Set(key, Get(key) - value);
            return true;
        }

        internal void Clear()
        {
            foreach (TEnum key in _counts.Keys.ToList())
            {
                _counts[key] = 0;
            }
        }
    }
}
