using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class RemapFbxMaterial : AssetPostprocessor
{
	static string[] materialNames = new string[]
	{
		"_Body_",
		"_EyeHighlight_",
		"_EyeIris_",
		"_EyeWhite_",
		"_Face_",
		"_FaceBrow_",
		"_FaceEyelash_",
		"_FaceEyeline_",
		"_FaceMouth_",
		"_HairBack_",
		"_Hair_",
		"_CLOTH_01",
		"_CLOTH_02",
		"_Shoes_",
		"_Onepiece_",
	};

	Material OnAssignMaterialModel(Material material, Renderer renderer)
	{
		DirectoryInfo parentPath = Directory.GetParent(assetImporter.assetPath); 
		string[] assetPaths = Directory.GetFiles(parentPath.FullName, "*.asset", SearchOption.AllDirectories);

		foreach (string keyword in materialNames)
		{
			if (material.name.Contains(keyword))
			{
				string path = assetPaths.First(x => x.Contains(keyword));
				string assetPath = Util.ToAssetPath(path);
				Material foundMaterial = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
				if (!foundMaterial) continue;
				return foundMaterial;
			}
		}

		// Eyelash 예외 설정
		if (material.name.Contains("Lit"))
		{
			string path = assetPaths.First(x => x.Contains("_FaceEyelash_"));
			string assetPath = Util.ToAssetPath(path);
			Material foundMaterial = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
			if (foundMaterial)
			{
				return foundMaterial;
			}
		}

		Debug.LogError($"Material을 찾을 수 없음: {material}");
		return material;
	}
}
