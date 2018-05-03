using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace StringHandling
{
	/// <summary>
	/// Purpose:
	///		Provide a vehicle that will transform a substring like {identifier::value} according 
	///		to the rules set forth in the appropriate identifier's Process class.  (In the 
	///		article's explanation, this process class is referred to as ProcessXxx, which is an 
	///		IProcessEvaluate derived class.)
	/// <remarks>
	///		The algorithm is quite simple:
	///			>	Loop through as follows
	///				>	Identify the next simple expression
	///				>	Run through all the ProcessXxx evaluators
	///				>	If none of the ProcessXxx can replace the simple expression then stop looping.
	/// 
	///		Initialization:
	///			Add all the ProcessXxx evaluate methods to the collection of handlers.
	/// </remarks>
	/// </summary>
	public class EnhancedStringEval : IDisposable
	{
		/// <summary>
		/// Preventing infinite looping in the evaluator:
		///		This limit equals to the depth of the nested constructs so the following expression:
		///			{id1::{id2::value}} 
		///		will require a 2 passes through the evaluation.  The PassThroughUpperLimit is a 
		///		"ridiculously" large number.  I am fairly confident that reaching it is equivalent
		///		to an infinite loop.  A more important question is: Is it logically possible to achieve
		///		a condition through which we have an infinite loop?  Thus far I cannot come up with such
		///		a condition.  Nevertheless, I keep the limit, it does not hurt.
		/// </summary>
		private const int PassThroughUpperLimit = 1000;

		private const string Tempkey = "*** Temporary string element Key that is not likely to clash with another StringElement key !!!";
		private readonly IDelimitersAndSeparator _delim;

		/// <summary>
		/// The evaluation context pointing to the various ProcessXxx.Evaluate() routines.
		/// </summary>
		public event EventHandler<EnhancedStringEventArgs> OnEvaluateContext;

		public EnhancedStringEval() : this(null, DelimitersAndSeparator.DefaultDelimitersAndSeparator) { }

		public EnhancedStringEval(IEnumerable<IProcessEvaluate> context) : this(context, DelimitersAndSeparator.DefaultDelimitersAndSeparator) { }

		public EnhancedStringEval(IEnumerable<IProcessEvaluate> context, IDelimitersAndSeparator delim)
		{
			if (delim == null) throw new ArgumentException("Delimiter may not be null", nameof(delim));

			// Keep the delimiter-and-separator and if one of the ProcessXxx do not conform then report it as an error
			_delim = delim;

			//
			// The heart of the initialization
			//
			AddContext(context);
		}

		/// <summary>
		/// Purpose:
		///		Add the Evaluate method of the processXxx classes (IProcessEvaluate derived classes) 
		///		to the OnEvaluateContext handler.
		/// <remarks>
		/// 	It is possible to have a null context in the constructor and add a collection of 
		///		ProcessXxx to the mix before the call to EvaluateString(..)
		///
		///		This also implies that the calling program, the client, can remove and/or add ProcessXxx 
		///		to the collection between successive calls the EvaluateString(..)
		/// </remarks>
		/// </summary>
		/// <param name="context"></param>
		public void AddContext(IEnumerable<IProcessEvaluate> context)
		{
			if (context == null) return;

			foreach (IProcessEvaluate processXxx in context)
			{
				// Error checking
				if (processXxx == null) throw new ArgumentException("Error:  context contains a null process", nameof(context));
				if (processXxx.Delimiter == null) throw new ArgumentException("One of the processes in the context contained a null delimiter", nameof(context));

				// Caution: One may get the urge to use the !=, not equal operator, part of the DelimitersAndSeparator class
				// However doing so will necessitate:
				//		>	processXxx can no longer be an IProcessEvaluate
				//		>	IProcessEvaluate needs to change and have Delimiter refer to DelimiterAndSeparator as opposed to IDelimiterAndSeparator
				//			(the class as opposed to the interface)
				//		>	Add a check here to see if the processXxx.Delimiter is in fact a DelimiterAndSeparator
				if (!processXxx.Delimiter.Equals(_delim))
					throw new ArgumentException($"{processXxx.GetType()} delimiter: {processXxx.Delimiter} is invalid.  Expected delimiter: {_delim}");
				//
				// Main purpose of the method
				//
				OnEvaluateContext += processXxx.Evaluate;
			}
		}

		//
		// One would expect that if a class provides an AddContext(..) method, then that class 
		// should also provide a RemoveContext(..) method.  I feel however, that processXxx 
		// classes are removed one at a time, if need be, or the entire collection of processXxx 
		// classes removed from the OnEvaluateContext through the Dispose() method.
		//
		// On the other hand if you feel that you need such a method, then your work is easy.  
		// Your RemoveContext will look as such:
		//
		public void RemoveContext(IEnumerable<IProcessEvaluate> context)
		{
			if (context == null) return;
			foreach (IProcessEvaluate processXxx in context)
				OnEvaluateContext -= processXxx.Evaluate;
		}

		/// <summary>
		/// Purpose:
		///		Delimiter balance check.  Each open delimiter should have a close delimiter
		///	
		///	<remarks>
		/// 	Called right before the evaluation ...
		///	</remarks>
		/// </summary>
		private string BalancePreEvaluate(string text)
		{
			if (_delim == null) throw new EnhancedStringException(null, text, "delimiters is not set");

			bool rc = _delim.IsBalancedOpenClose(text);
			if (!rc) throw new EnhancedStringException(null, text, "Delimiters are not balanced.");
			return text;
		}

		/// <summary>
		/// Purpose:
		///		Used as equivalent of the single-string BalancePreEvaluate(..)
		/// </summary>
		/// <param name="enhStrPairs"></param>
		/// <returns></returns>
		private void BalancePreEvaluate(IEnumerable<KeyValuePair<string, string>> enhStrPairs)
		{
			foreach (var elem in enhStrPairs)
			{
				bool rc = _delim.IsBalancedOpenClose(elem.Value);
				if (!rc) throw new EnhancedStringException(elem.Key, elem.Value, "Delimiters are not balanced.");
			}
		}

		/// <summary>
		/// In order to use the PreEvaluate/PostEvaluate methods one needs to override this, EnhancedStringEvaluate, class.  
		/// The PreEvaluate(..) is used to scrub or transform text before it is passed through the Evaluator.  See, for 
		/// example, the handling of different delimiters in the class: EnhancedStringEvalNoyyyMMdd, in the 
		/// EnhancedStringEvaluateTest.cs module <see cref="EvaluateSampleTest.EnhancedStringEvalNoyyyMMdd"/>.
		/// <code>
		/// 	sealed internal class EnhancedStringEvalNoyyyMMdd : EnhancedStringEval
		///		{
		///			public EnhancedStringEvalNoyyyMMdd(IEnumerable<IProcessEvaluate> context) : base(context) { }						// Close what looks like a tag </IProcessEvaluate>
		///			public EnhancedStringEvalNoyyyMMdd(IEnumerable<IProcessEvaluate> context, IDelimitersAndSeparator delim) : base(context, delim) { }		// Close what looks like an open tag </IProcessEvaluate>
		///			protected override string PreEvaluate(string text) { return text.Replace("#{", "${").Replace("}#", "}$"); }
		///		}
		/// 
		/// //Client
		/// 	IDelimitersAndSeparator delim = new DelimitersAndSeparator("${", "}$");
		///		var ctx = new List<IProcessEvaluate> { new ProcessMemory(delim) };			// Close what looks like an open tag </IProcessEvaluate>
		///		var eval = new EnhancedStringEvalNoyyyMMdd(ctx, delim);
		///		string fmtD = eval.EvaluateString("#{Memory::fmtD::dd/MM/yyyy}#");
		///		...
		/// </code>
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		protected virtual string PreEvaluate(string text)
		{
			return text;
		}

		/// <summary>
		/// The PostEvaluate(..) is used as an "opposite" operation to PreEvaluate(..).  It needs to "undo" what 
		/// the PreEvaluate(..) did.  (At times you do not need the PostEvaluate(..) to run.)
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		protected virtual string PostEvaluate(string text)
		{
			return text;
		}

		/// <summary>
		/// Equivalent to the single-string PreEvaluate(..) for a list of elements
		/// </summary>
		/// <param name="enhStrPairs"></param>
		/// <returns></returns>
		protected virtual void PreEvaluate(IDictionary<string, string> enhStrPairs) { }

		/// <summary>
		/// The opposite operation to multi-pair PreEvaluate(..)
		/// </summary>
		/// <param name="enhStrPairs"></param>
		/// <returns></returns>
		protected virtual void PostEvaluate(IDictionary<string, string> enhStrPairs) { }

		/// <summary>
		/// Purpose:
		///		Main entry point of the DLL.
		/// 
		///		Evaluate a string according to the rules laid out by the
		///		>	Pre / post Evaluate overrides
		///		>	ProcessXxx class
		/// </summary>
		/// <returns></returns>
		public virtual string EvaluateString(string text)
		{
			string preText = PreEvaluate(text);
			string balanceText = BalancePreEvaluate(preText);

			string evalText = EvaluateStringPure(balanceText);

			string postText = PostEvaluate(evalText);
			return postText;
		}

		/// <summary>
		/// Purpose:
		///		Perform the magic of the string evaluation
		/// 
		/// <remarks>
		///		1.	The inner loop resembles the EvalSimpleExpression(..) method, with one notable exception the 
		///			check for _delim.IsSimpleExpression(..).  This is done deliberately so that the ProcessLiteral, 
		///			processing expressions that are not a simple expressions, could be processed as well.  
		///			(ProcessLiteral evaluates "{xxx}" to "xxx".)
		///		2.	The handling of exceptions, by throwing an AggregateException(..) allows for the entire set of 
		///			processXxxEvaluate to run, the entire multi-cast delegate.  
		/// </remarks>
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		private string EvaluateStringPure(string text)
		{
			if (OnEvaluateContext == null) return text;

			var textElem = new EnhancedStrPairElement(Tempkey, _delim.PreMatch(text));
			var ea = new EnhancedStringEventArgs(textElem);

			IList<EnhancedStringException> exs = new List<EnhancedStringException>();
			Delegate[] cxtEvaluate = OnEvaluateContext.GetInvocationList();

			// The expected number of passes is the level of nested expressions + 1
			for (var passThroughCount = 0; passThroughCount < PassThroughUpperLimit; ++passThroughCount)
			{
				// Keep a flag, bHandled flag, for this passThrough, if one of the processXxxEvaluate delegates
				// successfully evaluates the text, in argument, then we are not done--we will go through another
				// pass, until the PassThroughUpperLimit.
				bool bHandled = false;

				foreach (Delegate processXxxEvaluate in cxtEvaluate)
				{
					try
					{
						// We pass our entire string through the next processXxxEvaluate handler/delegate.  
						// If there is no match, as per the processXxxEvaluate method/delegate, then the 
						// ea.IsHandled will be false.
						processXxxEvaluate.DynamicInvoke(this, ea);
					}
					catch (TargetInvocationException ex)
					{
						// We expect the invocation list to wrap our exception in a TargetinvocationException
						// as the method is invoked through reflection.
						var esx = ex.InnerException as EnhancedStringException;
						if (esx != null)
						{
							// We hope that if the invoked method, processXxxEvaluate, threw an exception it 
							// is an EnhancedStringException.
							exs.Add(esx);
						}
						else
						{
							exs.Add(new EnhancedStringException(ea.EhancedPairElem.Identifier, ea.EhancedPairElem,
								$"{MethodBase.GetCurrentMethod().Name}(\"{text}\"), PassThrough count={passThroughCount}: {processXxxEvaluate.GetType()}(this, {ea})",
								ex));
						}
					}
					catch (Exception ex)
					{
						exs.Add(new EnhancedStringException(ea.EhancedPairElem.Identifier, ea.EhancedPairElem,
							$"{MethodBase.GetCurrentMethod().Name}(\"{text}\"), PassThrough count={passThroughCount}: {processXxxEvaluate.GetType()}(this, {ea})",
							ex));
					}

					if (!ea.IsHandled) continue;

					// Invariant: ea.IsHandled is true.  Meaning this processXxxEvaluate has successfully changed the 
					// text, argument.
					bHandled = true;

					// At this point we need to Prepare for the next passThrough, by constructing the next EnhancedStringEventArgs.
					string val = _delim.PreMatch(ea.EhancedPairElem.Value);
					string preText = PreEvaluate(val);
					string balanceText = BalancePreEvaluate(preText);
					textElem = new EnhancedStrPairElement(Tempkey, balanceText);
					ea = new EnhancedStringEventArgs(textElem);

					// Rhetorical Question:
					// Do we need to loop-break (as in c# break), or are we better off letting the loop continue and process all 
					// processXxxEvaluate handlers?
					//
					// My answer:
					// I believe that the more expedient approach is to have the loop-break here.  We have successfully evaluated 
					// a simple construct by using one of the ProcessXxxEvaluate handlers, now if we do not loop-break here then 
					// we will continue to run the rest of the ProcessXxxEvalute handlers on the resulting Evaluated string.  I 
					// expect that there are situations where not breaking will expedite the process.  However, I expect that in 
					// general: to break here will be the better choice.
					//
					// Bothe, having a loop-break here and not have a loop-break here will result in the same result, what we are
					// talking about is execution time.
					break;
				}

				// AggregateExcpetion is new to .Net 4.0, so if you are using an older version of .Net you will need a different 
				// solution.  One possibility is to use the Data property of the Exception class and the exception could be:
				//		var ex = new Exception();
				//		ex.Data.Add("EvaluateStringPure", exs);
				//		if (exs.Count > 0) throw ex;
				if (exs.Count > 0) throw new AggregateException(exs);

				// If no processXxxEvaluate succeeded in changing the original string then we need not try evaluation any further.
				if (!bHandled) break;
			}

			// Now that we are done with our evaluations we need to run through PostMatch(..) evaluate only.  PostMatch(..) revert 
			// the delimiter change done in PreMatch(..).  The only other Post method that needs to be called is the PostEvaluate(..) 
			// method which is called by EvaluateString(..), further up the call stack from here.
			return _delim.PostMatch(ea.EhancedPairElem.Value);
		}

		/// <summary>
		/// Purpose:
		///		Optimization for a collection of strings that operate as a collection.
		///	
		/// <remarks>
		///		The equivalent to the EvaluateString--evaluating a single string;
		///		EvaluateStrings, evaluates a collection of pairs as a unit
		///			>	As a unit pre-evaluate (user overridden)
		///			>	Check balanced delimiters
		///			>	Run through pure evaluate
		///			>	Post check balanced delimiters
		///			>	Post-evaluate (user overridden)
		/// </remarks>
		/// </summary>
		/// <param name="enhStrPairs"></param>
		/// <returns></returns>
		public void EvaluateStrings(IDictionary<string, string> enhStrPairs)
		{
			PreEvaluate(enhStrPairs);
			BalancePreEvaluate(enhStrPairs);

			EvaluateStringsPure(enhStrPairs);

			PostEvaluate(enhStrPairs);
		}

		/// <summary>
		///		The magic happens here--for a collection <see cref="EvaluateSampleTest.EnhancedStringEvaluateTest.TestKey()"/>
		/// 
		///	Purpose:
		///		Processes all nodes at once.
		/// </summary>
		/// <param name="enhStrPairs"></param>
		private void EvaluateStringsPure(IDictionary<string, string> enhStrPairs)
		{
			if (OnEvaluateContext == null) return;

			//
			// Retrieve all nodes needing attention.
			// "Needing attention" means a node that has a simple expression in it.
			//
			var links = new LinkedList<EnhancedStrPairElement>();
			var pairNodes = enhStrPairs.Where(elem => _delim.IsSimpleExpression(elem.Value));
			foreach (KeyValuePair<string, string> pairNode in pairNodes)
				links.AddLast(new EnhancedStrPairElement(pairNode));

			if (links.Count == 0) return;

			// At this point runs through the process until we have no more notes to process.
			// The PassThroughUpperLimit avoids an infinite loop.
			for (int i = 0; i < PassThroughUpperLimit; ++i)
			{
				// Cycle through the linked list
				LinkedListNode<EnhancedStrPairElement> linkNode = links.First;
				while (linkNode != null)
				{
					// The magic of handling the static EnhancedStrPairElement expressions happen in EvalSimpleExpression(..).
					bool bEval = EvalSimpleExpression(linkNode.Value);
					if (!bEval)
					{
						// There is nothing to evaluate any further--Remove the node.
						LinkedListNode<EnhancedStrPairElement> p = linkNode.Next;
						links.Remove(linkNode);
						linkNode = p;
					}
					else
					{
						PreEvaluate(enhStrPairs);
						BalancePreEvaluate(enhStrPairs);
						linkNode = linkNode.Next;
					}

					// Invariant: linkNode is now advanced one node "forward" from where linkNode  was pointing to in the
					// beginning of the while loop.
				}

				if (links.Count == 0) break;
			}
		}

		/// <summary>
		/// Purpose:
		///		Evaluate a simple expression.
		/// 
		/// <remarks>
		///		Simple expression is one that does not have nested expressions.
		/// </remarks>
		/// </summary>
		/// <param name="elem"></param>
		/// <returns></returns>
		private bool EvalSimpleExpression(EnhancedStrPairElement elem)
		{
			// Error checking
			if (elem == null) throw new ArgumentException("Error: elem is null", nameof(elem));

			// If the expression is not simple then we move on.
			// The algorithm we use: evaluates one simple expression after the other until no simple expressions are left to 
			// evaluate (or no simple expressions are evaluated successfully).  At which point we are done.
			bool rc = _delim.IsSimpleExpression(elem.Value);
			if (!rc) return false;

			// When setting up the EnhancedStringEventArgs instance, to pass along, it is important that we use the single 
			// character delimiter.  _delim.PreMatch(..) ensures that the delimiter is either the original delimiter or the 
			// alternative delimiter.  Also note that the ProcessXxx classes use the xxxDelimEquivalent delimiters.
			var ea = new EnhancedStringEventArgs(new EnhancedStrPairElement(elem.Identifier, _delim.PreMatch(elem.Value)));

			// Saves all exceptions till the end where one throw will be used
			IList<EnhancedStringException> exs = new List<EnhancedStringException>();

			// run the simple expression by each one of the ProcessXxx evaluate methods
			foreach (Delegate processXxxEvaluate in OnEvaluateContext.GetInvocationList())
			{
				try
				{
					processXxxEvaluate.DynamicInvoke(this, ea);
				}
				catch (TargetInvocationException ex)
				{
					// We expect TargetInvocationExcetion as the method was invoked through reflection.
					var esx = ex.InnerException as EnhancedStringException;
					if (esx != null)
					{
						//
						// If the invoked method through an exception we hope that it is an EnhancedStringException
						//
						exs.Add(esx);
					}
					else
						exs.Add(new EnhancedStringException(elem.Identifier, elem,
							$"{MethodBase.GetCurrentMethod().Name}(EhancedStrPairElement): {processXxxEvaluate.GetType()}(this, {ea})",
							ex));
				}
				catch (Exception ex)
				{
					exs.Add(new EnhancedStringException(elem.Identifier, elem,
						$"{MethodBase.GetCurrentMethod().Name}(EhancedStrPairElement): {processXxxEvaluate.GetType()}(this, {ea})",
						ex));
				}

				// If exception was thrown by the ProcessXxxEvaluate.DynamicInvoke(..) then ea.IsHandled is false
				if (ea.IsHandled)
				{
					elem.Value = _delim.PostMatch(ea.EhancedPairElem.Value);
					return true;
				}
			}

			// AggregateExcpetion is new to .Net 4.0, so if you are using an older version of .Net, then you will 
			// need a different solution.  One possibility is to use the Data property of the Exception class and 
			// the resulting exception could be:
			//		var ex = new Exception("EvalSimpleExpression failed");
			//		ex.Data.Add("EvalSimpleExpression", exs);
			//		if (exs.Count > 0) throw ex;
			if (exs.Count > 0) throw new AggregateException(exs);
			return false;
		}

		#region IDisposable Members

		/// <summary>
		/// The dispose method is not necessary from a memory collection considerations.
		/// See TestEvaluation solution, end of the Program class.
		/// </summary>
		public void Dispose()
		{
			if (OnEvaluateContext == null) return;

			var dels = OnEvaluateContext.GetInvocationList();
			foreach (var pastXxx in dels.OfType<EventHandler<EnhancedStringEventArgs>>())
				if (pastXxx != null)
					OnEvaluateContext -= pastXxx;
		}

		#endregion

		//
		// No need for a finalizer as there are no unmanaged resources
		//
	}
}



