using System.Text.RegularExpressions;

namespace StringHandling.ProcessEvaluate
{
	/// <summary>
	/// Process:
	///		Divides integer-value-1 by integer-value-2 and return the trucncated result
	///		returns: truncated-value-of((integer-value-1) / (integer-value-2))
	/// Format:
	///		{%Integer-divide::5::2%}
	/// </summary>
	public sealed class ProcessIntegerDivide : ProcessEvaluateBase
	{
		public ProcessIntegerDivide() : this(DelimitersAndSeparator.DefaultDelimitersAndSeparator) { }

		public ProcessIntegerDivide(IDelimitersAndSeparator delim) : base(delim)
		{
			const RegexOptions reo = RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled;
			//string pattern = @"({%)\s*Integer-divide\s*::\s*(?<dividend>\d+)\s*::\s*(?<divisor>\d+)\s*(%})";
			string pattern = $@"({delim.OpenDelimEquivalent})\s*"
				+ $@"Integer-divide\s*{delim.SeparatorEquivalent}\s*"
				+ $@"(?<dividend>\d+)\s*{delim.SeparatorEquivalent}\s*"
				+ $@"(?<divisor>\d+)\s*"
				+ $@"({delim.CloseDelimEquivalent})";
			RePattern = new Regex(pattern, reo);
		}

		protected override Regex RePattern { get; set; }

		protected override string PatternReplace(Match m, EnhancedStringEventArgs ea)
		{
			string sDividend = m.Groups["dividend"].Value;
			string sDivisor = m.Groups["divisor"].Value;
			var dividend = int.Parse(sDividend);
			var divisor = int.Parse(sDivisor);
			var quotent = dividend / divisor;
			return quotent.ToString();
		}
	}
}
