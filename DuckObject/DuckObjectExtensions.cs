using DalSoft.Dynamic.DynamicExpressions;
using DalSoft.Dynamic.DynamicProxies;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace DalSoft.Dynamic
{
    public static class DuckObjectExtensions
    {
        //Supports property duck typing
        //Supports method extending
        //Doesn't support methods override
        //Doesn't support classes with parameterless ctor
        //Doesn't support Mapping lists as expected using AsIf
        //Doesn't support Extend and Set with dictionaries
        
        /// <summary>Creates a new instance of a class or interface and maps it's property values using the property values of the DuckObject.</summary>
        /// <typeparam name="T">The class or interface you want to create. When passing a class it *must* have a parameterless constructor. Note: interfaces *must* only contain property members
        /// Note: passing dynamic it will just cast the DuckObject to dynamic.</typeparam>
        /// <param name="duckObject">The DuckObject you want to map to T.</param>
        /// <returns>A new instance of T with the property values mapped to the values of the DuckObject.</returns>
        public static T AsIf<T>(this DuckObject duckObject) where T : class
        {
            if (typeof(T) == typeof(object)) //"The typeof operator cannot be used on the dynamic type"
                return (dynamic)duckObject;

            return (T)CreateInstance(duckObject, typeof(T));
        }

        /// <summary>
        /// Maps the property values of a anonymous type using the property values of the DuckObject. The anonymous type's initial values are used defaults.
        /// </summary>
        /// <typeparam name="T">Don't pass let it be infered from the anonymous type parameter.</typeparam>
        /// <param name="duckObject">The DuckObject you want to map to the anonymous type.</param>
        /// <param name="anonymousType">The anonymous type that will be mapped to the DuckObject.</param>
        /// <returns>The anonymous type mapped to the property values of the DuckObject.</returns>
        public static T AsIf<T>(this DuckObject duckObject, T anonymousType) where T : class
        {
            return MapDuckObjectToType(duckObject, anonymousType);
        }

        /// <summary>
        /// Extends the DuckObject with the properties and property values of another class or anonymous type. If the DuckObject already has one or more of the properties you will get an ArgumentException.
        /// </summary>
        /// <typeparam name="T">Don't pass let it be infered from the extendWith parameter.</typeparam>
        /// <param name="duckObject">.</param>
        /// <param name="extendWith">The class or anonymous type that will be used to extend the properties and property values of the DuckObject.</param>
        public static void Extend<T>(this DuckObject duckObject, T extendWith) where T : class
        {
            duckObject.ExtendWithAnonymousType(extendWith); //can easliy been extended to use Dictionary too
        }

        /// <summary>
        /// Extends the DuckObject using a expression. If the DuckObject already has one or more of the properties you will get an ArgumentException.
        /// </summary>
        /// <typeparam name="TValue">Don't pass let it be infered from the expression parameter.</typeparam>
        /// <param name="duckObject">The DuckObject you want to extend</param>
        /// <param name="expression">The expression used to extend the DuckObject</param>
        /// <returns></returns>
        public static DuckObject Extend<TValue>(this DuckObject duckObject, Func<dynamic, TValue> expression)
        {
            var expressionDictionary = expression.DynamicExpressionToDictionary();
            if (expressionDictionary.Values.Last() == null)
                return duckObject;

            var newDictionary = duckObject[expressionDictionary.Keys.First()] as DuckObject ?? new DuckObject(); //Is it already a DuckObject, if it isn't create a DuckObject
            foreach (var key in expressionDictionary.Keys)
            {
                if (key == expressionDictionary.Keys.Last())
                {
                    var previousKey = expressionDictionary.Keys.Skip(expressionDictionary.Count - 2).First();

                    if (newDictionary[previousKey] != null)
                        ((DuckObject)newDictionary[previousKey]).Add(key, expressionDictionary.Values.Last());
                    else
                        duckObject.Add(key, expressionDictionary.Values.Last());
                }
                else
                {
                    newDictionary[key] = new DuckObject();
                }
            }

            if (newDictionary.Any())
                duckObject[expressionDictionary.Keys.First()] = newDictionary;

            return duckObject;
        }

        /// <summary>
        ///  Sets the DuckObject's properties using the property values of an anonymous type. If the DuckObject doesn't have one or more of the properties you are trying to set you will get an ArgumentException.
        /// </summary>
        /// <typeparam name="T">Don't pass let it be infered from the setWith parameter.</typeparam>
        /// <param name="duckObject">The DuckObject who's properties you want to set.</param>
        /// <param name="setWith">The anonymous type who's values will be used to set the DuckObject's properties. If you try to use a class other than an anonymous type you will you will get an ArgumentException.</param>
        public static void Set<T>(this DuckObject duckObject, T setWith) where T : class
        {
            SetWithAnonymousType(duckObject, setWith); //can easliy been extended to use Dictionary too
        }

        public static void Set<TValue>(this DuckObject duckObject, Func<dynamic, TValue> expression)
        {
            throw new NotImplementedException(); //TODO add support for set properties using a expression
        }

        /// <summary>
        /// Given an a object returns a new plain DuckObject based on that object
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DuckObject ToDuckObject<T>(this T value) where T : class
        {
            return new DuckObject(value);
        }

        /// <summary>
        /// Typed Duck object, the T is *important* as it allows for typed DuckObject e.g. where a class inherits from DuckObject
        /// </summary>
        public static TDerivedFromDuckObject ToDuckObject<TDerivedFromDuckObject>(this object value) where TDerivedFromDuckObject : DuckObject, new()
        {
            var derivedDuckObject = new TDerivedFromDuckObject();
            derivedDuckObject.Set(value);
            return derivedDuckObject;
        }


        // Based on http://theburningmonk.com/2011/05/idictionarystring-object-to-expandoobject-extension-method/ thanks
        /// <summary>
        /// Takes a Dictionary object, string and produces duckobject where all nested objects are converted duckobjects to.
        /// Typed Duck object, T is *important* as it allows for typed DuckObject e.g. where a class inherits from DuckObject
        /// </summary>
        public static TDerivedFromDuckObject ToDuckObject<TDerivedFromDuckObject>(this IDictionary<string, object> dictionary) where TDerivedFromDuckObject : DuckObject, new()
        {
            var duckObject = new TDerivedFromDuckObject();
            var duckObjectDictionary = (IDictionary<string, object>)duckObject;

            foreach (var kvp in dictionary)
            {
                if (kvp.Value is IDictionary<string, object>)
                {
                    var duckObjectValue = ((IDictionary<string, object>)kvp.Value).ToDuckObject<TDerivedFromDuckObject>();
                    duckObjectDictionary.Add(kvp.Key, duckObjectValue);
                }

                else if (kvp.Value is ICollection)
                {
                    var itemList = new List<object>();

                    foreach (var item in (ICollection)kvp.Value)
                    {
                        if (item is IDictionary<string, object>)
                        {
                            var duckObjectItem = ((IDictionary<string, object>)item).ToDuckObject<TDerivedFromDuckObject>();
                            itemList.Add(duckObjectItem);
                        }
                        else
                        {
                            itemList.Add(item);
                        }
                    }

                    duckObjectDictionary[kvp.Key] = itemList;
                }
                else
                {
                    duckObjectDictionary[kvp.Key] = kvp.Value;

                }
            }

            return duckObject;
        }

        internal static DuckObject ExtendWithAnonymousType<T>(this DuckObject duckObject, T anonymousType) where T : class
        {
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(anonymousType))
            {
                if (duckObject[property.Name] == null)
                    duckObject.Add(property.Name, property.GetValue(anonymousType));
                else if (IsClassThatWeCanMap(property.PropertyType))
                    duckObject.Add(property.Name, ExtendWithAnonymousType(duckObject[property.Name].ToDuckObject(), property.GetValue(anonymousType)));
                else
                    duckObject.Add(property.Name, duckObject[property.Name]);
            }

            return duckObject;
        }

        internal static void SetPropertyValue(object instance, string propertyName, Type propertyType, object value)
        {
            var instanceType = instance.GetType(); //anonymous types and dynamic proxies don't play nicely with typeof(T)

            if (instanceType.BaseType == typeof(DynamicProxyBase)) return; //Dynamic duck already sets the properties for us.

            if (propertyType != value.GetType()) //try conversion
            {
                value = value is IConvertible ? Convert.ChangeType(value, propertyType) : (propertyType.IsClassThatWeCanMap() ? value.ToDuckObject() : value);
            }

            if (IsAnonymousType(instanceType))
            {
                var fieldInfo = instanceType.GetField(string.Format("<{0}>i__Field", propertyName), BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null) fieldInfo.SetValue(instance, value); //Very naughty
            }
            else
            {
                var property = instanceType.GetProperty(propertyName);
                if (property.CanWrite) property.SetValue(instance, value, null);
            }
        }

        //Create new T and map the duck object to it
        private static T MapDuckObjectToType<T>(IDictionary<string, object> currentDuckObject, T anonymousType) where T : class
        {
            foreach (var property in TypeDescriptor.GetProperties(anonymousType).Cast<PropertyDescriptor>().Where(property => currentDuckObject[property.Name] != null)) //select only properties that have a matching value in the DuckObject
            {
                var valueFromDuckObject = currentDuckObject[property.Name];
                var typeFromDuckObject = currentDuckObject[property.Name].GetType();

                if (IsClassThatWeCanMap(typeFromDuckObject))
                {
                    property.SetValue(anonymousType, IsAnonymousType(property.PropertyType) ? MapDuckObjectToType(valueFromDuckObject as DuckObject ?? valueFromDuckObject.ToDuckObject(), property.GetValue(anonymousType)) : CreateInstance(valueFromDuckObject.ToDuckObject(), property.PropertyType));
                }
                else
                {
                    SetPropertyValue(instance: anonymousType,
                                     propertyName: property.Name,
                                     propertyType: property.PropertyType,
                                     value: valueFromDuckObject);
                }
            }

            return anonymousType;
        }

        private static object CreateInstance(DuckObject duckObject, Type type)
        {
            if (!type.IsInterface && type.GetConstructor(Type.EmptyTypes) == null)
                throw new ArgumentException(string.Format("Can't create the type {0} as it doesn't have a parameterless constructor", type.FullName));

            if (type.IsInterface && type.GetMethods().Any(x => !x.IsSpecialName))
                throw new ArgumentException(string.Format("The interface {0} contains method members AsIf supports interfaces with property members only", type.FullName));

            return MapDuckObjectToType(duckObject, type.IsInterface ? DynamicDuck.AsIf(duckObject, type) : Activator.CreateInstance(type));
        }

        private static void SetWithAnonymousType<T>(object current, T setWith) where T : class
        {
            if (!setWith.GetType().IsAnonymousType()) throw new ArgumentException("setWith can only be used with an anonymous type", "setWith");
            
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(setWith))
            {
                var duckobject = current as DuckObject ?? new DuckObject();
                var propertyInfo = current.GetType().GetProperty(property.Name);
                var currentValue = (propertyInfo!=null ? propertyInfo.GetValue(current, null) : null) ?? duckobject[property.Name];

                if (currentValue == null)
                    throw new ArgumentException(string.Format("The property {0} does not exist", property.Name));

                if (IsClassThatWeCanMap(property.PropertyType))
                {
                    if (!currentValue.GetType().IsClass)
                        throw new ArgumentException(string.Format("Can't set {0} to a reference type as it is a value type", property.Name));

                    SetWithAnonymousType(currentValue, property.GetValue(setWith));
                }
                else
                {
                    SetPropertyValue(current, property.Name, property.PropertyType, property.GetValue(setWith));
                }
            }
        }

        private static bool IsAnonymousType(this Type type)
        {
            return type.Namespace == null;
        }

        private static bool IsValueTypeOrPrimitiveOrStringOrDateTime(this Type type)
        {
            return type.IsValueType || type.IsPrimitive || type == typeof(string) || type == typeof(DateTime);
        }

        private static bool IsClassThatWeCanMap(this Type type) //TODO CHECK BUG IN DUCKOBEJC
        {
            if (IsAnonymousType(type) && !IsValueTypeOrPrimitiveOrStringOrDateTime(type))
                return true;

            return type.IsClass && !IsValueTypeOrPrimitiveOrStringOrDateTime(type);
        }
    }
}
