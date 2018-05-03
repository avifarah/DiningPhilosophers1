using System.Text.RegularExpressions;

namespace StringHandling
{
	/// <summary>
	/// Purpose:
	///		This class comes to cover some boiler plate code of the client.
	/// 
	/// <remarks>
	///		Strictly speaking you need not use this ProcessEvaluateBase class to write a client.  You could 
	///		write your client class by implementing the IProcessEvaluate interface (the EnhancedStrEval class
	///		in its EvaluateStringPure() method expects an IProcessEvaluate derived class).  The abstract,
	///		ProcessEvaluateBase, class provides a boiler plate code that makes your work writing a ProcessXxx 
	///		class easier. 
	/// </remarks>
	/// </summary>
	public abstract class ProcessEvaluateBase : IProcessEvaluate
	{
		/// <summary>Evaluation pattern to determine of the string should be a replacement candidate</summary>
		protected abstract Regex RePattern { get; set; }

		///  <summary>
		/// 		Replacement pattern itself, embellished with EnhancedStringEventArgs.  Enables the 
		/// 		PatternReplace() method to throw an EnhancedStringException if need be.
		///  
		/// 		The PatternReplace is called by the Evaluate(..) method of this class.
		///  </summary>
		///  <param name="m"></param>
		/// <param name="ea"></param>
		/// <returns></returns>
		protected abstract string PatternReplace(Match m, EnhancedStringEventArgs ea);

		/// <summary>	
		/// .ctor that keeps track of the delimiters.
		/// Keeping track of the delimiter is not an absolute necessity, but having it will potentially avoid
		/// the mistakes of having the ProcessXxx bearing different delimiter than the EnhancedStringEval class.
		/// I decided to keep track of the delimiters.
		/// </summary>
		protected ProcessEvaluateBase() : this(DelimitersAndSeparator.DefaultDelimitersAndSeparator) { }

		protected ProcessEvaluateBase(IDelimitersAndSeparator delim) { Delimiter = delim; }

		/// <summary>
		/// Purpose:
		///		Use this virtual method as well as the PostEvaluate(..) method that allows you to "surgically"
		///		alter the value of the text just before it is pattern matched.
		/// 
		///		if you "surgically" alter the text just before it is pattern matched, then you will need to
		///		alter it back after it is pattern as it was before calling the PreEvaluateBase(..).
		/// 
		/// <remarks>
		///		Be clear that you need to use these methods.  For regular pre/post alterations we do have PreEaluate(..)
		///		and PostEvaluate(..) methods of the EnhancedStringEval class.  This is where we demonstrated having one
		///		kind of delimiters then change them on the fly to a different kind of delimiters.  See method: 
		///		EnhancedStringEvalNoyyyMMdd of EnhancedStringEvaluateTest class.  
		/// 
		///		We also have PreMatch() / PostMatch() of the DelimitersAndSeparator class to alter the delimiters and
		///		separator to their equivalent values.
		/// 
		///		These PreEvaluateBase() / PostEvaluateBase() are special in the fact that they operate on the expression
		///		right before and right after evaluation.  This will allow you, for example, to change only part of the 
		///		expression if need be.
		/// </remarks>
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		protected virtual string PreEvaluateBase(string text)
		{
			return text;
		}

		/// <summary>
		/// The undoing what was done in PreEvaluateBase(..)
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		protected virtual string PostEvaluateBase(string text)
		{
			return text;
		}

		#region IProcessEvaluate Members

		/// <summary>The delimiter of the ProcessXxx (the derived class)</summary>
		public IDelimitersAndSeparator Delimiter { get; protected set; }

		/// <summary>
		///		For the most part boiler plate Evaluate code.
		///	
		///		I expect that for the majority of the cases, this Evaluate() method will remain as 
		///		is and the client will not need to override it.  However, if need be this method 
		///		may be overridden.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="ea"></param>
		public virtual void Evaluate(object src, EnhancedStringEventArgs ea)
		{
			// Initialize return code -- This is the default behavior but is included here for better clarity.
			// The ea.IsHandled indicates if the replaced pattern returned in ea.EhancedPairElem.Value 
			// is the original text (no change) or the new text (changed).  Therefore, default is: No Change.
			ea.IsHandled = false;

			// text is the original string line (in its entirety)
			string text = ea.EhancedPairElem.Value;
			if (string.IsNullOrWhiteSpace(text)) return;

			// Gives the ProcessXxx, derived class, a uniform place to transform the incoming text.  This 
			// transformation takes place right before pattern replace.
			string preText = PreEvaluateBase(text);

			// Does the preText contains the construct that ProcessXxx derived class can handle?
			// If text did not match the pattern, then there is nothing to do!  ea.IsHandled will be false.
			// If so, then there is no need to restore text back, since text is internal to this method.
			bool rc = RePattern.IsMatch(preText);
			if (!rc) return;

			// At this point pattern has been matched, meaning that ProcessXxx derived class can handle the 
			// construct passed through.  If the replace operation yields the same string as the original text, 
			// then return--ea.IsHandled is still false--no change.
			string replacement = RePattern.Replace(preText, m => PatternReplace(m, ea));
			if (replacement == preText) return;

			//
			// At this point the ProcessXxx derived class successfully replaced the construct.
			// Finish up the method ..
			//

			string postText = PostEvaluateBase(replacement);        // Reverse the action of PreEvaluateBase(..)
			ea.EhancedPairElem.Value = postText;                    // Store new, transformed, text
			ea.IsHandled = true;                                    // Store success for examination elsewhere
		}

		#endregion
	}
}
