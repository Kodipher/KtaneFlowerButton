using UnityEngine;
using System;
using System.Reflection;
using Rephidock.GeneralUtilities.Collections;


namespace FlowerButtonMod.FlowerButton {

	internal class BombTimerSapper {

		const string moddedTimerOverrideName = "_temporaryOverride";

		readonly Lazy<object> bombTimer;
		readonly Lazy<object> timerDisplayTextMeshPro;
		readonly Lazy<bool> timerHasModdedOverride;

		public BombTimerSapper(KMBombModule sappingModule) {

			bombTimer = new Lazy<object>(
				() => sappingModule?.GetComponent("BombComponent")?.GetValue<object>("Bomb")?.CallMethod<object>("GetTimer")
			);

			timerDisplayTextMeshPro = new Lazy<object>(
				() => bombTimer.Value?.GetValue<object>("text")
			);

			timerHasModdedOverride = new Lazy<bool>(
				() => {
					Type timerType = bombTimer.Value.GetType();

					FieldInfo overrideField = timerType.GetCachedMember<FieldInfo>(moddedTimerOverrideName);
					if (overrideField != null) return true;

					PropertyInfo overrideProperty = timerType.GetCachedMember<PropertyInfo>(moddedTimerOverrideName);
					if (overrideField != null) return true;

					return false;
				}
			);
		}

		/// <summary>Called if the timer could not be sapped.</summary>
		public event Action<Exception> OnSapError = (_) => { };

		/// <summary>Called if the timer could not be subtracted.</summary>
		public event Action<Exception> OnSubtractError = (_) => { };

		/// <summary>
		/// Overrides the bomb timer for
		/// - 1 frame if the timer is vanilla
		/// - until clearned with <see cref="UnsapBombTimer"/> if the timer is modded.
		/// </summary>
		/// <param name="displayOverride">The string to override with</param>
		public void SapBombTimer(string displayOverride) {
			try {
				SapBombTimerInternal(displayOverride);
			} catch (Exception ex) {
				OnSapError.Invoke(ex);
			}
		}

		/// <remarks>throws if could not sap the timer</remarks>
		internal void SapBombTimerInternal(string displayOverride) {

			if (Application.isEditor) {

				// Override time in test harness
				GameObject
					.Find("Bomb/TimerModule(Clone)/Timer_Screen/Timer Text")
					.GetComponent<TextMesh>()
					.text = displayOverride;

				return;
			};

			// Initialize (and cache) lazy values
			if (bombTimer.Value == null) throw new InvalidOperationException("Could not find the bomb timer refernece.");
			if (timerDisplayTextMeshPro.Value == null) throw new InvalidOperationException("Could not find the bomb timer text refernece.");
			
			// Override
			if (timerHasModdedOverride.Value) {
				// modded timer, until cleared
				bombTimer.Value.SetValue(moddedTimerOverrideName, displayOverride);
			} else {
				// vanilla timer for 1 frame
				timerDisplayTextMeshPro.Value.SetValue("text", displayOverride);
			}
		}

		public void UnsapBombTimer() {

			// Only for Bomb Timer Modifier
			// as test hardness and vanilla timers override text every frame
			if (Application.isEditor || !timerHasModdedOverride.Value) return;

			bombTimer.Value.SetValue(moddedTimerOverrideName, null);
			bombTimer.Value.SetValue("_lastTimeKey", -1); // Forces the timer to update
		}

		public void SubtractTime(TimeSpan time) {
			try {
				SubtractTimeInternal(time);
			} catch (Exception ex) {
				OnSubtractError.Invoke(ex);
			}
		}

		/// <remarks>throws if could not subtract timer</remarks>
		internal void SubtractTimeInternal(TimeSpan time) {

			if (Application.isEditor) {

				#if UNITY_EDITOR
				// Subtract time in test harness
				// TimerModule is not in the final assembly
				TimerModule testHardnessTimer = GameObject
													.Find("Bomb/TimerModule(Clone)")
													.GetComponent<TimerModule>();

				testHardnessTimer.TimeRemaining -= (float)time.TotalSeconds;
				#endif

				return;
			}

			// Find the bomb timer
			if (bombTimer.Value == null) throw new InvalidOperationException("Could not find the bomb time refernece.");

			// Perform additional subtraction
			float newTime = bombTimer.Value.GetValue<float>("TimeRemaining") - (float)time.TotalSeconds;
			bombTimer.Value.SetValue("TimeRemaining", newTime);
		}

	}

}
