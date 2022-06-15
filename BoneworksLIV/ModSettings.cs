using MelonLoader;
using ModThatIsNotMod.BoneMenu;
using UnityEngine;

namespace BoneworksLIV
{
	public class ModSettings
	{
		public ModSettings()
		{
			var prefCategory = MelonPreferences.CreateCategory("LIV");

			ShowPlayerBody = prefCategory.CreateEntry("ShowPlayerBody", false, "Show player body",
				"If enabled, the Boneworks player body will be visible in the LIV camera. Make sure to hide your LIV avatar or they'll overlap.");

			var category = MenuManager.CreateCategory("LIV", Color.green);
			category.CreateBoolElement("Show player body", Color.white, ShowPlayerBody.Value, HandleShowPlayerBodyBoneMenuChange);
		}

		public MelonPreferences_Entry<bool> ShowPlayerBody { get; }

		private void HandleShowPlayerBodyBoneMenuChange(bool newValue)
		{
			ShowPlayerBody.Value = newValue;
			ShowPlayerBody.Save();
		}
	}
}