using System;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DalSoft.Dynamic.Serialization
{
    internal class DuckObjectConverter<T> : ExpandoObjectConverter where T : DuckObject, new()
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            IDictionary<string, object> result = (ExpandoObject)base.ReadJson(reader, objectType, existingValue, serializer);
            return result.ToDuckObject<T>();
        }
    }
}