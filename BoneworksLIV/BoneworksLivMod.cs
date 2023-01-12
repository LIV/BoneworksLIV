using System;
using LIV.SDK.Unity;
using LIV.SDK.Unity.Volumetric.GameSDK;
using MelonLoader;
using StressLevelZero.Pool;
using UnhollowerRuntimeLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BoneworksLIV
{
	public class BoneworksLivMod : MelonMod
	{
		public static Action<Camera> OnCameraReady;

		private GameObject livObject;
		private ModSettings modSettings;
		private Camera spawnedCamera;
		private static LIV.SDK.Unity.LIV livInstance => LIV.SDK.Unity.LIV.Instance;

		public override void OnApplicationStart()
		{
			base.OnApplicationStart();

			SetUpLiv();
			ClassInjector.RegisterTypeInIl2Cpp<LIV.SDK.Unity.LIV>();
			ClassInjector.RegisterTypeInIl2Cpp<BodyRendererManager>();
			ClassInjector.RegisterTypeInIl2Cpp<VolumetricGameSDK>();
			OnCameraReady += SetUpLiv;
			modSettings = new ModSettings();
			modSettings.ShowPlayerBody.OnValueChanged += HandleShowPlayerBodyChanged;
			
			SystemLibrary.LoadLibrary($@"{MelonUtils.BaseDirectory}\Mods\LIVAssets\LIV_Bridge.dll");
			
			// TODO this needs to be loaded from the Plugins folder and added to preladed plugins in globalgamemanagers.
			// SystemLibrary.LoadLibrary($@"{MelonUtils.BaseDirectory}\Mods\LIVAssets\LIV_VOLCAP.dll");
		}

		private static void HandleShowPlayerBodyChanged(bool oldShowPlayerBody, bool newShowPlayerBody)
		{
			SetUpPlayerVisibility(livInstance, newShowPlayerBody);
		}

		// Changes the LIV layer mask to toggle the Boneworks player model visibility on LIV's camera.
		private static void SetUpPlayerVisibility(LIV.SDK.Unity.LIV liv, bool showPlayerBody)
		{
			if (liv == null)
			{
				MelonLogger.Error("Tried to set up player visibility but LIV instance is null");
				return;
			}

			if (showPlayerBody)
			{
				liv.spectatorLayerMask |= 1 << (int) GameLayer.Player;
				liv.spectatorLayerMask |= 1 << (int) GameLayer.LivOnly;
			}
			else
			{
				liv.spectatorLayerMask &= ~(1 << (int) GameLayer.Player);
				liv.spectatorLayerMask &= ~(1 << (int) GameLayer.LivOnly);
			}
		}

		public override void OnUpdate()
		{
			base.OnUpdate();
			if (Input.GetKeyDown(KeyCode.F3))
			{
				SetUpSpawnedCamera();
			}

			UpdateFollowSpawnedCamera();
		}

		private void UpdateFollowSpawnedCamera()
		{
			var livRender = GetLivRender();
			if (livRender == null || spawnedCamera == null) return;

			// When spawned objects get removed in Boneworks, they might not be destroyed and just be disabled.
			if (!spawnedCamera.gameObject.activeInHierarchy)
			{
				spawnedCamera = null;
				return;
			}

			var cameraTransform = spawnedCamera.transform;
			livRender.SetPose(cameraTransform.position, cameraTransform.rotation, spawnedCamera.fieldOfView);
		}

		private static void SetUpLiv()
		{
			var assetManager = new AssetManager($@"{MelonUtils.BaseDirectory}\Mods\LIVAssets\");
			var livAssetBundle = assetManager.LoadBundle("liv-shaders");
			SDKShaders.LoadFromAssetBundle(livAssetBundle);
		}

		private static Camera GetLivCamera()
		{
			try
			{
				return !livInstance ? null : livInstance.HMDCamera;
			}
			catch (Exception)
			{
				LIV.SDK.Unity.LIV.Instance = null;
			}
			return null;
		}


		private static SDKRender GetLivRender()
		{
			try
			{
				return !livInstance ? null : livInstance.render;
			}
			catch (Exception)
			{
				LIV.SDK.Unity.LIV.Instance = null;
			}
			return null;
		}

		private void SetUpLiv(Camera camera)
		{
			if (!camera)
			{
				MelonLogger.Msg("No camera provided, aborting LIV setup.");
				return;
			}

			var livCamera = GetLivCamera();
			if (livCamera == camera)
			{
				MelonLogger.Msg("LIV already set up with this camera, aborting LIV setup.");
				return;
			}

			MelonLogger.Msg($"Setting up LIV with camera {camera.name}...");
			if (livObject)
			{
				Object.Destroy(livObject);
			}

			var cameraParent = camera.transform.parent;
			var cameraPrefab = new GameObject("LivCameraPrefab");
			cameraPrefab.SetActive(false);
			var cameraFromPrefab = cameraPrefab.AddComponent<Camera>();
			cameraFromPrefab.allowHDR = false;
			cameraPrefab.transform.SetParent(cameraParent, false);

			livObject = new GameObject("LIV");
			livObject.SetActive(false);

			var liv = livObject.AddComponent<LIV.SDK.Unity.LIV>();
			liv.HMDCamera = camera;
			liv.MRCameraPrefab = cameraFromPrefab;
			liv.stage = cameraParent;
			liv.fixPostEffectsAlpha = true;
			SetUpPlayerVisibility(liv, modSettings.ShowPlayerBody.Value);
			
			var volumetricGameSDK = livObject.AddComponent<VolumetricGameSDK>();
			volumetricGameSDK.stage = cameraParent;
			volumetricGameSDK.HMDCamera = cameraFromPrefab;
			volumetricGameSDK.spectatorLayerMask = camera.cullingMask | 1 << (int) GameLayer.LivOnly;

			livObject.SetActive(true);
		}

		private void SetUpSpawnedCamera()
		{
			if (!PoolManager._instance) return;

			var pools = PoolManager._instance.GetComponentsInChildren<Pool>();
			foreach (var pool in pools)
			{
				foreach (var spawnedObject in pool._spawnedObjects)
				{
					var camera = spawnedObject.GetComponentInChildren<Camera>();
					if (!camera) continue;

					spawnedCamera = camera;

					// Disable the spawned camera to reduce performance hit.
					spawnedCamera.enabled = false;

					return;
				}
			}
		}
	}
}