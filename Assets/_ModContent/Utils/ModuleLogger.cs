
//#define MODULELOGGER_AUTOPRINT_HELLOWORLD


using System.Collections.Generic;

using UnityEngine;


namespace FlowerButtonMod.Utils {

	/// <summary>
	/// A class that is designed to log stuff in a propper format for the log file analyser.
	/// Also performs instance counting per given name.
	/// Should be created one time per module instance due to counting.
	/// Not a singleton but has <see langword="static"/> logic.
	/// </summary>
	public class ModuleLogger {

		// Made just to have logging functionaly split off

		// Primarily for log file analyser
		// and thus must be included in release

		#region //// Constructor, Tag

		/// <inheritdoc cref="ModuleLogger"/>
		/// <param name="moduleComponent">The module component to grab display name from.</param>
		public ModuleLogger(KMBombModule moduleComponent) : this(moduleComponent.ModuleDisplayName) {
		}

		/// <inheritdoc cref="ModuleLogger"/>
		/// <param name="moduleComponent">The module component to grab display name from.</param>
		public ModuleLogger(KMNeedyModule moduleComponent) : this(moduleComponent.ModuleDisplayName) {
		}

		/// <inheritdoc cref="ModuleLogger"/>
		/// <param name="moduleDisplayName">Name that appears in the log and that is used to count instances</param>
		public ModuleLogger(string moduleDisplayName) : this(moduleDisplayName, CountNext(moduleDisplayName)) {
		}

		/// <inheritdoc cref="ModuleLogger.ModuleLogger(string)"/>
		/// <param name="moduleInstanceIndex">
		/// Index in the tag. No couting is involved if provided. 
		/// null can be provided to not add the number (e.g. for a service).
		/// </param>
		public ModuleLogger(string displayName, int? moduleInstanceIndex) {

			// Number-less tag
			if (!moduleInstanceIndex.HasValue) {
				tag = CreateLogTagNumberless(displayName);
				return;
			}

			// Create log tag
			tag = CreateLogTag(displayName, moduleInstanceIndex.Value);

			// Autoprint something
			#if MODULELOGGER_AUTOPRINT_HELLOWORLD
			LogString("Notice me, Logfile Analyzer!");
			LogLine();
			#endif
		}

		/// <summary>Log tag that is used to identify the module in the logfile.</summary>
		readonly string tag;

		static string CreateLogTag(string moduleDisplayName, int moduleInstanceIndex) {
			return $"[{moduleDisplayName} #{moduleInstanceIndex}]";
		}

		static string CreateLogTagNumberless(string moduleDisplayName) {
			return $"[{moduleDisplayName}]";
		}

		#endregion

		#region //// (static) Instance counting

		private static readonly object counterLock = new object();

		private static readonly Dictionary<string, int> instanceCounts = new Dictionary<string, int>();

		/// <summary>Resets count of a specific name</summary>
		public static void CountReset(string name) {
			lock (counterLock) {
				instanceCounts.Remove(name);
			}
		}

		/// <summary>Starts a new instance counter at 1 or advances existing by one.</summary>
		/// <returns>Newly counted index (new total)</returns>
		public static int CountNext(string name) {
			lock (counterLock) {
				// Known name
				if (instanceCounts.ContainsKey(name)) {
					instanceCounts[name] = instanceCounts[name] + 1;
					return instanceCounts[name];
				}

				// New name
				instanceCounts[name] = 1;
				return instanceCounts[name];
			}
		}

		/// <summary>Returns current count of instances without advancing it</summary>
		/// <returns>Number of counted instances</returns>
		public static int CountCurrent(string name) {
			lock (counterLock) {
				// Known name
				if (instanceCounts.ContainsKey(name)) return instanceCounts[name];

				// New name
				return 0;
			}
		}

		#endregion

		#region //// Settings, Logging

		const char lineChar = '═';
		const int lineLength = 15;

		public void LogString(string str) {
			Debug.Log($"{tag} {str}");
		}

		public void LogStringFormat(string formatString, params object[] args) {
			Debug.LogFormat($"{tag} {formatString}", args);
		}

		public void LogStringError(string str) {
			Debug.LogError($"{tag} {str}");
		}

		public void LogException(System.Exception ex) {
			Debug.LogWarning($"{tag} An exception has occured. See below.");
			Debug.LogException(ex);
		}

		public void LogStrings(IEnumerable<string> strs) {
			foreach (string s in strs) LogString(s);
		}

		public void LogLine() {
			LogString(new string(lineChar, lineLength));
		}

		#endregion

	}

}
