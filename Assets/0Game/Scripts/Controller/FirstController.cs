using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SingletonManager;

public class FirstController : MonoBehaviour
{
	public SingletonManager loader;

	void Start()
	{
		StartCoroutine(UI.SceneTransition("Editing"));
	}
}
