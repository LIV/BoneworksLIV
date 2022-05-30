using System;
using System.IO;
using MelonLoader;
using UnityEngine;

namespace BoneworksLIV
{
	public static class AssetManager
	{
		private static readonly string assetsDir = @"C:\Users\ricar\AppData\Roaming\r2modmanPlus-local\BONEWORKS\profiles\Default\Mods\LIVAssets\";

		public static AssetBundle LoadBundle(string assetName)
		{
			var bundlePath = Path.Combine(assetsDir, assetName);
			// var bundle = AssetBundle.LoadFromFile(Path.Combine(MelonLoader.MelonUtils.BaseDirectory, assetsDir, assetName));
			var bundle = AssetBundle.LoadFromFile(bundlePath);
			
			MelonLogger.Msg("### Loading bundle from " + bundlePath);
				
			if (bundle == null)
			{
				throw new Exception("Failed to load asset bundle " + bundlePath);
			}
			
			return bundle;
		}
	}
}