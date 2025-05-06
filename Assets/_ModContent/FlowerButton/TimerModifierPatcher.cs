using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FlowerButtonMod.Utils;

using UnityEngine;
using UnityEngine.UI;

using System.Reflection;
using HarmonyLib;


namespace FlowerButtonMod.FlowerButton {

	/// <summary>
	/// <para>
	/// Provides compatibility with Bomb Timer Modifier
	/// </para>
	/// <para>
	/// Flower Button saps TimerComponent's display
	/// however BombTimerModifier mod provides it's own timer component
	/// and updates time in a unity couroutine
	/// which means it requires a more advanced patch
	/// (on top of it having controling it differently)
	/// </para>
	/// </summary>
	public class TimerModifierPatcher {

		const string HarmonyId = "kodipher.FlowerButtonMod.TimerModifierPatcher";
		static internal ModuleLogger staticLogger;  // for harmony

		#region //// String names

		const string TimerModifierAssembly = "KtaneTimerV2";
		const string ModifiedTimerComponentTypeName = "CustomTimerComponent";
		const string GeneratedUpdateCoroutineNestedTypeName = "<CustomUpdate>c__Iterator2";
		const string IteratorNextMethodName = nameof(IEnumerator.MoveNext);

		const string IteratorThisContextFieldName = "$this";
		const string IteratorProgramCounterFieldName = "$PC";

		const string TimerModelPropertyName = "Model";
		const string ShowModeTextPropertyName = "ShowModeText";
		const string LastTimePropertyName = "_lastTimeKey";

		const string TimerModelTextFieldName = "TimeText";
		const string TimerModelTextUnderlayFieldName = "TimeTextUnderlay";

		#endregion

		#region //// Patcher

		internal ModuleLogger logger;
		internal Harmony harmony;

		private static readonly object patchingLock = new object();

		public TimerModifierPatcher(ModuleLogger logger)  {
			this.logger = logger;
			harmony = new Harmony(HarmonyId);
		}

		/// <remarks>Only a requirement becuase the patch must also be static.</remarks>
		public void StageHarmonyLogger(ModuleLogger logger) {
			staticLogger = logger;
		}

		/// <summary>Applies the patch if it is not there.</summary>
		public void PatchOrConfirm() {

			// Try find the class and method
			Type modifiedTimerComponentClass = 
				ReflectionHelper.FindType(ModifiedTimerComponentTypeName, TimerModifierAssembly);

			Type updateIteratorType = modifiedTimerComponentClass
											?.GetNestedType(
												GeneratedUpdateCoroutineNestedTypeName,
												BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
											);

			var originalMethod = updateIteratorType
											?.GetMethod(
												IteratorNextMethodName, 
												BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
											);

			if (updateIteratorType == null || originalMethod == null) {
				logger.LogString(
					"Could not find modified timer commonent class or target method. " +
					"Mod might not be loaded. Skipping patch."
				);
				return;
			}

			
			lock (patchingLock) {

				// Check if the patch already exists
				Patches patches = Harmony.GetPatchInfo(originalMethod);
				if (patches != null) {
					if (patches.Postfixes.Where(patch => patch.owner == HarmonyId).Any()) {
						logger.LogString("Modified timer component is already patched.");
						return;
					}
				}

				// Patch
				logger.LogString("Performing modified timer patch.");

				const string patchName = nameof(PostfixPatch_CustomTimerComponent_CustomUpdate_MoveNext);
				var postfixPatchMethodInfo = typeof(TimerModifierPatcher).GetMethod(patchName);

				harmony.Patch(originalMethod, postfix: new HarmonyMethod(postfixPatchMethodInfo));
			}
		}

		/// <returns>true if the patch was applied, was already there, or was not needed.</returns>
		public bool TryPatch() {

			try {
				logger.LogString($"Patching Bomb Timer Modifier...");
				PatchOrConfirm();
				return true;
			} catch (System.Exception ex) {
				logger.LogException(ex);
				logger.LogString($"Bomb Timer Modifier patch failed.");
				TryUnpatch();
				return false;
			}

		}

		/// <summary>Removes the applied patch.</summary>
		/// <returns>true if the patch was removed or is not there.</returns>
		public bool TryUnpatch() {
			try {

				lock (patchingLock) {
					logger.LogString("Trying to unpatching Bomb Timer Modifier...");
					logger.LogString("Removing harmony patches if exist.");
					harmony.UnpatchAll(HarmonyId);
					return true;
				}

			} catch (System.Exception ex) {
				logger.LogException(ex);
				logger.LogString("Bomb Timer Modifier patch removal failed.");
				return false;
			}
		}

		#endregion

		#region //// The Patch

		internal static object patchOriginalBombTarget = null;
		internal static string patchStringReplacement = null;

		public static void OverrideNewBombTimerForOneFrame(object originalBombReference, string newTest) {
			patchOriginalBombTarget = originalBombReference;
			patchStringReplacement = newTest;
		}

		public static void PostfixPatch_CustomTimerComponent_CustomUpdate_MoveNext(object __instance) {

			// Skip patch on incorrect state
			if (__instance.GetValue<int>(IteratorProgramCounterFieldName) != 2) {
				return;
			}

			// Skip patch if no refernece to patch on
			if (patchOriginalBombTarget == null) return;

			// Only patch if the bomb references match
			object timerComponent = __instance?.GetValue<object>(IteratorThisContextFieldName);
			object originalBomb = timerComponent?.GetValue<object>("Bomb");
			if (patchOriginalBombTarget != originalBomb) return;

			// Find text models
			object timerModel = timerComponent?.GetValue<object>(TimerModelPropertyName);
			Text timeText = timerModel.GetValue<Text>(TimerModelTextFieldName);
			Text timeUnderlayText = timerModel.GetValue<Text>(TimerModelTextUnderlayFieldName);

			// Override the timer
			timeText.text = patchStringReplacement;

			// Update other stuff
			// (magic values copied from decomp)
			bool showModeText = timerComponent.GetValue<bool>(ShowModeTextPropertyName);
			timeText.rectTransform.localScale = new Vector3(1f, !showModeText ? 1.57f : 1.2f, 1f);
			timeUnderlayText.text = "88:88";
			timeUnderlayText.rectTransform.localScale = timeText.rectTransform.localScale;

			// Clear the override for the next frame
			patchOriginalBombTarget = null;
			patchStringReplacement = null;
			timerComponent.SetValue(LastTimePropertyName, (int)-1); // Forces the timer to update
		}

		#endregion

	}

}
