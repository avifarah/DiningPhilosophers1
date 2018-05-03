using System;
using System.Collections.Generic;
using System.Text;

namespace StringHandling
{
	public interface IProcessEvaluate
	{
		/// <summary>A necessary part of the ProcessEvaluate</summary>
		IDelimitersAndSeparator Delimiter { get; }

		/// <summary>
		/// Signature of a standard event driven delegate
		/// This method is used in the ProcessXxx classes
		/// </summary>
		/// <param name="src"></param>
		/// <param name="ea"></param>
		void Evaluate(object src, EnhancedStringEventArgs ea);
	}
}
