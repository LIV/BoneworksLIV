using System;
using System.Collections.Generic;
using System.Linq;
using BoneworksLIV.AvatarTrackers;
using HarmonyLib;
using RealisticEyeMovements;
using RootMotion.FinalIK;
using StressLevelZero.Player;
using StressLevelZero.Rig;
using StressLevelZero.VRMK;
using TMPro;
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
			var renderers = __instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			var bodyRenderer = renderers.First(renderer => renderer.name == "brett_body");
			var bodyRendererCopyEnabledState = bodyRenderer.gameObject.AddComponent<BodyRendererManager>();

			__instance.gameObject.AddComponent<PathfinderRigidTransform>();
			
			foreach (var renderer in __instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				var rendererObject = renderer.gameObject;
				var isHeadObject = faceObjectNames.Contains(rendererObject.name);

				if (isHeadObject)
				{
					bodyRendererCopyEnabledState.headRenderers.Add(renderer);
					rendererObject.SetActive(true);
				}
				rendererObject.layer = isHeadObject ? (int) GameLayer.LivOnly : (int) GameLayer.Player;
			}

			__instance.gameObject.AddComponent<PathfinderAvatarTrackers>();
		}
		
		[HarmonyPostfix]
		[HarmonyPatch(typeof(RigEvent), "Awake")]
		private static void HideHeadEffectsFromLiv(RigEvent __instance)
		{
			__instance.gameObject.layer = (int) GameLayer.Player;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(BodyVitals), "CalibratePlayerBodyScale", new Type[]{})]
		[HarmonyPatch(typeof(BodyVitals), "CalibratePlayerBodyScale", typeof(RigManager))]
		private static void SendPlayerBodyDimensionsToPathFinder(BodyVitals __instance)
		{
			// TODO cleanup this, extract to a helper to be used in pathFinderRigidTransformBehaviour too.
            const string pathBase = "localAvatarTrackers.";

            var height = __instance.realWorldHeight;
			SDKBridgePathfinder.SetValue($"{pathBase}bob.stage.avatar.height", ref height, (int) PathfinderType.Float);
			var slzBody = __instance.mngr_Rig.GetComponentInChildren<SLZ_Body>();
			var armSpan = height * slzBody.arms.armLengthFactor;
			SDKBridgePathfinder.SetValue($"{pathBase}bob.stage.avatar.armspan", ref armSpan, (int) PathfinderType.Float);
		}
	}
}