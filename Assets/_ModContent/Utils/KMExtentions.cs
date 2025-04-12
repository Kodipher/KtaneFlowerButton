

namespace FlowerButtonMod.Utils {

	public static class KMExtentions {

		/// <summary>
		/// Equivalent of PlaySoundAtTransformWithRef but with the loop option disabled.
		/// </summary>
		public static KMAudio.KMAudioRef PlaySoundAtTransformWithRefNoLoop(
			this KMAudio audio, 
			string name,
			UnityEngine.Transform transform
		) {
			if (audio.HandlePlaySoundAtTransformWithRef != null) {
				return audio.HandlePlaySoundAtTransformWithRef(name, transform, false);
			}

			return null;
		}

	}

}