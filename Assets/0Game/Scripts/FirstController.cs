using System.Collections;
using UnityEngine;
using static SingletonManager;

public class FirstController : MonoBehaviour
{
	void Start()
	{
		Game.LoadTitleScene();
	}
}
