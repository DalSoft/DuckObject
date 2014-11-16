using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DalSoft.Dynamic.Extensions
{
    internal static class DictionaryExtensions
    {
        public static void CopyTo(this IDictionary<string, object> current, IDictionary<string, object> destination)
        {
            CopyTo(current, destination, false /* replaceEntries */);
        }

        public static void CopyTo(this IDictionary<string, object> current, IDictionary<string, object> destination, bool replaceEntries)
        {
            if (current == null)
            {
                throw new ArgumentNullException("current");
            }
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }

            foreach (var key in current.Keys.Where(key => replaceEntries || !destination.ContainsKey(key)))
            {
                destination[key] = current[key];
            }
        }
    }

    internal static class HttpFileCollectionExtensions
    {
        public static void CopyTo(this HttpFileCollectionBase current, IDictionary<string, object> destination)
        {
            CopyTo(current, destination, false /* replaceEntries */);
        }

        public static void CopyTo(this HttpFileCollectionBase current, IDictionary<string, object> destination, bool replaceEntries)
        {
            if (current == null)
            {
                throw new ArgumentNullException("current");
            }
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }

            foreach (var key in current.Keys.Cast<string>().Where(key => replaceEntries || !destination.ContainsKey(key)))
            {
                destination[key] = current[key];
            }
        }
    }

}
