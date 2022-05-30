using HarmonyLib;
using Steamworks;
using UnityEngine;
using Valve.VR;

namespace BoneworksLIV
{
	[HarmonyPatch]
	public static class Patches
	{
		[HarmonyPrefix]
		[HarmonyPatch(typeof(SteamAPI), "RestartAppIfNecessary")]
		// TODO definitely don't include this in the mod.
		private static bool SkipSteamCheck()
		{
			return false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SteamVR_Camera), "OnEnable")]
		private static void SetUpLiv(SteamVR_Camera __instance)
		{
			BoneworksLivMod.PlayerReady(__instance.GetComponent<Camera>());
		}
	}
}