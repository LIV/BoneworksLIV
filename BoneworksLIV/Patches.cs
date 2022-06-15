using System.Linq;
using HarmonyLib;
using StressLevelZero.Player;
using UnityEngine;
using Valve.VR;

namespace BoneworksLIV
{
	[HarmonyPatch]
	public static class Patches
	{
		// Names of objects belonging to the head of the default Boneworks player model.
		// TODO: investigate a good way to do this for custom models.
		private static readonly string[] faceObjectNames =
		{
			"brett_face",
			"brett_hairCap",
			"brett_hairCards"
		};

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SteamVR_Camera), "OnEnable")]
		private static void SetUpLiv(SteamVR_Camera __instance)
		{
			BoneworksLivMod.OnCameraReady(__instance.GetComponent<Camera>());
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(CharacterAnimationManager), "OnEnable")]
		private static void SetUpBodyVisibility(CharacterAnimationManager __instance)
		{
			foreach (var renderer in __instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				var rendererObject = renderer.gameObject;
				var isHeadObject = faceObjectNames.Contains(rendererObject.name);

				if (isHeadObject)
				{
					rendererObject.SetActive(true);
				}
				rendererObject.layer = isHeadObject ? (int) GameLayer.LivOnly : (int) GameLayer.Player;
			}
		}
	}
}