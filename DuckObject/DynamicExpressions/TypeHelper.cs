using System;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;

namespace DalSoft.Dynamic.DynamicExpressions 
{
    //Thanks and credit to http://www.codeproject.com/Articles/74018/How-to-Parse-and-Convert-a-Delegate-into-an-Expres
    internal static class TypeHelper
	{
		public const string InvalidMultiPartNameChars = " +-*/^[]{}!\"\\%&()=?";
		public const string InvalidMemberNameChars = "." + InvalidMultiPartNameChars;

		/// <summary>
		/// Indicates whether the type given is a nullable type or not.
		/// </summary>
		/// <param name="type">The Type to validate.</param>
		/// <returns>If the references to objects of this type can be null or not.</returns>
		public static bool IsNullableType( Type type )
		{
			if( type == null ) throw new ArgumentNullException( "Type" );
			if( type.IsValueType ) return false;
			if( type.IsClass ) return true;

			Type generic = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
			bool r = generic == null ? false : generic.Equals( typeof( Nullable<> ) );
			return r;
		}
		public static bool IsNullableType<T>()
		{
			return IsNullableType( typeof( T ) );
		}

		#region
		internal static Delegate _CreateConverterDelegate( Type sourceType, Type targetType )
		{
			// Following is a variation of code courtesy of Richard Deeming...
			var input = Expression.Parameter( sourceType, "input" );
			
			Expression body;
			try { body = Expression.Convert( input, targetType ); }
			catch( InvalidOperationException ) {
				var conversionType = Expression.Constant( targetType );
				body = Expression.Call( typeof( Convert ), "ChangeType", null, input, conversionType );
			}
			var result = Expression.Lambda( body, input );
			return result.Compile();
		}
		#endregion
		public static object ConvertTo( object source, Type targetType )
		{
			// Following code courtesy of Richard Deeming, answering an original article posted by MBarbaC.
			if( targetType == null ) throw new ArgumentNullException( "Target Type" );
			Type sourceType = ( source == null ) ? typeof( object ) : source.GetType();
			Delegate converter = _CreateConverterDelegate( sourceType, targetType );
			return converter.DynamicInvoke( source );
		}
		public static T ConvertTo<T>( object source )
		{
			T target = (T)ConvertTo( source, typeof( T ) );
			return target;
		}

		public static string GetMemberName<T>( Expression<Func<T, object>> exp, bool raise = true )
		{
			if( exp == null ) throw new ArgumentNullException( "Member access expression" );
			Type type = typeof( T );

			var member = exp.Body as MemberExpression; if( member != null ) return member.Member.Name;

			var unary = exp.Body as UnaryExpression; if( unary != null ) {
				MemberExpression temp = unary.Operand as MemberExpression;
				if( temp != null ) return temp.Member.Name;
			}

			if( raise ) throw new ArgumentException( "Invalid member access expression '" + exp.ToString() + "' for type '" + type + "'." );
			return null;
		}

		#region
		static BindingFlags _memberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
		#endregion

		public static MemberInfo GetMemberInfo( Type declaringType, string memberName, bool raise = true )
		{
			if( declaringType == null ) throw new ArgumentNullException( "Declaring Type" );
			memberName = memberName.Validated( "Member name", invalidChars: InvalidMemberNameChars );

			MemberInfo info = null;

			// Trying properties...
			info = declaringType.GetProperty( memberName, _memberFlags );
			if( info != null ) return info;

			// Trying fields...
			info = declaringType.GetField( memberName, _memberFlags );
			if( info != null ) return info;

			// Not found...
			if( raise ) throw new ArgumentException( "Member '" + memberName + "' not found in type '" + declaringType + "'." );
			return null;
		}
		public static MemberInfo GetMemberInfo<T>( string memberName, bool raise = true )
		{
			return TypeHelper.GetMemberInfo( typeof( T ), memberName, raise );
		}

