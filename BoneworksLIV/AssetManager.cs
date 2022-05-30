using System;
using System.IO;
using UnityEngine;

namespace BoneworksLIV
{
	public static class AssetManager
	{
		private static readonly string assetsDir =  MelonLoader.MelonUtils.UserDataDirectory + "/LIVAssets/";

		public static AssetBundle LoadBundle(string assetName)
		{
			var bundle = AssetBundle.LoadFromFile(assetsDir +	assetName);

			if (bundle == null)
			{
				throw new Exception("Failed to load asset bundle " + assetName);
			}
			
			return bundle;
		}
	}
}