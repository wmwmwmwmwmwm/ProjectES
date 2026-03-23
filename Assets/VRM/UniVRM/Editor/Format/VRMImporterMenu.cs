using System.IO;
using UnityEditor;
using UnityEngine;
using UniGLTF;

namespace VRM
{
	public static class VRMImporterMenu
	{
		public const string MENU_NAME = "Import VRM 0.x...";
		public const string LastOpenFolderKey = "VRMImporterMenu.LastOpenFolder";
		public const string LastSaveFolderKey = "VRMImporterMenu.LastSaveFolder";

		public static void OpenImportMenu()
		{
			string openDir = EditorPrefs.GetString(LastOpenFolderKey);
			string path = EditorUtility.OpenFilePanel(MENU_NAME + ": open vrm", openDir, "vrm");

			if (string.IsNullOrEmpty(path))
			{
				return;
			}

			EditorPrefs.SetString(LastOpenFolderKey, Path.GetDirectoryName(path));
			if (Application.isPlaying)
			{
				// import vrm to scene without asset creation
				ImportRuntime(path);
			}
			else
			{
				// import vrm to asset
				if (path.StartsWithUnityAssetPath())
				{
					UniGLTFLogger.Warning("disallow import from folder under the Assets");
					return;
				}

				string saveDir = EditorPrefs.GetString(LastSaveFolderKey);
				string prefabPath = EditorUtility.SaveFilePanel("save prefab", saveDir, Path.GetFileNameWithoutExtension(path), "prefab");
				if (string.IsNullOrEmpty(path))
				{
					return;
				}

				EditorPrefs.SetString(LastSaveFolderKey, Path.GetDirectoryName(prefabPath));
				vrmAssetPostprocessor.ImportVrmAndCreatePrefab(path, UnityPath.FromFullpath(prefabPath));
			}
		}

		/// <summary>
		/// load into scene
		/// </summary>
		/// <param name="path">vrm path</param>
		static void ImportRuntime(string path)
		{
			using (var data = new GlbFileParser(path).Parse())
			using (var context = new VRMImporterContext(new VRMData(data)))
			{
				var loaded = context.Load();
				loaded.EnableUpdateWhenOffscreen();
				loaded.ShowMeshes();
				Selection.activeGameObject = loaded.gameObject;
			}
		}
	}
}
