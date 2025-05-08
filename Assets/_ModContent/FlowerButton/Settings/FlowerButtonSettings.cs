using System.Collections.Generic;
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

	public static class FlowerButtonSettingsTweaksAnnotations {

		public static Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[] {
			
			new Dictionary<string, object>() {
				{ "Filename", "FlowerButtonSettings.json" },
				{ "Name", "Flower Button" },
				{ 
					"Listings",
					new List<Dictionary<string, object>> {
						new Dictionary<string, object> { { "Key", "version" }, { "Type", "Hidden" } },
						new Dictionary<string, object> { { "Key", "readme" }, { "Type", "Hidden" } },
						
						new Dictionary<string, object> { { "Text", "Note: These settings are meant for accessibility" }, { "Type", "Section" } },
						new Dictionary<string, object> { { "Key", "disable_forced_detonation" }, { "Text", "Disable Forced Detonation" }, { "Description", "If enabled, the module only strikes once and resets." } },
						new Dictionary<string, object> { { "Key", "disable_visual_distortion" }, { "Text", "Disable Visual Distortion" } },
						new Dictionary<string, object> { { "Key", "disable_musicbox" }, { "Text", "Disable Musicbox" } },
					}
				}
			}
			// [end of FlowerButtonSettings.json annotations]

		};

	}

}
