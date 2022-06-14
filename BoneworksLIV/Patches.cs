using HarmonyLib;
using StressLevelZero.Player;
using UnityEngine;
using Valve.VR;

namespace BoneworksLIV
{
	[HarmonyPatch]
	public static class Patches
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(SteamVR_Camera), "OnEnable")]
		private static void SetUpLiv(SteamVR_Camera __instance)
		{
			BoneworksLivMod.PlayerReady(__instance.GetComponent<Camera>());
		}


		[HarmonyPostfix]
		[HarmonyPatch(typeof(CharacterAnimationManager), "OnEnable")]
		private static void SetUpBodyVisibility(CharacterAnimationManager __instance)
		{
			foreach (var renderer in __instance.GetComponentsInChildren<SkinnedMeshRenderer>())
			{
				renderer.gameObject.layer = (int) GameLayer.Player;
			}
		}
	}
}