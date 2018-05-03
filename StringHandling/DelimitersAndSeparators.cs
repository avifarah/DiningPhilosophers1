
namespace StringHandling
{
	using System;
	using System.Linq;
	using System.Text.RegularExpressions;

	/// <summary>
	/// Purpose:
	///		Encapsulate the handling of the delimiters and separator.
	///		Delimiters: OpenDelimiter, CloseDelimiter.
	/// 
	/// For all practical purposes this is an immutable class
	///		Once created the OpenDelimiter, CloseDelimiter and Separator cannot be changed.
	/// 
	/// <remarks>
	/// The original constructs in mind for which we need this, DelimitersAndSeparator, are:
	///		{%value%}
	///		{%id::value%}							// Where ID is thought of as an operator
	///		{%id:value1::value2%}
	///		{%id:vale1::value2::value3}	etc...
	/// 
	/// In the above the "{%" and "%}" are the open and close delimiters, respectively, and the "::" 
	/// is the separator.  These are the default delimiters and separator.  You may change those values
	/// via this class.
	/// </remarks>
	/// </summary>
	public sealed class DelimitersAndSeparator : IDelimitersAndSeparator, IEquatable<IDelimitersAndSeparator>
	{
		/// <summary>Default Open/Close and separator delimiters</summary>
		private const string CDefaultOpen = "{%";
		private const string CDefaultClose = "%}";
		private const string CDefaultSeparator = "::";

		/// <summary>
		/// Alternate, single character, for original open/close and separator delimiters.
		/// 
		/// The consumer of this engine, in our example are ProcessXxx classes, need, at times, to
		/// match against not-having-the-delimiter construct.  The xxxAlternate comes to provide
		/// capability to do that and use the regular expression construct for it.
		/// 
		/// The regular expression machinery contains the caret operator (the NOT within the "[]" 
		/// construct like so: the "[^abc]" pattern will match on any character other than 'a', 
		/// 'b' or 'c') which works for single characters only.  So, if we are looking for any 
		/// string not containing "::" we can search for a regular expression that is one of the 
		/// following patterns:
		///		"([^:]+?:)*"	-- a single colon character preceded by a non colon, or
		///		"(:[^:]*)*"		-- a colon character followed by a non-colon 
		///		"[^:]*"			-- no colons in input string altogether
		/// 
		/// This is a lot of work.  Now imagine searching for a pattern needing to be
		/// NOT A STRING OF LENGTH GREATER THAN 2.  The complication using the above 
		/// "smart methodology" is prohibitive.  
		/// 
		/// An alternative, to the above, is to first replace the pattern "::" within
		/// the input string with a single character that does not appear throughout 
		/// the rest of the input string, say "\u0001" then look for "[^\u001]*". 
		/// Then replace the "\u001", in the output string, back with "::".  This
		/// replace methodology is not string length dependent.  Therefore, could be
		/// preferred as a genral purpose method. 
		/// </summary>
		private const string COpenAlternate = "\u0001";
		private const string CCloseAlternate = "\u0002";
		private const string CSeparatorAlternate = "\u0003";

		/// <summary>
		/// Since its an immutable class there is no need to warry that it will be modified externally.
		/// </summary>
		public static readonly IDelimitersAndSeparator DefaultDelimitersAndSeparator = new DelimitersAndSeparator();

		/// <summary>
		/// Evaluating if an expression is "simple" is entirely in the syntax of the delimiters and separator.
		/// An expression is simple if it has:
		///		-	Open delimiter
		///		-	Non-empty identifier
		///		-	Separator
		///		-	optional (value separator) x N 
		///		-	Close delimiter
		///		Moreover, the expression does not contain a nested expressions.
		/// </summary>
		private readonly Regex _smplExprEvaluator;

		/// <summary>
		/// .ctor
		/// </summary>
		/// <param name="openDelim"></param>
		/// <param name="closeDelim"></param>
		/// <param name="separator"></param>
		public DelimitersAndSeparator(string openDelim = CDefaultOpen, string closeDelim = CDefaultClose, string separator = CDefaultSeparator)
		{
			// Validator will throw an exception if the open/close delimiters or the separators do not meet 
			// minimum standard
			Validator(openDelim, closeDelim, separator);

			OpenDelimiter = openDelim;
			CloseDelimiter = closeDelim;

			string balanceOpenClose = BalancePattern;
			BalancedEvaluator = new Regex(balanceOpenClose, RegexOptions.Singleline);

			Separator = separator;

			//
			// Looking for a simple construct.  A construct not containing other constructs.
			// A construct, in our vernacular, is: {key::value}
			//		{%		-	The open delimiter (may be another string than "{%")
			//		Key		-	Is the identifier of the construct
			//		::		-	The separator (may be another string than "::")
			//		Value	-	Is the "instruction" on "how" to evaluate the key.  The value may contain other separators and other keys...
			//		%}		-	Closing delimiter (may be anohter string than "%}")
			//
			// The following pattern looks for a construct which neither the key, nor the value contain delimiters, makes it
			// a simple construct.
			//
			// For C# prior to v 6 use:
			//string pattern = string.Format($@"({OpenDelimEquivalent})(?<reSmplExpr>[^{OpenDelimEquivalent}{CloseDelimEquivalent}]+({SeparatorEquivalent})[^{OpenDelimEquivalent}{CloseDelimEquivalent}]*?)({CloseDelimEquivalent})");

			//string pattern = @"({%)(?<SmplExpr>[^{%%}]+(::)[^{%%}]*?)(%})";
			string pattern = $@"({OpenDelimEquivalent})"
				+ $@"(?<reSmplExpr>[^{OpenDelimEquivalent}{CloseDelimEquivalent}]+({SeparatorEquivalent})[^{OpenDelimEquivalent}{CloseDelimEquivalent}]*?)"
				+ $@"({CloseDelimEquivalent})";
			_smplExprEvaluator = new Regex(pattern, RegexOptions.Singleline);
		}

