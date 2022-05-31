
using LIV.SDK.Unity;
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
			SDKShaders.LoadFromAssetBundle(livAssetBundle);

		}

		private LIV.SDK.Unity.LIV liv;
		private GameObject livObject;

		public override void OnUpdate()
		{
			base.OnUpdate();
			if (Input.GetKeyDown(KeyCode.F3))
			{
				MelonLogger.Msg("Pressed L");
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
		}
	}
}