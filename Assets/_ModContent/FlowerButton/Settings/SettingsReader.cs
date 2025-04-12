

namespace FlowerButtonMod.FlowerButton.Settings {


	/// <summary>
	/// Reader of <see cref="FlowerButtonSettings"/> from disk.
	/// </summary>
	public static class SettingsReader {

		readonly static ModConfig<FlowerButtonSettings> modconfig;

		static SettingsReader() {

			// Create the inner reader
			modconfig = new ModConfig<FlowerButtonSettings>("FlowerButtonSettings");

			// Check the current save
			FlowerButtonSettings read = modconfig.Read();

			// If it is invalid -- start anew
			if (!modconfig.SuccessfulRead) {
				modconfig.SuccessfulRead = true;	// force write to work
				modconfig.Write(new FlowerButtonSettings());
			}

			// If settings are outdated -- resave with new version
			if (read.version < FlowerButtonSettings.CurrentVersion) {
				read.version = FlowerButtonSettings.CurrentVersion;
				modconfig.Write(read);
			}
		}

		/// <summary>
		/// Tries to read <see cref="FlowerButtonSettings"/> from mod configurations.
		/// </summary>
		/// <remarks>
		/// Internally uses <see cref="ModConfig{T}.Read"/>.
		/// </remarks>
		/// <returns>
		/// A new <see cref="FlowerButtonSettings"/> object.
		/// </returns>
		public static FlowerButtonSettings ReadSettings() {
			return modconfig.Read();
		}

	}

}
