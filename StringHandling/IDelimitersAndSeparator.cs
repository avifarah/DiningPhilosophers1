namespace StringHandling
{
	public interface IDelimitersAndSeparator
	{
		/// <summary>Original delimiters and separator as user intended it to be</summary>
		string OpenDelimiter { get; }
		string CloseDelimiter { get; }
		string Separator { get; }

		/// <summary>
		/// A single character to be used in case original delimiters or separator are multi-character and the need
		/// arises to construct a regular expression with "not construct"
		/// </summary>
		string OpenDelimAlternate { get; }
		string CloseDelimAlternate { get; }
		string SeparatorAlternate { get; }

		/// <summary>
		/// If OpenDelimOrig/CloseDelimOrig is a single character then use 
		/// original value or else use OpenDelimAlternate/CloseDelimAlternate
		/// </summary>
		string OpenDelimEquivalent { get; }
		string CloseDelimEquivalent { get; }
		string SeparatorEquivalent { get; }

		/// <summary>
		/// Checks for balanced open-close symbol
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		bool IsBalancedOpenClose(string text);

		/// <summary>
		/// A place for the system to transform the delimiters to their equivalent
		/// counterpart.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		string PreMatch(string text);

		/// <summary>
		/// A place for the system to transform the counterparts back to their
		/// original delimiters.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		string PostMatch(string text);

		/// <summary>Does text contains a simple construct</summary>
		/// <param name="text"></param>
		/// <returns></returns>
		bool IsSimpleExpression(string text);
	}
}