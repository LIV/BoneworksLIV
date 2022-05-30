using HarmonyLib;
using Steamworks;

namespace BoneworksLIV
{
	[HarmonyPatch]
	public class Patches
	{
		[HarmonyPrefix]
		[HarmonyPatch(typeof(SteamAPI), "RestartAppIfNecessary")]
		// TODO definitely don't include this in the mod.
		private static bool SkipSteamCheck()
		{
			return false;
		}
	}
}