using System;
using System.Text.RegularExpressions;
using System.IO;

namespace StringHandling
{
	/// <summary>
	/// IO path and directory and file helper
	/// A helper class for file processing like ProcessForeignKey and ProcessIf
	/// </summary>
	public static class UtilHelper
    {
		/// <summary>Define a "good" relative path</summary>
		private static readonly Regex ReGoodRelativePath;

		/// <summary>Define a "good" path</summary>
		private static readonly Regex ReGoodPath;

		/// <summary>
		/// .cctor
		/// </summary>
		static UtilHelper()
		{
			// FileName restricted character set (characters not allowed in a file name)
			char[] cR = Path.GetInvalidFileNameChars();

			// Convert the restricted characters to a Unicode string understood by the 
			// regular expression evaluator ("\u9999") and make a single string out of it.
			// Note that if your first instinct is to form a string like:
			//		string restricted = new string(cR);
			// or
			//		byte[] bR = Array.ConvertAll<char, byte>(cR, c => (byte)c);
			//		string restricted = Encoding.UTF8.GetString(bR);
			// Then resist this urge, it leads to nothing but trouble when running it through 
			// a regular expression pattern matching.  The string has characters like a back 
			// slash ("\") affecting regular expression pattern matching adversely.
			// Instead do the following:
			string[] sR = Array.ConvertAll(cR, c => $"\\u{(int)c:X4}");
			string restricted = string.Join(string.Empty, sR);

			// A relative path is one not starting with a back-slash ("\") and 
			// between back-slash characters it contains no restricted character
			string relativePattern = $@"[^{restricted}]+(\\[^{restricted}]+)*(\\)?";
			ReGoodRelativePath = new Regex($@"^{relativePattern}$", RegexOptions.Singleline);

			// A full path starts with either a drive letter followed by a colon
			// or a double back-slash characters
			// then followed by a relative path.
			string pathPattern = $@"^((\w:\\{relativePattern})|(\\{{2}}{relativePattern}))$";
			ReGoodPath = new Regex(pathPattern, RegexOptions.Singleline);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="testPath"></param>
		/// <returns></returns>
		public static bool IsValidRelativeFilePath(this string testPath) => ReGoodRelativePath.IsMatch(testPath);

		/// <summary>
		/// Makes sure that the path starts with either a drive letter 
		/// followed by a colon or starts with a double back slash ("\").
		/// </summary>
		/// <param name="testPath"></param>
		/// <returns></returns>
		public static bool IsValidFullPath(this string testPath) => ReGoodPath.IsMatch(testPath);

		public static string ExtendToFullPath(this string pathNm)
		{
			// If the path pointed to by pathNm is a full path then we are good to go and no 
			// further path processing is needed.  But if, on the other hand, the path is a 
			// relative path, then prepend pathNm with the path of the executing program's path.
			bool rc = pathNm.IsValidFullPath();
			if (rc) return pathNm;

			rc = pathNm.IsValidRelativeFilePath();
			if (!rc)
			{
				// A better choice for this exception would be a more specialized exception 
				// structure, potentially employing Enterprise Library.  However, worrying about 
				// the exception will take us away from our main topic.
				throw new ArgumentException($"Invalid file name path: {pathNm}", nameof(pathNm));
			}

			// The fileNm is a valid relative path
			string basedir = AppDomain.CurrentDomain.BaseDirectory;
			pathNm = Path.Combine(basedir, pathNm);
			return pathNm;
		}
	}
}

