using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace DalSoft.Dynamic
{
    /// <summary>
    /// Inspired by and credit to http://www.west-wind.com/weblog/posts/2012/Feb/08/Creating-a-dynamic-extensible-C-Expando-Object
    /// </summary>
    public class DuckObject : DynamicObject, IDictionary<string, object>
    {
        private readonly IDictionary<string, object> _properties = new Dictionary<string, object>();
        private readonly Type _instanceType;
        private readonly IEnumerable<PropertyInfo> _reflectionProperties;
        private const BindingFlags ReflectionBindingFlags = BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Instance;

        public DuckObject()
        {
            _instanceType = GetType();
            _reflectionProperties = _instanceType.GetProperties(ReflectionBindingFlags).Where(v => v.Name != "Item" && v.Name != "Keys" && v.Name != "Values");
        }

        public DuckObject(object anonymousType) : this()
        {
            if (!anonymousType.GetType().IsClass)
                throw new ArgumentException("anonymousPrototype must be a reference type", "anonymousType");

            _properties = new DuckObject().ExtendWithAnonymousType(anonymousType);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return TryGetValue(binder.Name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this[binder.Name] = value;
            return true;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            result = this.AsIf(Activator.CreateInstance(binder.Type));
            return true;
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.</returns>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (var property in _reflectionProperties)
            {
                var value = property.GetValue(this, null);
                if (value != null)
                {
                    yield return new KeyValuePair<string, object>(property.Name, value);
                }
            }

            foreach (var key in _properties.Keys)
            {
                yield return new KeyValuePair<string, object>(key, _properties[key]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        public void Add(KeyValuePair<string, object> item)
        {
            var alreadyAddedErrorMessage = string.Format("Can't add the property {0} as it has already been added", item.Key);
            
            if (_reflectionProperties.Any(x => x.Name == item.Key))
            {
                throw new ArgumentException(alreadyAddedErrorMessage);
            }
            
            if (!_properties.ContainsKey(item.Key))
            {
                _properties.Add(item);
            }
            else
            {
                throw new ArgumentException(alreadyAddedErrorMessage);
            }
        }

        /// <summary>Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</summary>
        public void Clear()
        {
            _properties.Clear();
        }

        /// <summary>Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.</summary>
        /// <returns>true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.</returns>
        public bool Contains(KeyValuePair<string, object> item)
        {
             return _reflectionProperties.ToDictionary(k => k.Name, v => v.GetValue(this, null)).Contains(item) || _properties.Contains(item);
        }

        /// <summary>Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.</summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            var properties = from p in _reflectionProperties
                             select new { name = p.Name, value = p.GetValue(this, null) };

            IDictionary<string, object> dictionary = properties
                .ToDictionary(k => k.name, v => v.value)
                .Union(_properties)
                .ToDictionary(k => k.Key, v => v.Value);

            dictionary.CopyTo(array, arrayIndex);
        }

        /// <summary>Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</summary>
        /// <returns>true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.</returns>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        public bool Remove(KeyValuePair<string, object> item)
        {
            return _properties.Remove(item);
        }

        int ICollection<KeyValuePair<string, object>>.Count
        {
            get { return Count(); }
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</summary>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</returns>
        public int Count()
        {
            return _properties.Count + _reflectionProperties.Count();
        }

        /// <summary>Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</summary>
        /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.</returns>
        public bool IsReadOnly()
        {
            return false;
        }

        /// <summary>Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key.</summary>
        /// <returns>true if the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the key; otherwise, false.</returns>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</param>
        public bool ContainsKey(string key)
        {
            return _reflectionProperties.Any(x => x.Name == key) || _properties.ContainsKey(key);
        }

        /// <summary>Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</summary>
        public void Add(string key, object value)
        {
            Add(new KeyValuePair<string, object>(key, value));
        }

        /// <summary>Removes item by key from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</summary>
        /// <returns>true if item was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/> otherwise, false.</returns>
        /// <param name="key">The key to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        public bool Remove(string key)
        {
            return _properties.Remove(key);
        }

        /// <summary>Gets the value associated with the specified key.</summary>
        /// <returns>true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key; otherwise, false.</returns>
        /// <param name="key">The key whose value to get.</param><param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public bool TryGetValue(string key, out object value)
        {
            if (_properties.Keys.Contains(key))
            {
                _properties.TryGetValue(key, out value);
                return true;
            }

            if (_reflectionProperties.Any(x => x.Name == key))
            {
                value = _reflectionProperties.Single(x => x.Name == key).GetValue(this, null);
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>Gets or sets the value associated with the specified key.</summary>
        /// <returns>The value associated with the specified key. If the specified key is not found, a get operation throws a <see cref="T:System.Collections.Generic.KeyNotFoundException"/>, and a set operation creates a new element with the specified key.</returns>
        /// <param name="key">The key of the value to get or set.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception><exception cref="T:System.Collections.Generic.KeyNotFoundException">The property is retrieved and <paramref name="key"/> does not exist in the collection.</exception>
        public object this[string key]
        {
            get
            {
                object value;
                TryGetValue(key, out value);
                return value;
            }
            set
            {
                if (_reflectionProperties.Any(x => x.Name == key))
                {
                    var property = _reflectionProperties.Single(x => x.Name == key);
                    var errorMessage = string.Format("The value {0} provided is type of {1} which can't converted to the property {2} which is type of {3}", value, value.GetType().FullName, property.Name, property.PropertyType.FullName);

                    TryCatch<Exception, ArgumentException>(
                        @try: () => DuckObjectExtensions.SetPropertyValue(instance: this,
                                                                          propertyName: property.Name,
                                                                          propertyType: property.PropertyType,
                                                                          value: value),
                            errorMessage: errorMessage
                     );
                }
                else
                {
                    _properties[key] = value;
                }
            }
        }

        /// <summary>Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</summary>
        /// <returns>An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.</returns>
        public ICollection<string> Keys
        {
            get
            {
                return _reflectionProperties
                    .Select(k => k.Name)
                    .Union(_properties.Select(k => k.Key))
                    .ToList();
            }
        }

        /// <summary>Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</summary>
        /// <returns>An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.</returns>
        public ICollection<object> Values
        {
            get
            {
                return _reflectionProperties
                   .Select(k => k.GetValue(this, null))
                   .Union(_properties.Select(k => k.Value))
                   .ToList();
            }
        }

        private static void TryCatch<TCatch, TThrow>(Action @try, string errorMessage = null)
            where TCatch : Exception
            where TThrow : Exception, new()
        {
            try
            {
                @try();
            }
            catch (TCatch)
            {
                if (errorMessage != null)
                    throw (TThrow)Activator.CreateInstance(typeof(TThrow), new object[] { errorMessage });

                throw new TThrow();
            }
        }
    }

    public class DuckObject<T> : DuckObject where T : class, new()
    {
        public DuckObject() { }
        public DuckObject(object anonymousType) : base(anonymousType) { }

        public static implicit operator T(DuckObject<T> duckObject)
        {
            return duckObject.AsIf<T>();
        }
    }
}
