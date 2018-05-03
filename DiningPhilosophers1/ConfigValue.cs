using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;
using StringHandling;
using StringHandling.ProcessEvaluate;
using System.Linq;

namespace DiningPhilosophers1
{
	/// <summary>
	/// Centralize the access to the configuration file.
	/// </summary>
	public class ConfigValue
	{
		private static readonly Lazy<ConfigValue> LazyInst = new Lazy<ConfigValue>(() => new ConfigValue());
		public static readonly ConfigValue Inst = LazyInst.Value;

		private EnhancedStringEval _eval = null;
		private IList<IProcessEvaluate> _context;
		private IDictionary<string, string> _configValues;

		private ConfigValue()
		{
			var pIntDivide = new ProcessIntegerDivide();
			_configValues = ConfigurationManager.AppSettings.AllKeys.ToDictionary(id => id, id => ConfigurationManager.AppSettings[id]);
			var pConfig = new ProcessConfigKey(_configValues);
			_context = new List<IProcessEvaluate> { pIntDivide, pConfig };
			_eval = new EnhancedStringEval(_context);
		}

		public int PhilosopherCount
		{
			get
			{
				const string key = "Philosopher Count";
				const int philosopherCountDefault = 5;
				var philosopherCount = ExtractInteger(key, philosopherCountDefault);
				return philosopherCount;
			}
		}

		public int ForkCount
		{
			get
			{
				const string key = "Fork Count";
				var forkCountDefault = PhilosopherCount;
				var forkCount = ExtractInteger(key, forkCountDefault);
				return forkCount;
			}
		}

		public int MaxPhilsophersToEatSimultaneously
		{
			get
			{
				const string key = "Max philosophers to eat simultaneously";
				const int maxPhilsophersToEatSimultaneouslyDefault = 2;
				var maxPhilsophersToEatSimultaneously = ExtractInteger(key, maxPhilsophersToEatSimultaneouslyDefault);
				return maxPhilsophersToEatSimultaneously;
			}
		}

		public int DurationPhilosophersEat
		{
			get
			{
				const string key = "Duration Allow Philosophers To Eat [seconds]";
				const int durationPhilosophersEatDefault = 20 * 1000;
				var durationPhilosophersEat = ExtractInteger(key, durationPhilosophersEatDefault);
				return durationPhilosophersEat * 1000;
			}
		}

		public int MaxEatDuration
		{
			get
			{
				const string key = "philosopher Max Eat Duration [milliseconds]";
				const int maxEatDurationDefault = 1000;
				var maxEatDuration = ExtractInteger(key, maxEatDurationDefault);
				return maxEatDuration;
			}
		}

		public int MinEatDuration
		{
			get
			{
				const string key = "philosopher Min Eat Duration [milliseconds]";
				const int minEatDurationDefault = 50;
				var minEatDuration = ExtractInteger(key, minEatDurationDefault);
				return minEatDuration;
			}
		}

		public int DurationBeforeAskingPermissionToEat
		{
			get
			{
				const string key = "Duration Before Requesting Next Permission To Eat [milliseconds]";
				const int durationBeforeSeekEatPermissionDefault = 20;
				var durationBeforeSeekEatPermission = ExtractInteger(key, durationBeforeSeekEatPermissionDefault);
				return durationBeforeSeekEatPermission;
			}
		}

		private string GetConfigValue(string key)
		{
			var val = _configValues[key];
			var eVal = _eval.EvaluateString(val);
			return eVal;
		}

		private string ReplaceKey(Match m)
		{
			var key = m.Groups["val"].Value;
			if (string.IsNullOrEmpty(key)) return string.Empty;

			var val = ConfigurationManager.AppSettings[key];
			return val ?? string.Empty;
		}

		private int ExtractInteger(string key, int defaultValue)
		{
			var sValue = GetConfigValue(key);

			var rc = int.TryParse(sValue, NumberStyles.Integer, CultureInfo.CurrentCulture, out int intValue);
			if (!rc)
			{
				Console.WriteLine($"{key} configuration variable does not value to an integer: \"{sValue}\".  Using default {defaultValue}");
				return defaultValue;
			}

			if (intValue <= 0)
			{
				Console.WriteLine($"{key} configuration variable may be 0 or negative: {intValue}.  Using default {defaultValue}");
				return defaultValue;
			}

			return intValue;
		}
	}
}
