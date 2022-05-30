using LIV.SDK.Unity;
using MelonLoader;

namespace BoneworksLIV
{
	public class BoneworksLivPlugin: MelonMod
	{
		public override void OnApplicationStart()
		{
			base.OnApplicationStart();
			SDKShaders.LoadFromAssetBundle(AssetManager.LoadBundle("liv-shaders"));
		}
	}
}