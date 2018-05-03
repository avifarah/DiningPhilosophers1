using System;


namespace StringHandling
{
	/// <summary>
	/// Purpose:
	///		Wrap the string to be evaluated with the following:
	///		>	Use the EnhancedStrPairElement that allows for case insensitive search of the key
	///			in the {key::value} element.
	///		>	Add the IsHandled boolean construct keeping track of the successful evaluation through
	///			the EnhancedStringEval process.
	/// </summary>
	[Serializable]
	public sealed class EnhancedStringEventArgs : EventArgs
	{
		/// <summary>EnhancedStrPairElement that this node is operating on</summary>
		public readonly EnhancedStrPairElement EhancedPairElem;

		/// <summary>Was pattern resolved</summary>
		public bool IsHandled { get; set; }

		public EnhancedStringEventArgs(EnhancedStrPairElement enhancedPairElem)
		{
			EhancedPairElem = enhancedPairElem;
			IsHandled = false;
		}

		public override string ToString()
		{
			return $"EhangedPairElem={EhancedPairElem}, IsHandled={IsHandled}";
		}
	}
}
