using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace ImpliedReflection
{
    internal class Members<TMemberType> : IDictionary<string, TMemberType>, IEnumerable<TMemberType>
        where TMemberType : PSMemberInfo
    {
        private readonly Dictionary<string, PSMemberInfo> _dictionary =
            new Dictionary<string, PSMemberInfo>(StringComparer.OrdinalIgnoreCase);

        internal Members(Dictionary<string, PSMemberInfo> dictionary)
        {
            _dictionary = dictionary;
        }

        public TMemberType this[string key]
        {
            get
            {
                if (_dictionary.TryGetValue(key, out PSMemberInfo existing) &&
                    existing is TMemberType converted)
                {
                    return converted;
                }

                return null;
            }
            set
            {
                if (_dictionary.ContainsKey(key))
                {
                    _dictionary[key] = value;
                    return;
                }

                _dictionary.Add(key, value);
            }
        }

        public ICollection<string> Keys => _dictionary.Keys;

        public ICollection<TMemberType> Values => _dictionary.Values.OfType<TMemberType>().ToArray();

        public int Count => _dictionary.Count;

        public bool IsReadOnly => false;

        public void Add(string key, TMemberType value)
        {
            if (_dictionary.ContainsKey(key))
            {
                _dictionary[key] = value;
                return;
            }

            _dictionary.Add(value.Name, value);
        }

        public void Add(KeyValuePair<string, TMemberType> item) => _dictionary.Add(item.Key, item.Value);

        public void Clear() => _dictionary.Clear();

        public bool Contains(KeyValuePair<string, TMemberType> item)
        {
            return _dictionary.Contains(
                new KeyValuePair<string, PSMemberInfo>(item.Key, item.Value));
        }

        public bool ContainsKey(string key) => _dictionary.ContainsKey(key);

        public void CopyTo(KeyValuePair<string, TMemberType>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        IEnumerator<KeyValuePair<string, TMemberType>> IEnumerable<KeyValuePair<string, TMemberType>>.GetEnumerator()
        {
            return _dictionary
                .Where(kvp => kvp.Value is TMemberType)
                .Select(kvp => new KeyValuePair<string, TMemberType>(kvp.Key, (TMemberType)kvp.Value))
                .GetEnumerator();
        }

        public bool Remove(string key) => _dictionary.Remove(key);

        public bool Remove(KeyValuePair<string, TMemberType> item) => _dictionary.Remove(item.Key);

        public bool TryGetValue(string key, out TMemberType value)
        {
            if (_dictionary.TryGetValue(key, out PSMemberInfo innerValue) &&
                innerValue is TMemberType convertedValue)
            {
                value = convertedValue;
                return true;
            }

            value = default(TMemberType);
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<TMemberType> GetEnumerator()
        {
            return _dictionary.Values.OfType<TMemberType>().GetEnumerator();
        }
    }
}