		public static Type GetMemberType( Type declaringType, string memberName, bool raise = true )
		{
			if( declaringType == null ) throw new ArgumentNullException( "Declaring Type" );
			memberName = memberName.Validated( "Member name", invalidChars: InvalidMemberNameChars );

			PropertyInfo pinfo = declaringType.GetProperty( memberName, _memberFlags );
			if( pinfo != null ) return pinfo.PropertyType;

			FieldInfo finfo = declaringType.GetField( memberName, _memberFlags );
			if( finfo != null ) return finfo.FieldType;

			if( raise ) throw new ArgumentException( "Cannot find member '" + memberName + "' for type '" + declaringType + "'." );
			return null;
		}
		public static Type GetMemberType<T>( string memberName, bool raise = true )
		{
			return GetMemberType( typeof( T ), memberName, raise );
		}

		public static object GetMemberValue( object obj, MemberInfo info )
		{
			if( info == null ) throw new ArgumentNullException( "MemberInfo" );

			switch( info.MemberType ) {
				case MemberTypes.Property: return ( (PropertyInfo)info ).GetValue( obj, null );
				case MemberTypes.Field: return ( (FieldInfo)info ).GetValue( obj );
			}
			throw new InvalidOperationException( "Member '" + info.Name + "' is not a property or a field." );
		}
		public static object GetMemberValue<T>( T obj, string memberName )
		{
			MemberInfo info = GetMemberInfo<T>( memberName, raise: true );
			return TypeHelper.GetMemberValue( obj, info );
		}

		public static void SetMemberValue( object obj, MemberInfo info, object val )
		{
			if( info == null ) throw new ArgumentNullException( "MemberInfo" );

			switch( info.MemberType ) {
				case MemberTypes.Property: ( (PropertyInfo)info ).SetValue( obj, val, null ); return;
				case MemberTypes.Field: ( (FieldInfo)info ).SetValue( obj, val ); return;
			}
			throw new InvalidOperationException( "Member '" + info.Name + "' is not a property or a field." );
		}
		public static void SetMemberValue<T>( T obj, string memberName, object val )
		{
			MemberInfo info = GetMemberInfo<T>( memberName, raise: true );
			TypeHelper.SetMemberValue( obj, info, val );
		}

		
		public static MethodInfo GetMethodInfo( Type declaringType, string methodName, bool raise = true )
		{
			if( declaringType == null ) throw new ArgumentNullException( "Declaring Type" );
			methodName = methodName.Validated( "Member name", invalidChars: InvalidMemberNameChars );

			MethodInfo info = declaringType.GetMethod( methodName, _memberFlags );
			if( info == null && raise ) throw new ArgumentException( "Method: '" + methodName + "' not found in type '" + declaringType + "'." );
			return info;
		}
		public static MethodInfo GetMethodInfo<T>( string methodName, bool raise = true )
		{
			return GetMethodInfo( typeof( T ), methodName, raise );
		}

		public static object InvokeMethod( Type declaringType, string methodName, object target, params object[] args )
		{
			MethodInfo info = GetMethodInfo( declaringType, methodName, raise: true );
			object obj = info.Invoke( target, args );
			return obj;
		}
		public static object InvokeMethod<T>( string methodName, object target, params object[] args )
		{
			MethodInfo info = GetMethodInfo<T>( methodName, raise: true );
			object obj = info.Invoke( target, args );
			return obj;
		}

		public static string ObjectStateToString( object obj )
		{
			if( obj == null ) throw new ArgumentNullException( "Object reference" );
			Type type = obj.GetType();

			MemberInfo[] members = type.GetMembers( _memberFlags );
			StringBuilder sb = new StringBuilder();
			bool first = true;
			
			foreach( MemberInfo member in members ) {
				if( member.MemberType == MemberTypes.Field || member.MemberType == MemberTypes.Property ) {
					if( member.Name[ 0 ] == '<' ) continue;
					object value = TypeHelper.GetMemberValue( obj, member );
					if( !first ) sb.Append( ", " ); else first = false;
					sb.AppendFormat( "{0}:'{1}'", member.Name, value == null ? "[null]" : value.ToString() );
				}
			}
			return sb.ToString();
		}
	}
}
