using LIV.SDK.Unity;
using LIV.SDK.Unity.Volumetric;
using MelonLoader;
using StressLevelZero.Pool;
using StressLevelZero.Rig;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace BoneworksLIV
{
	public class BoneworksLivMod : MelonMod
	{
		private LIV.SDK.Unity.LIV liv;
		private GameObject livObject;
		private Camera spawnedCamera;
		private VolumetricCapture volCap;
		private GameObject volCapObject;

		public override void OnApplicationStart()
		{
			base.OnApplicationStart();
			var livAssetBundle = AssetManager.LoadBundle("liv-shaders");

			MelonLogger.Msg("### livAssetBundle exists? " + (livAssetBundle != null));
			ClassInjector.RegisterTypeInIl2Cpp<LIV.SDK.Unity.LIV>();
			ClassInjector.RegisterTypeInIl2Cpp<VolumetricCapture>();
			SDKShaders.LoadFromAssetBundle(livAssetBundle);
		}

		public override void OnUpdate()
		{
			base.OnUpdate();
			if (Input.GetKeyDown(KeyCode.F3))
			{
				SetUpLiv();
			}

			if (Input.GetKeyDown(KeyCode.F4))
			{
				SetUpVolcap();
			}

			UpdateSpectatorCameras();
		}

		private void UpdateSpectatorCameras()
		{
			if (liv == null || liv.render == null || spawnedCamera == null) return;
			var cameraTransform = spawnedCamera.transform;
			liv.render.SetPose(cameraTransform.position, cameraTransform.rotation, spawnedCamera.fieldOfView);
		}

		private void SetUpLiv()
		{
			MelonLogger.Msg("Pressed F3");
			if (livObject)
			{
				Object.Destroy(livObject);
			}

			var cameraPrefab = new GameObject("LivCameraPrefab");
			cameraPrefab.SetActive(false);
			cameraPrefab.AddComponent<Camera>();
			cameraPrefab.transform.SetParent(Camera.main.transform.parent, false);

			livObject = new GameObject("LIV");
			livObject.SetActive(false);
			liv = livObject.AddComponent<LIV.SDK.Unity.LIV>();
			liv.HMDCamera = Camera.main;
			liv.MRCameraPrefab = cameraPrefab.GetComponent<Camera>();
			liv.stage = Camera.main.transform.parent;
			liv.fixPostEffectsAlpha = true;
			liv.spectatorLayerMask = Camera.main.cullingMask & ~(1 << LayerMask.NameToLayer("Player")) | 1 << 31;
			livObject.SetActive(true);

			var skeletonRig = Object.FindObjectOfType<GameWorldSkeletonRig>();

			// TODO: Add option for enabling stencil mask. Disabled for now.
			// SetUpStencilMask(skeletonRig);

			var renderers = skeletonRig.gameObject.GetComponentsInChildren<Renderer>();
			foreach (var renderer in renderers)
			{
				renderer.gameObject.layer = LayerMask.NameToLayer("Player");
			}

			spawnedCamera = GetSpawnedCamera();
		}

		private void SetUpVolcap()
		{
			if (volCapObject)
			{
				Object.Destroy(volCapObject);
			}

			var cameraPrefab = new GameObject("LivCameraPrefab");
			cameraPrefab.SetActive(false);
			cameraPrefab.AddComponent<Camera>();
			cameraPrefab.transform.SetParent(Camera.main.transform.parent, false);

			volCapObject = new GameObject("VolCap");
			volCapObject.SetActive(false);
			volCap = volCapObject.AddComponent<VolumetricCapture>();
			volCap.HMDCamera = Camera.main;
			volCap.cameraPrefab = cameraPrefab.GetComponent<Camera>();
			volCap.stage = Camera.main.transform.parent;
			volCapObject.SetActive(true);
		}

		private void SetUpStencilMask(GameWorldSkeletonRig skeletonRig)
		{
			var stencilMaskBundle = AssetManager.LoadBundle("stencil-mask");
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