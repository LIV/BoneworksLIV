using System;
using MelonLoader;
using ModThatIsNotMod.BoneMenu;
using UnityEngine;

namespace BoneworksLIV
{
	public class ModSettings
	{
		public static Action OnSettingChanged;

		public ModSettings()
		{
			var prefCategory = MelonPreferences.CreateCategory("LIV");

			var showPlayerBodyEntry = prefCategory.CreateEntry("ShowPlayerBody", false, "Show player body",
				"If enabled, the Boneworks player body will be visible in the LIV camera. Make sure to hide your LIV avatar or they'll overlap.");
			ShowPlayerBody = showPlayerBodyEntry.Value;
			showPlayerBodyEntry.OnValueChanged += HandleShowPlayerBodyMelonChange;

			var category = MenuManager.CreateCategory("LIV", Color.green);
			category.CreateBoolElement("Show player body", Color.white, ShowPlayerBody, HandleShowPlayerBodyChange);
		}

		public bool ShowPlayerBody { get; private set; }

		private void HandleShowPlayerBodyChange(bool newValue)
		{
			ShowPlayerBody = newValue;
			OnSettingChanged?.Invoke();
		}

		private void HandleShowPlayerBodyMelonChange(bool oldValue, bool newValue)
		{
			HandleShowPlayerBodyChange(newValue);
		}
	}
}