using System;

namespace DalSoft.Dynamic.DynamicExpressions 
{
    //Thanks and credit to http://www.codeproject.com/Articles/74018/How-to-Parse-and-Convert-a-Delegate-into-an-Expres
	internal static class StringHelper
	{
		/// <summary>
		/// /// Returns a validated string, or throws exceptions to reflect the specific error condition.
		/// </summary>
		/// <param name="str">The string to validate.</param>
		/// <param name="desc">The description of the string to validate.</param>
		/// <param name="canBeNull">If the string can be 'null'.</param>
		/// <param name="trim">Used to request the string is trimmed before returning it.</param>
		/// <param name="canBeEmpty">If the string can be empty.</param>
		/// <param name="minLen">The minimum lenght acceptable for the string.</param>
		/// <param name="maxLen">The maximun lenght acceptable for the string.</param>
		/// <param name="padLeft">Used to left-pad the string (if maxLen is given).</param>
		/// <param name="padRight">Used to right-pad the string (is maxLen if given).</param>
		/// <param name="invalidChars">A string with the invalid chars.</param>
		/// <param name="validChars">A string with the only valid chars.</param>
		/// <returns></returns>
		public static string Validated( this string str, string desc,
			bool canBeNull = false, bool trim = true, bool canBeEmpty = false,
			int minLen = -1, int maxLen = -1,
			char padLeft = '\0', char padRight = '\0',
			string invalidChars = null, string validChars = null
			)
		{
			// Assuring we have a valid parameter's description...
			if( string.IsNullOrWhiteSpace( desc ) ) desc = "String";

			// Validating if the string can be null or not...
			if( str == null ) {
				if( !canBeNull ) throw new ArgumentNullException( desc + " cannot be null." );
				return str;
			}

			// Trimming, and validating if the string can be empty...
			if( trim ) str = str.Trim();
			if( !canBeEmpty && str.Length == 0 ) throw new ArgumentException( desc + " cannot be empty." );

			/// Validating the string's lenght...
			if( minLen >= 0 ) if( str.Length < minLen ) throw new ArgumentOutOfRangeException( desc + " lenght cannot be less than " + minLen + "." );
			if( maxLen >= 0 ) if( str.Length < maxLen ) throw new ArgumentOutOfRangeException( desc + " lenght cannot be bigger than " + maxLen + "." );

			// Padding, if requested...
			if( padLeft != '\0' && maxLen > 0 ) str = str.PadLeft( maxLen, padLeft );
			if( padRight != '\0' && maxLen > 0 ) str = str.PadRight( maxLen, padRight );

			// Validating the characters in the string...
			if( validChars != null ) {
				foreach( char c in str )
					if( !validChars.Contains( c.ToString() ) )
						throw new ArgumentException( "Not allowed character '" + c + "' found in '" + desc + "': '" + str + "'." );
			}
			if( invalidChars != null ) {
				foreach( char c in str ) {
					if( invalidChars.Contains( c.ToString() ) )
						throw new ArgumentException( "Not allowed character '" + c + "' found in '" + desc + "': '" + str + "'." );
				}
			}

			// Returning...
			return str;
		}

		public static bool ContainsAnyCharIn( this string str, string array )
		{
			if( str == null ) throw new NullReferenceException( "This string to check" );
			if( array == null ) throw new ArgumentNullException( "String containing the characters to validate" );
			return str.ContainsAnyCharIn( array.ToCharArray() );
		}
		public static bool ContainsAnyCharIn( this string str, params char[] array )
		{
			if( str == null ) throw new NullReferenceException( "This string to check" );
			if( array == null ) throw new ArgumentNullException( "Array of characters to validate" );
			if( array.Length == 0 ) throw new ArgumentNullException( "Array of characters to validate cannot be empty." );

			foreach( char c in array ) if( str.Contains( c.ToString() ) ) return true;
			return false;
		}
	}
}
