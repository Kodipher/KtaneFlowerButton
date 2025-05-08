using Newtonsoft.Json;


namespace FlowerButtonMod.FlowerButton.Settings {

	/// <summary>
	/// Contains user configuarion of this mod.
	/// Is not immutable and is not global.
	/// </summary>
	public /*record*/ class FlowerButtonSettings {

		public const int CurrentVersion = 1;

		[JsonProperty("readme", Required = Required.Default, Order = -2)]
		public string readme = "Note: These settings are meant for accessibility";

		[JsonProperty("version", Required = Required.Always, Order = -1)]
		public int version = CurrentVersion;

		[JsonProperty("disable_forced_detonation")]
		public bool disableForcedDetonation = false;

		[JsonProperty("disable_visual_distortion")]
		public bool disableVisualDistortion = false;

		[JsonProperty("disable_musicbox")]
		public bool disableMusicbox = false;

	}

}
