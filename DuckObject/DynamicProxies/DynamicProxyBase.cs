using System.Dynamic;

//Thanks and credit to http://weblogs.asp.net/britchie/archive/2010/08/03/dynamicduck-duck-typing-in-a-dynamic-world.aspx
namespace DalSoft.Dynamic.DynamicProxies
{
    /// <summary>
    /// Base class for dynamic objects.
    /// </summary>
    public abstract class DynamicProxyBase
    {
        /// <summary>
        /// Target object.
        /// </summary>
        private object _target;

        /// <summary>
        /// Creates a new dynamic object wrapping the specified <paramref name="target">target object</paramref>.
        /// </summary>
        /// <param name="target">Wrapped target object.</param>
        protected DynamicProxyBase(object target)
        {
            this._target = target;
        }

        /// <summary>
        /// Determines whether the wrapped object refers to the same object as the object wrapped by the passed in dynamic object.
        /// </summary>
        /// <param name="obj">Object to compare to.</param>
        /// <returns>true if the wrapped object refers to the same object as the object wrapped by the passed in dynamic object; false otherwise.</returns>
        /// <remarks>Equality between a dynamic wrapped object and a non-wrapped object object always returns false as we can't guarantee commutativity.</remarks>
        public override bool Equals(object obj)
        {
            var dynamic = obj as DynamicProxyBase;

            if (dynamic == null)
                return false;

            return object.ReferenceEquals(dynamic._target, this._target);
        }

        /// <summary>
        /// Returns the hash code of the wrapped object.
        /// </summary>
        /// <returns>Hash code of the wrapped object.</returns>
        public override int GetHashCode()
        {
            return _target.GetHashCode();
        }

        /// <summary>
        /// Returns the string representation of the wrapped object.
        /// </summary>
        /// <returns>String representation of the wrapped object.</returns>
        public override string ToString()
        {
            return _target.ToString();
        }

        
        public object GetProperty(string member)
        {
            if (_target is System.Collections.Generic.IDictionary<string, object>)
                return (_target as System.Collections.Generic.IDictionary<string, object>)[member];
            else
            {
                object result;
                (_target as DynamicObject).TryGetMember(new GetMemberBinderSimple(member), out result);
                return result;
            }
        }

        public void SetProperty(string member, object val)
        {
            if (_target is System.Collections.Generic.IDictionary<string, object>)
                (_target as System.Collections.Generic.IDictionary<string, object>)[member] = val;
            else
                (_target as DynamicObject).TrySetMember(new SetMemberBinderSimple(member), val);
        }

        private class GetMemberBinderSimple : GetMemberBinder
        {
            public GetMemberBinderSimple(string name) : base(name, false) { }
            public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
            {
                return null;
            }
        }

        private class SetMemberBinderSimple : SetMemberBinder
        {
            public SetMemberBinderSimple(string name) : base(name, false) { }
            public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
            {
                return null;
            }
        }

    }
}