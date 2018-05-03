using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StringHandling.ProcessEvaluate
{
	/// <summary>
	/// Purpose:
	///		The intended use of this ProcessXxx is to provide an evaluation for key evaluation of a 
	///		configuration file like app.config.
	/// 
	/// Process {%key%}
	/// 
	/// <example>
	///		<code>
	///			// Retrieve all entries of AppSettings of app.config into a dictionary named config
	/// 		var config = ConfigurationManager.AppSettings.AllKeys.ToDictionary(name => name, name => ConfigurationManager.AppSettings[name]);
	/// 
	/// 		var context = new List<IProcessEvaluate> { new ProcessConfigKey(configDictionaryOfKeyValues) };
	/// 		var eval = new EnhancedStringEval(context);
	/// 		string key = /* a key in the configuration file's section: AppSettings */;
	/// 		string test = eval.EvaluateString(config[key]);
	///		</code>
	///		<Assumption>
	///			AppSettings section has no repeating key fields
	///		</Assumption>
	/// </example>
	/// </summary>
	public sealed class ProcessConfigKey : ProcessEvaluateBase
    {
		/// <summary>
		/// Process key could handle the old config entries.
		/// </summary>
		/// <param name="pairs"> </param>
		public ProcessConfigKey(IDictionary<string, string> pairs) : this(pairs, DelimitersAndSeparator.DefaultDelimitersAndSeparator) { }

		public ProcessConfigKey(IDictionary<string, string> pairs, IDelimitersAndSeparator delim) : base(delim)
		{
			if (delim == null) throw new ArgumentException("Delim may not be null", nameof(delim));

			_pairEntries = new Dictionary<string, string>(pairs, StringComparer.CurrentCultureIgnoreCase);

			const RegexOptions reo = RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled;
			//string pattern = @"({%)(?<Name>([^{%%}])*?)(%})";
			string pattern = $@"({delim.OpenDelimEquivalent})"
				+ $@"(?<Name>([^{delim.OpenDelimEquivalent}{delim.CloseDelimEquivalent}])*?)"
				+ $@"({delim.CloseDelimEquivalent})";
			RePattern = new Regex(pattern, reo);

			ResolveKeys();
		}

		private void ResolveKeys()
		{
			var eval = new EnhancedStringEval(new List<IProcessEvaluate> { this }, Delimiter);
			eval.EvaluateStrings(_pairEntries);
		}

		/// <summary>
		/// Keep all entry in a dictionary so that we can evaluate all of them for {Date::value}, {key::value}
		/// or other evaluations like {ForeignKey::path::value}
		/// </summary>
		private readonly Dictionary<string, string> _pairEntries;

		protected override string PatternReplace(Match m, EnhancedStringEventArgs ea)
		{
			string key = m.Groups["Name"].Value;
			//string key = txt.ToUpper();				// Case insensitive key.  ToUpper() is more optimized than ToLower()
			if (!_pairEntries.ContainsKey(key)) return m.ToString();

			string rplcElem = _pairEntries[key];
			if (rplcElem == null) return m.ToString();
			return rplcElem;
		}

		protected override Regex RePattern { get; set; }
	}
}

