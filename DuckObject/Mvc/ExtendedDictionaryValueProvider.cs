using System.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DalSoft.Dynamic.Mvc
{
    /// <summary>
    /// Clone of the DictionaryValueProvider just extended to expose _prefixes, used with DuckObjectModelBinder and JsonDotNetValueProvider.
    /// Also dervied type used as a simple marker to know that JsonDotNetValueProvider was used see DuckObjectModelBinder
    /// </summary>
    public class ExtendedDictionaryValueProvider<TValue> : IValueProvider
    {
        private readonly Dictionary<string, ValueProviderResult> _values = new Dictionary<string, ValueProviderResult>(StringComparer.OrdinalIgnoreCase);
        
        public HashSet<string> Prefixes { get; private set; }
        
        public ExtendedDictionaryValueProvider(ICollection<KeyValuePair<string, TValue>> dictionary, CultureInfo culture)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException("dictionary");
            }
            
            Prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddValues(dictionary, culture);
        }

        
        private void AddValues(ICollection<KeyValuePair<string, TValue>> dictionary, CultureInfo culture)
        {
            if (dictionary == null) throw new ArgumentNullException("dictionary");
            if (dictionary.Count > 0)
            {
                Prefixes.Add("");
            }

            foreach (var entry in dictionary)
            {
                Prefixes.UnionWith(GetPrefixes(entry.Key));

                object rawValue = entry.Value;
                var attemptedValue = Convert.ToString(rawValue, culture);
                _values[entry.Key] = new ValueProviderResult(rawValue, attemptedValue, culture);
            }
        }

        public virtual bool ContainsPrefix(string prefix)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException("prefix");
            }

            return Prefixes.Contains(prefix);
        }

        public virtual ValueProviderResult GetValue(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            ValueProviderResult vpResult;
            _values.TryGetValue(key, out vpResult);
            return vpResult;
        }

        // Given "foo.bar[baz].quux", this method will return:
        // - "foo.bar[baz].quux"
        // - "foo.bar[baz]"
        // - "foo.bar"
        // - "foo"
        private static IEnumerable<string> GetPrefixes(string key)
        {
            yield return key;
            for (var i = key.Length - 1; i >= 0; i--)
            {
                switch (key[i])
                {
                    case '.':
                    case '[':
                        yield return key.Substring(0, i);
                        break;
                }
            }
        }

    }
}


