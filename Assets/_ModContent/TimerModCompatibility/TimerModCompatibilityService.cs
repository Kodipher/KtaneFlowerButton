using UnityEngine;
using FlowerButtonMod.Utils;


namespace FlowerButtonMod.TimerModCompatibility {

	public class TimerModCompatibilityService : MonoBehaviour {

		const string ServiceName = "FlowerButtonTimerModCompatibilityService";

		#region //// Components and parts

		internal ModuleLogger logger;
		internal TimerModifierPatcher timerModifierPatcher;

		private void PrepareComponents() {

			// Name
			name = ServiceName;

			// Logger
			logger = new ModuleLogger(name, null);

			// Patcher
			timerModifierPatcher = new TimerModifierPatcher(logger);
			timerModifierPatcher.StageHarmonyLogger(logger);
		}

		#endregion

		#region //// Starting, Ending, Event connection

		bool isInitiated = false;
		bool isServicing = false;

		void Start() {
			PrepareComponents();
			logger.LogString("Service prefab initiated.");
			isInitiated = true;

			StartService();
		}

		void OnDestroy() {
			EndService();
			logger.LogString("Service prefab destroyed.");
		}

		private void OnEnable() {
			if (!isInitiated) return;
			logger.LogString("Service prefab enabling...");
			StartService();
		}

		private void OnDisable() {
			if (!isInitiated) return;
			logger.LogString("Service prefab disabling...");
			EndService();
		}

		void StartService() {
			if (isServicing) return;

			logger.LogString("Starting service.");
			Application.logMessageReceived += OnLogMessage;

			isServicing = true;
		}

		void EndService() {
			if (!isServicing) return;

			logger.LogString("Ending service.");
			OnServiceEnd();
			Application.logMessageReceived -= OnLogMessage;

			isServicing = false;
		}

		#endregion

		void OnLogMessage(string logString, string stackTrace, LogType type) {
			if (logString.StartsWith("[BombGenerator] Generating bomb")) OnBombGeneration();
		}

		void OnBombGeneration() {
			logger.LogString("Bomb is generating. Trying to apply patch...");
			timerModifierPatcher.TryPatch();
		}
		
		void OnServiceEnd() {
			logger.LogString("Trying to remove patch...");
			timerModifierPatcher.TryUnpatch();
		}

	}

}
