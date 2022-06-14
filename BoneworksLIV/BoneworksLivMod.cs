using System;
using LIV.SDK.Unity;
using MelonLoader;
using StressLevelZero.Pool;
using UnhollowerRuntimeLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BoneworksLIV
{
	public class BoneworksLivMod : MelonMod
	{
		public static Action<Camera> PlayerReady;
		private GameObject livObject;
		private Camera spawnedCamera;
		private LIV.SDK.Unity.LIV livInstance => LIV.SDK.Unity.LIV.Instance;

		public override void OnApplicationStart()
		{
			base.OnApplicationStart();

			SetUpLiv();
			ClassInjector.RegisterTypeInIl2Cpp<LIV.SDK.Unity.LIV>();
			PlayerReady += SetUpLiv;
		}

		public override void OnUpdate()
		{
			base.OnUpdate();
			if (Input.GetKeyDown(KeyCode.F3))
			{
				SetUpLiv(Camera.main);
			}

			// TODO: Allow using spectator camera mods to control the LIV camera. Disabling for now.
			// UpdateSpectatorCameras();
		}

		private void UpdateSpectatorCameras()
		{
			if (livInstance == null || livInstance.render == null || spawnedCamera == null) return;
			var cameraTransform = spawnedCamera.transform;
			livInstance.render.SetPose(cameraTransform.position, cameraTransform.rotation, spawnedCamera.fieldOfView);
		}

		private static void SetUpLiv()
		{
			// Since the mod manager doesn't copy stuff to the game directory,
			// we're loading the dll manually from the mod directory,
			// to make sure DllImport works as expected in the LIV SDK.
			SystemLibrary.LoadLibrary($@"{MelonUtils.BaseDirectory}\Mods\LIVAssets\LIV_Bridge.dll");

			var assetManager = new AssetManager($@"{MelonUtils.BaseDirectory}\Mods\LIVAssets\");
			var livAssetBundle = assetManager.LoadBundle("liv-shaders");
			SDKShaders.LoadFromAssetBundle(livAssetBundle);
		}

		private Camera GetLivCamera()
		{
			try
			{
				return !livInstance ? null : livInstance.HMDCamera;
			}
			catch (Exception)
			{
				MelonLogger.Msg("ObjectCollectedException");
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
			cameraPrefab.AddComponent<Camera>();
			cameraPrefab.transform.SetParent(cameraParent, false);

			livObject = new GameObject("LIV");
			livObject.SetActive(false);

			var liv = livObject.AddComponent<LIV.SDK.Unity.LIV>();
			liv.HMDCamera = camera;
			liv.MRCameraPrefab = cameraPrefab.GetComponent<Camera>();
			liv.stage = cameraParent;
			liv.fixPostEffectsAlpha = true;
			liv.spectatorLayerMask = camera.cullingMask & ~(1 << (int) GameLayer.Player);
			livObject.SetActive(true);
		}

		// TODO: Allow using spectator camera mods to control the LIV camera. Disabling for now.
		private Camera GetSpawnedCamera()
		{
			if (!PoolManager._instance) return null;

			var pools = PoolManager._instance.GetComponentsInChildren<Pool>();
			foreach (var pool in pools)
			{
				foreach (var spawnedObject in pool._spawnedObjects)
				{
					var camera = spawnedObject.GetComponentInChildren<Camera>();
					if (camera)
					{
						return camera;
					}
				}
			}
			return null;
		}
	}
}