
using LIV.SDK.Unity;
using LIV.SDK.Unity.Volumetric;
using MelonLoader;
using UnhollowerBaseLib.Runtime;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace BoneworksLIV
{
	public class BoneworksLivMod: MelonMod
	{
		public override void OnApplicationStart()
		{
			base.OnApplicationStart();
			var livAssetBundle = AssetManager.LoadBundle("liv-shaders");
			// var shaders = livAssetBundle.LoadAll<Shader>();
			
			MelonLogger.Msg("### livAssetBundle exists? " + (livAssetBundle != null));
			ClassInjector.RegisterTypeInIl2Cpp<LIV.SDK.Unity.LIV>();
			ClassInjector.RegisterTypeInIl2Cpp<VolumetricCapture>();
			SDKShaders.LoadFromAssetBundle(livAssetBundle);

		}

		private LIV.SDK.Unity.LIV liv;
		private GameObject livObject;
		private VolumetricCapture volCap;
		private GameObject volCapObject;

		public override void OnUpdate()
		{
			base.OnUpdate();
			if (Input.GetKeyDown(KeyCode.F3))
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
				livObject.SetActive(true);
			}

			if (Input.GetKeyDown(KeyCode.F4))
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
		}
	}
}