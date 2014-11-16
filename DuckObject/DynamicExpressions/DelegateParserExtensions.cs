using System;
using System.Collections.Generic;
using System.Linq;

namespace DalSoft.Dynamic.DynamicExpressions
{
    public static class DelegateParserExtensions
    {
        /// <summary>an expression tree cannot contain an assignment operator</summary>
        public static Dictionary<string, object> DynamicExpressionToDictionary<TValue>(this Func<dynamic, TValue> expression)
        {
            object nodeValue = null;
            var delegateNode = DelegateParser.Parse(expression);
            if (delegateNode as DelegateNode.MemberSet != null)
                nodeValue = ((DelegateNode.MemberSet)delegateNode).NodeValue;
            
            var dictionary = expression.DynamicExpressionToString().Split(".".ToArray()).Select(x=> new { key=x, value = default(object) }).ToDictionary(x=>x.key, x=> x.value);
            dictionary[dictionary.Keys.Last()] = nodeValue;
            return dictionary;
        }

        /// <summary>an expression tree cannot contain an assignment operator</summary>
        public static string DynamicExpressionToString<TValue>(this Func<dynamic, TValue> expression)
        {
            var delegateNode = DelegateParser.Parse(expression);
            var nodeName = string.Empty;
            
            while (delegateNode != null)
            {
                nodeName = delegateNode.NodeName + "." + nodeName;
                delegateNode = delegateNode.NodeParent;
            }

            nodeName = nodeName.Replace("T0.", string.Empty);
            nodeName = nodeName.TrimEnd(".".ToCharArray());

            return nodeName;
        }
    }
}
