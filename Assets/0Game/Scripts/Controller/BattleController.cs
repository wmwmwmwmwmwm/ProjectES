using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Battle
{
	public class BattleController : MonoBehaviour
	{
		[ReadOnly] public Character PlayerCharacter;
		public GameObject CubePrefab;
		Transform MainCamera, BackgroundCamera;
		BoxCollider SpawnPlane;
		[ReadOnly] public List<GameObject> Cubes;

		void Start()
		{
			Cubes = new List<GameObject>();
			SpawnPlane = GameObject.Find("CubeSpawnArea").GetComponent<BoxCollider>();
			MainCamera = Camera.main.transform;
			BackgroundCamera = GameObject.Find("BackgroundCamera").transform;
			SpawnCubes();
		}

		void SpawnCubes()
		{
			for (int i = 0; i < 20; i++)
			{
				SpawnCube();
			}
			StartCoroutine(SpawnCoroutine());
			IEnumerator SpawnCoroutine()
			{
				while (true)
				{
					yield return new WaitForSeconds(1f);
					if (Cubes.Count < 20) SpawnCube();
				}
			}
			void SpawnCube()
			{
				Vector3 NewSpawnPosition = new Vector3(Random.Range(SpawnPlane.bounds.min.x, SpawnPlane.bounds.max.x), 0.8f, Random.Range(SpawnPlane.bounds.min.z, SpawnPlane.bounds.max.z));
				GameObject NewCubeObject = Instantiate(CubePrefab, NewSpawnPosition, Quaternion.identity);
				Cubes.Add(NewCubeObject);
			}
		}

		void Update()
		{
			BackgroundCamera.rotation = MainCamera.rotation;
		}
	}
}
