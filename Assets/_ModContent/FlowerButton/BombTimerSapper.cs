using UnityEngine;
using FlowerButtonMod.TimerModCompatibility;
using TimeSpan = System.TimeSpan;


namespace FlowerButtonMod.FlowerButton {

	internal class BombTimerSapper {

		public BombTimerSapper(KMBombModule sappingModule) {
			this.sappingModule = sappingModule;
		}

		readonly KMBombModule sappingModule;

		// Cache
		object cachedBombReference = null;
		object cachedTimerComponentDisplayTextMeshPro = null;

		bool? serviceObjectExists = null;	// null if not checked (cache not populated)
		object cachedServiceObject = null;

		/// <returns>true if could sap the timer</returns>
		public bool SapBombTimerForOneFrame(string displayOverride) {

			if (Application.isEditor) {

				// Override time in test harness
				GameObject
					.Find("Bomb/TimerModule(Clone)/Timer_Screen/Timer Text")
					.GetComponent<TextMesh>()
					.text = displayOverride;

				return true;
			};

			// Find the bomb
			if (cachedBombReference == null) {
				cachedBombReference = sappingModule
									   ?.GetComponent("BombComponent")
									   ?.GetValue<object>("Bomb");

				if (cachedBombReference == null) return false;
			}

			// Find the timer text
			if (cachedTimerComponentDisplayTextMeshPro == null) {
				cachedTimerComponentDisplayTextMeshPro = cachedBombReference
										?.CallMethod<object>("GetTimer")
										?.GetValue<object>("text");

				if (cachedTimerComponentDisplayTextMeshPro == null) return false;
			}

			// Override for 1 frame
			cachedTimerComponentDisplayTextMeshPro.SetValue("text", displayOverride);

			// Also give notice to the patcher, if exists
			if (serviceObjectExists == null) {
				cachedServiceObject = Object.FindObjectOfType<TimerModCompatibilityService>();
				serviceObjectExists = cachedServiceObject != null;
			}

			if (!serviceObjectExists.Value) return true;

			TimerModifierPatcher.OverrideNewBombTimerForOneFrame(
				cachedBombReference,
				displayOverride
			);

			return true;
		}

		/// <returns>true if could subtract time</returns>
		public bool SubtractTime(TimeSpan time) {

			if (Application.isEditor) {

				#if UNITY_EDITOR
				// Subtract time in test harness
				// TimerModule is not in the final assembly
				TimerModule testHardnessTimer = GameObject
													.Find("Bomb/TimerModule(Clone)")
													.GetComponent<TimerModule>();

				testHardnessTimer.TimeRemaining -= (float)time.TotalSeconds;
				#endif

				return true;
			}

			// Find the bomb
			if (cachedBombReference == null) {
				cachedBombReference = sappingModule
									   ?.GetComponent("BombComponent")
									   ?.GetValue<object>("Bomb");

				if (cachedBombReference == null) return false;
			}

			// Find the timer
			object timer = cachedBombReference?.CallMethod<object>("GetTimer");
			if (timer == null) return false;
			
			// Perform additional subtraction
			float newTime = timer.GetValue<float>("TimeRemaining") - (float)time.TotalSeconds;
			timer.SetValue("TimeRemaining", newTime);

			return true;
		}

	}

}