		private Regex BalancedEvaluator { get; }

		/// <summary>
		/// Note: xxxDelimEquivalent (OpenDelimEquivalent/CloseDelimEquivalent/SeparatorEquivalent) are 
		/// single character in length.
		/// 
		/// <remarks>
		/// For better explanation of the BalancePattern, Search the msdn.com site for 
		/// "regular expression, Grouping Constructs" then within the page "Grouping Constructs 
		/// in Regular Expressions" find "Balancing Group Definitions".  The key to this
		/// pattern is the "?<open-close>" regular expression pattern.
		/// </open-close>
		/// </remarks>
		/// 
		/// For C# versions less than 6:
		/// return string.Format(@"^[^{0}{1}]*(((?<Open>{0})[^{0}{1}]*)+((?<Close-Open>({1}))[^{0}{1}]*)+)*(?(Open)(?!))$", OpenDelimEquivalent, CloseDelimEquivalent);
		/// </Close-Open></Open>
		/// 
		/// return @"^[^{}]*(((?<Open>{)[^{}]*)+((?<Close-Open>(}))[^{}]*)+)*(?(Open)(?!))$";
		/// </Close-Open></Open>
		/// </summary>
		private string BalancePattern => $@"^[^{OpenDelimEquivalent}{CloseDelimEquivalent}]*"
										 + $@"(((?<Open>{OpenDelimEquivalent})[^{OpenDelimEquivalent}{CloseDelimEquivalent}]*)+"
										 + $@"((?<Close-Open>({CloseDelimEquivalent}))[^{OpenDelimEquivalent}{CloseDelimEquivalent}]*)+)*"
										 + $@"(?(Open)(?!))$";

		private static bool IsReSpecialChar(char inC)
		{
			// These characters have special meaning for the regular expression.  If they appear in the open
			// delimiter, close delimiter or separator then a substitution is warranted.
			// 
			// A noticeable omission from the list of regular-expression special characters are the "{" and "}".
			// The open/close braces do not need escaping to be searched for therefore there is no need to include
			// them in the list of special characters.
			const string reSpecialCharacters = @".$^[](|)*+?\";
			bool rc = reSpecialCharacters.FirstOrDefault(c => c == inC) != '\0';
			return rc;
		}

		#region IDelimitersAndSeparator Members

		public string OpenDelimiter { get; }

		public string OpenDelimAlternate { get { return COpenAlternate; } }

		public string OpenDelimEquivalent
		{
			get
			{
				if (OpenDelimiter.Length == 1)
					if (!IsReSpecialChar(OpenDelimiter[0]))
						return OpenDelimiter;
				return COpenAlternate;
			}
		}

		public string CloseDelimiter { get; }

		public string CloseDelimAlternate => CCloseAlternate;

		public string CloseDelimEquivalent
		{
			get
			{
				if (CloseDelimiter.Length == 1)
					if (!IsReSpecialChar(CloseDelimiter[0]))
						return CloseDelimiter;
				return CCloseAlternate;
			}
		}

		public string Separator { get; }

		public string SeparatorAlternate => CSeparatorAlternate;

		public string SeparatorEquivalent
		{
			get
			{
				if (Separator.Length == 1)
					if (!IsReSpecialChar(Separator[0]))
						return Separator;
				return CSeparatorAlternate;
			}
		}

		public bool IsBalancedOpenClose(string text)
		{
			if (OpenDelimEquivalent == CloseDelimEquivalent) return true;
			if (text == null) return true;
			string preText = PreMatch(text);
			Match m = BalancedEvaluator.Match(preText);
			// No need to revert back the preText string with PostMatch(..) since we are not going to use its result.
			return m.Success;
		}

		/// <summary>
		/// Transform multi-char delimiters to single-char delimiters
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public string PreMatch(string text)
		{
			if (text == null) return null;
			string pre1 = (OpenDelimiter.Length > 1 || IsReSpecialChar(OpenDelimiter[0])) ? text.Replace(OpenDelimiter, COpenAlternate) : text;
			string pre2 = (CloseDelimiter.Length > 1 || IsReSpecialChar(CloseDelimiter[0])) ? pre1.Replace(CloseDelimiter, CCloseAlternate) : pre1;
			string pre3 = (Separator.Length > 1 || IsReSpecialChar(Separator[0])) ? pre2.Replace(Separator, CSeparatorAlternate) : pre2;
			return pre3;
		}

