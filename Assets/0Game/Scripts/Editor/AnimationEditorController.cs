using Animancer;
using Battle;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static DataManager;
using static DataManager.Character;
using static SingletonManager;
using Character = DataManager.Character;

public class AnimationEditorController : MonoBehaviour
{
	public List<Button> _Buttons;

	void Start()
	{
		foreach (Button button in _Buttons)
		{
			string text = button.GetComponentInChildren<TMP_Text>().text;
			button.onClick.AddListener(() => FacialButton(text));
		}
	}

	void FacialButton(string name)
	{
		SkinnedMeshRenderer renderer = GameObject.Find("Astar").transform.Find("Face").GetComponent<SkinnedMeshRenderer>();
		for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
		{
			renderer.SetBlendShapeWeight(i, 0f);
		}
		Facial facial = Data.GetCharacter("Astar").GetFacial(name);
		foreach (Facial.BlendShape blendShape in facial._BlendShapes)
		{
			int blendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(blendShape._BlendShapeName);
			renderer.SetBlendShapeWeight(blendShapeIndex, blendShape._Value);
		}
	}
}
