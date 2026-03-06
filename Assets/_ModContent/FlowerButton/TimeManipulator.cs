using UnityEngine;


namespace FlowerButtonMod.FlowerButton {

	internal static class TimeManipulator {

		const float slowedTimeScale = 0.001f;

		private static readonly object gameTimeManipulationLock = new object();

		// A flag to prevent multiple buttons from being held
		public static bool IsAnyButtonManipulatingTime { get; private set; } = false;


		/// <returns>true on success, false if time is manipulated by some other button.</returns>
		public static bool TrySlowTime() {

			// Check if allowed to manipulate time
			lock (gameTimeManipulationLock) {
				if (IsAnyButtonManipulatingTime) return false;

				IsAnyButtonManipulatingTime = true;
			}

			Time.timeScale = slowedTimeScale;

			return true;
		}

		public static void RestoreTime() {
			lock (gameTimeManipulationLock) {
				if (!IsAnyButtonManipulatingTime) return;
				IsAnyButtonManipulatingTime = false;
				Time.timeScale = 1f;
			}
		}

	}

}