		/// <summary>
		/// Transform back to the original delimiters
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public string PostMatch(string text)
		{
			if (text == null) return null;
			string post1 = (OpenDelimiter.Length > 1 || IsReSpecialChar(OpenDelimiter[0])) ? text.Replace(CCloseAlternate, CloseDelimiter) : text;
			string post2 = (CloseDelimiter.Length > 1 || IsReSpecialChar(CloseDelimiter[0])) ? post1.Replace(COpenAlternate, OpenDelimiter) : post1;
			string post3 = (Separator.Length > 1 || IsReSpecialChar(Separator[0])) ? post2.Replace(CSeparatorAlternate, Separator) : post2;
			return post3;
		}

		/// <summary>
		/// Purpose:
		///		Determines if the text contains a simple express.
		///	
		/// Comment:
		///		Simple expression has no inner expressions.  Therefore, {key::Abc} is a simple 
		///		expression, while {key::Abc_{key::ip address}} is not a simple expression.  
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public bool IsSimpleExpression(string text)
		{
			if (text == null) return true;
			string preText = PreMatch(text);
			Match m = _smplExprEvaluator.Match(preText);
			// No need to revert back the preText string with PostMatch(..) since we are not going to use its result.
			return m.Success;
		}

		#endregion

		/// <summary>
		/// Purpose:
		///		Validate open/close delimiters.
		///		*	They are not allowed to be ws (white space)
		/// 
		///		Validate separator.
		///		*	It may not be ws (white space).
		///		*	It may equal neither the open nor close delimiters
		/// 
		/// There is no restriction against an open-delimiter == close-delimiter.
		/// </summary>
		/// <param name="openDelim"></param>
		/// <param name="closeDelim"></param>
		/// <param name="separator"></param>
		private static void Validator(string openDelim, string closeDelim, string separator)
		{
			// If you use earlier version of .Net 4.0 (that does not support string.IsNullOrWhiteSpace(..)) you may use the construct:
			//		string.IsNullOrEmpty(..)  
			// or the more equivalent version:
			//		string.IsNullOrEmpty(openDelim) || openDelim.trim().Length == 0
			if (string.IsNullOrWhiteSpace(openDelim))
				throw new EnhancedStringException(null, EnhancedStrPairElement.Empty, "Open delimiter cannot be null, empty or white-space");

			if (string.IsNullOrWhiteSpace(closeDelim))
				throw new EnhancedStringException(null, EnhancedStrPairElement.Empty, "Close delimiter cannot be null, empty or white-space");

			if (string.IsNullOrWhiteSpace(separator))
				throw new EnhancedStringException(null, EnhancedStrPairElement.Empty, "Separator cannot be null, empty or white-space");

			if (string.Compare(separator, openDelim, StringComparison.CurrentCultureIgnoreCase) == 0)
				throw new EnhancedStringException(null, EnhancedStrPairElement.Empty, "Separator cannot equal open-delimiter");

			if (string.Compare(separator, closeDelim, StringComparison.CurrentCultureIgnoreCase) == 0)
				throw new EnhancedStringException(null, EnhancedStrPairElement.Empty, "Separator cannot equal close-delimiter");
		}

		#region IEquatable<IDelimitersAndSeparator> Members

		public bool Equals(IDelimitersAndSeparator other)
		{
			// Case matters
			if (string.Compare(OpenDelimiter, other.OpenDelimiter, StringComparison.Ordinal) != 0) return false;
			if (string.Compare(CloseDelimiter, other.CloseDelimiter, StringComparison.Ordinal) != 0) return false;
			if (string.Compare(Separator, other.Separator, StringComparison.Ordinal) != 0) return false;
			return true;
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(obj, null)) return false;
			if (ReferenceEquals(this, obj)) return true;

			var iOther = obj as IDelimitersAndSeparator;
			if (ReferenceEquals(iOther, null)) return false;

			return Equals(iOther);
		}

		public override int GetHashCode() { return ToString().GetHashCode(); }

		public static bool operator ==(DelimitersAndSeparator lhs, IDelimitersAndSeparator rhs)
		{
			if (ReferenceEquals(lhs, null)) return false;
			return lhs.Equals(rhs);
		}

		public static bool operator !=(DelimitersAndSeparator lhs, IDelimitersAndSeparator rhs) { return !(lhs == rhs); }

		public override string ToString() { return $"(\"{OpenDelimiter}\", \"{CloseDelimiter}\", \"{Separator}\")"; }

#if TEST    // TEST is defined in the DEBUG mode only

		//
		// For testing only
		//
		public static bool IsReSpecialCharTestHelper(char c)
		{
			return IsReSpecialChar(c);
		}

#endif

	}
}

