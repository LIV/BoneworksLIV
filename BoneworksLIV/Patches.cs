using System.Collections.Generic;
using System.Linq;
using BoneworksLIV.AvatarTrackers;
using HarmonyLib;
using RealisticEyeMovements;
using RootMotion.FinalIK;
using StressLevelZero.Player;
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

		private static readonly Dictionary<string, string> boneMap = new Dictionary<string, string>()
		{
			{ "Neck_01SHJnt", "avatar.trackers.head" }, // other options: Neck_02SHJnt / Neck_TopSHJnt / Head_JawSHJnt / Head_TopSHJnt
			{ "Spine_TopSHJnt", "avatar.trackers.chest" },
			{ "ROOTSHJnt", "avatar.trackers.waist" },
			{ "l_Hand_1SHJnt", "avatar.trackers.leftHand" }, // other options:  l_Hand_1SHJnt / l_Hand_2SHJnt / l_GripPoint_AuxSHJnt
			{ "l_Arm_Elbow_CurveSHJnt", "avatar.trackers.leftElbowGoal" },
			{ "r_Hand_1SHJnt", "avatar.trackers.rightHand" }, // other options: r_Hand_1SHJnt / r_Hand_2SHJnt / r_GripPoint_AuxSHJnt
			{ "r_Arm_Elbow_CurveSHJnt", "avatar.trackers.rightElbowGoal" },
			{ "l_Leg_AnkleSHJnt", "avatar.trackers.leftFoot" }, // other options:  l_Leg_BallSHJnt
			{ "l_Leg_KneeSHJnt", "avatar.trackers.leftKneeGoal" },
			{ "r_Leg_AnkleSHJnt", "avatar.trackers.rightFoot" }, // other options: r_Leg_BallSHJnt
			{ "r_Leg_KneeSHJnt", "avatar.trackers.rightKneeGoal" },
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

			__instance.gameObject.AddComponent<PathfinderRigidTransformSet>();
			
			foreach (var renderer in __instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				var rendererObject = renderer.gameObject;
				var isHeadObject = faceObjectNames.Contains(rendererObject.name);

				if (isHeadObject)
				{
					bodyRendererCopyEnabledState.headRenderers.Add(renderer);
					rendererObject.SetActive(true);
				}
				rendererObject.layer = (int) GameLayer.LivOnly;
			}

			var skeleton = __instance.transform.Find("SHJntGrp");
			var children = skeleton.GetComponentsInChildren<Transform>();

			foreach (var child in children)
			{
				if (boneMap.ContainsKey(child.name))
				{
					var rigidTransformSet = child.gameObject.AddComponent<PathfinderRigidTransformSet>();
					rigidTransformSet.Key = boneMap[child.name];
					rigidTransformSet.Root = __instance.transform;
				}
				
				if (child.gameObject.GetComponent<BoneText>()) continue;
				child.gameObject.AddComponent<BoneText>();
			}
		}
		
		[HarmonyPostfix]
		[HarmonyPatch(typeof(RigEvent), "Awake")]
		private static void HideHeadEffectsFromLiv(RigEvent __instance)
		{
			__instance.gameObject.layer = (int) GameLayer.Player;
		}
	}
}