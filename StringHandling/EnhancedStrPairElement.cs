using System;
using System.Collections.Generic;

namespace StringHandling
{
	/// <summary>
	/// Purpose:
	///		We would like the identifier to be case insensitive, so we employ a 
	///		Dictionary<string, EnhancedStrPairElement> where the first generic parameter, 
	///		a string, is the identifier of the pair, operated on with the ToUpper() function 
	///		(see for example ProcessKey class).  In this way we ensure that the search for the 
	///		identifier is:
	///			>	case insensitive 
	///			>	we do not loose the original casing (it is still stored within the 
	///				EnhancedStrPairElement--the second generic parameter)
	///			>	and this mechanism is fairly simple to implement and understand
	/// 
	/// </string, EnhancedStrPairElement>
	/// </summary>
	[Serializable]
	public sealed class EnhancedStrPairElement : IEquatable<EnhancedStrPairElement>, IEqualityComparer<EnhancedStrPairElement>
	{
		private string _value;
		private static readonly EnhancedStrPairElement EmptyEnhancedStrElement = new EnhancedStrPairElement(null, null);
		public static EnhancedStrPairElement Empty => EmptyEnhancedStrElement;


		public EnhancedStrPairElement(string identifier, string value)
		{
			Identifier = identifier;
			_value = value;
		}

		public EnhancedStrPairElement(KeyValuePair<string, string> enhancedStrPair)
			: this(enhancedStrPair.Key, enhancedStrPair.Value)
		{
		}

		/// <summary>
		/// Identifier--is immutable so it will fit for for a Dictionary<..> key, or a Hashtable(..) key.
		/// </summary>
		public string Identifier { get; }

		/// <summary>Value may change and does change by the outside world.</summary>
		public string Value
		{
			get { return _value; }
			set
			{
				// Protect against an attempt to change the value of Empty (public static EnhancedStrPairElement Empty...)
				if (Identifier == null)
					throw new EnhancedStringException("Null", value ?? "Null", "Cannot change the value of the Empty EnhancedStrPairElement");
				_value = value;
			}
		}

		public static explicit operator KeyValuePair<string, string>(EnhancedStrPairElement elem)
		{
			if (elem == null) throw new ArgumentException("elem cannot be null", "elem");
			return new KeyValuePair<string, string>(elem.Identifier, elem.Value);
		}

		public static implicit operator EnhancedStrPairElement(KeyValuePair<string, string> elem) => new EnhancedStrPairElement(elem.Key, elem.Value);

		/// <summary>A nice to have only method.</summary>
		/// <returns></returns>
		public KeyValuePair<string, string> ToKeyValuePair() => (KeyValuePair<string, string>)this;

		public override string ToString() => $"({Identifier}, {Value})";

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is EnhancedStrPairElement && Equals((EnhancedStrPairElement)obj);
		}

		public override int GetHashCode() => $"{Value}/{Identifier}".GetHashCode();

		#region IEquatable<EnhancedStrPairElement> Members

		public bool Equals(EnhancedStrPairElement other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return string.Equals(Value, other.Value) && string.Equals(Identifier, other.Identifier);
		}

		#endregion

		#region IEqualityComparer<EnhancedStrPairElement> Members

		public bool Equals(EnhancedStrPairElement lhs, EnhancedStrPairElement rhs) => lhs.Equals(rhs);

		public int GetHashCode(EnhancedStrPairElement obj) => $"{obj.Value}/{obj.Identifier}".GetHashCode();

		#endregion

		public static bool operator ==(EnhancedStrPairElement lhs, EnhancedStrPairElement rhs) => (ReferenceEquals(lhs, null)) ? false : lhs.Equals(rhs);

		public static bool operator !=(EnhancedStrPairElement lhs, EnhancedStrPairElement rhs) => !(lhs == rhs);
	}
}

