using System;
using LIV.SDK.Unity;
using MelonLoader;
using StressLevelZero.Pool;
using StressLevelZero.Rig;
using UnhollowerRuntimeLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BoneworksLIV
{
	public class BoneworksLivMod : MelonMod
	{
		public static Action<Camera> PlayerReady;
		private AssetManager assetManager;
		private GameObject livObject;
		private Camera spawnedCamera;
		private LIV.SDK.Unity.LIV livInstance => LIV.SDK.Unity.LIV.Instance;

		public override void OnApplicationStart()
		{
			base.OnApplicationStart();
			assetManager = new AssetManager($@"{MelonUtils.BaseDirectory}\Mods\LIVAssets\");
			var livAssetBundle = assetManager.LoadBundle("liv-shaders");

			MelonLogger.Msg("### livAssetBundle exists? " + (livAssetBundle != null));
			ClassInjector.RegisterTypeInIl2Cpp<LIV.SDK.Unity.LIV>();
			SDKShaders.LoadFromAssetBundle(livAssetBundle);
			PlayerReady += SetUpLiv;

			SystemLibrary.LoadLibrary($@"{MelonUtils.BaseDirectory}\Mods\LIVAssets\LIV_Bridge.dll");
		}

		public override void OnUpdate()
		{
			base.OnUpdate();
			if (Input.GetKeyDown(KeyCode.F3))
			{
				SetUpLiv(Camera.main);
			}

			// SetUpLiv(Camera.main);

			// TODO: Allow using spectator camera mods to control the LIV camera. Disabling for now.
			// UpdateSpectatorCameras();
		}

		private void UpdateSpectatorCameras()
		{
			if (livInstance == null || livInstance.render == null || spawnedCamera == null) return;
			var cameraTransform = spawnedCamera.transform;
			livInstance.render.SetPose(cameraTransform.position, cameraTransform.rotation, spawnedCamera.fieldOfView);
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
			MelonLogger.Msg("2");
			if (!camera)
			{
				MelonLogger.Msg("No Camera, returning");
				return;
			}

			MelonLogger.Msg("3");
			var livCamera = GetLivCamera();
			MelonLogger.Msg($"LIV Camera {(livCamera ? livCamera.name : "none")}");
			MelonLogger.Msg($"camera {(camera ? camera.name : "none")}");
			if (livCamera == camera)
			{
				MelonLogger.Msg("LIV already active, returning");
				return;
			}

			MelonLogger.Msg("Setting up LIV...");
			if (livObject)
			{
				Object.Destroy(livObject);
			}


			MelonLogger.Msg("4");
			var cameraParent = camera.transform.parent;


			MelonLogger.Msg("5");
			var cameraPrefab = new GameObject("LivCameraPrefab");
			cameraPrefab.SetActive(false);
			cameraPrefab.AddComponent<Camera>();
			cameraPrefab.transform.SetParent(cameraParent, false);

			MelonLogger.Msg("6");
			livObject = new GameObject("LIV");
			livObject.SetActive(false);

			MelonLogger.Msg("7");
			var liv = livObject.AddComponent<LIV.SDK.Unity.LIV>();
			liv.HMDCamera = camera;
			liv.MRCameraPrefab = cameraPrefab.GetComponent<Camera>();
			liv.stage = cameraParent;
			liv.fixPostEffectsAlpha = true;
			liv.spectatorLayerMask = camera.cullingMask & ~(1 << LayerMask.NameToLayer("Player")) | 1 << 31;
			livObject.SetActive(true);

			MelonLogger.Msg("8");
			var skeletonRig = Object.FindObjectOfType<GameWorldSkeletonRig>();

			MelonLogger.Msg("9");
			// TODO: Add option for enabling stencil mask. Disabled for now.
			// SetUpStencilMask(skeletonRig);

			var renderers = skeletonRig.gameObject.GetComponentsInChildren<Renderer>();
			foreach (var renderer in renderers)
			{
				renderer.gameObject.layer = LayerMask.NameToLayer("Player");
			}

			// TODO: Allow using spectator camera mods to control the LIV camera. Disabling for now.
			// spawnedCamera = GetSpawnedCamera();
		}

		private void SetUpStencilMask(GameWorldSkeletonRig skeletonRig)
		{
			var stencilMaskBundle = assetManager.LoadBundle("stencil-mask");
			var stencilMaskShader = stencilMaskBundle.LoadAsset<Shader>("StencilStandard");
			stencilMaskShader.hideFlags |= HideFlags.DontUnloadUnusedAsset;

			var renderers = skeletonRig.gameObject.GetComponentsInChildren<Renderer>();
			foreach (var renderer in renderers)
			{
				foreach (var material in renderer.materials)
				{
					MelonLogger.Msg($"Replacing shader {material.shader.name}");
					material.shader = stencilMaskShader;
				}
			}

			var stencilMaskCapsulePrefab = stencilMaskBundle.LoadAsset<GameObject>("StencilCapsule");
			var stencilMaskCapsule = Object.Instantiate(stencilMaskCapsulePrefab, skeletonRig.transform, false);
			stencilMaskCapsule.layer = 31; // 31 is visible to liv but not player.
			Object.Destroy(stencilMaskCapsule.GetComponent<Collider>());
			stencilMaskCapsule.hideFlags |= HideFlags.DontUnloadUnusedAsset;
		}

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