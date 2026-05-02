using Battle;
using NaughtyAttributes;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Battle.CharacterController_Rigidbody;

public class Fracture : MonoBehaviour
{
	public GameObject _TargetMesh;
	public Transform _FragmentParent;
	public GameObject _FragmentPrefab;
	public FractureOptions _Options;

	[Button("Fragment 생성")]
	public void Button()
	{
		if (!_TargetMesh) return;
		if (Application.isPlaying) return;
		PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
		if (!stage) return;

		// 기존 오브젝트 삭제
		List<Transform> childs = new();
		foreach (Transform child in _FragmentParent)
		{
			childs.Add(child);
		}
		childs.ForEach(x => DestroyImmediate(x.gameObject));

		// 기존 애셋 삭제
		string assetPath = GetAssetPath();
		AssetDatabase.DeleteAsset(assetPath);

		// Fracture
		Fragmenter.Fracture(_TargetMesh,
							_Options,
							_FragmentPrefab,
							_FragmentParent);

		// 생성된 메쉬 모으기
		List<Mesh> meshes = new();
		foreach (Transform child in _FragmentParent)
		{
			// Mesh
			Mesh mesh = child.GetComponent<MeshFilter>().sharedMesh;
			meshes.Add(mesh);

			// Material
			MeshRenderer renderer = child.GetComponent<MeshRenderer>();
			if (!renderer.sharedMaterial)
			{
				Material material = _TargetMesh.GetComponent<MeshRenderer>().sharedMaterial;
				List<Material> materials = new();
				for (int i = 0; i < mesh.subMeshCount; i++)
				{
					materials.Add(material);
				}
				renderer.SetSharedMaterials(materials);
			}
		}

		// 저장
		FragmentAsset mainAsset = GetMainMeshAsset();
		foreach (Mesh mesh in meshes)
		{
			AssetDatabase.AddObjectToAsset(mesh, mainAsset);
		}
		//foreach (Material material in materials)
		//{
		//	AssetDatabase.AddObjectToAsset(material, mainAsset);
		//}
		GetComponent<CharacterController_Rigidbody>()._Fragment = _FragmentParent;
		Util.SetDirty(gameObject);
		AssetDatabase.SaveAssets();
	}

	FragmentAsset GetMainMeshAsset()
	{
		string filePath = GetAssetPath();
		FragmentAsset obj = ScriptableObject.CreateInstance<FragmentAsset>();
		AssetDatabase.CreateAsset(obj, filePath);
		return AssetDatabase.LoadAssetAtPath<FragmentAsset>(filePath);
	}

	string GetAssetPath()
	{
		PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
		string directory = Path.GetDirectoryName(stage.assetPath);
		string fileName = $"{Path.GetFileNameWithoutExtension(stage.assetPath)}_Fragment.asset";
		return Path.Combine(directory, fileName);
	}
}