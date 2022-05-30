
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
			
			SDKShaders.LoadFromAssetBundle(livAssetBundle);

		}

		public override void OnUpdate()
		{
			base.OnUpdate();
			if (Input.GetKeyDown(KeyCode.L))
			{
				var livAssetBundle = AssetManager.LoadBundle("liv-shaders");

				SDKShaders.LoadFromAssetBundle(livAssetBundle);
				// var shaders = livAssetBundle.LoadAll<Shader>();
				// SDKShaders.LoadFromAssetBundle(AssetManager.LoadBundle("liv-shaders"));
			}
		}
	}
}