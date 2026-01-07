using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;
using static SingletonManager;

public class TestCube : MonoBehaviour
{
	[ReadOnly] public Vector3 MoveVelocity;
	public int HP;

	CharacterController ThisController;

	void Start()
	{
		ThisController = GetComponent<CharacterController>();
		transform.DOMoveY(transform.position.y + 0.2f, 1.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
	}

	public void DamageDealt()
	{
		HP--;
		if (HP == 0)
		{
			StartCoroutine(DieCoroutine());
		}
		IEnumerator DieCoroutine()
		{
			ThisController.enabled = false;
			transform.DORotate(transform.eulerAngles.WithY(transform.eulerAngles.x + 1440f), 1.2f, RotateMode.FastBeyond360).SetEase(Ease.OutCirc);
			transform.DOMoveY(transform.position.y + 1.8f, 1.2f);
			Color EmissionColor = GetComponent<MeshRenderer>().material.GetColor("_EmissionColor");
			GetComponent<MeshRenderer>().material.DOColor(EmissionColor * 5f, "_EmissionColor", 1.2f);
			yield return GetComponent<MeshRenderer>().material.DOFloat(-0.5f, "_Dissolve", 1.2f).WaitForCompletion();
			//Game.Cubes.Remove(gameObject);
			Destroy(gameObject);
		}
	}

	void Update()
	{
		MoveVelocity = Vector3.Lerp(MoveVelocity, Vector3.zero, 10f * Time.deltaTime);
		if (ThisController.enabled) ThisController.Move(Time.deltaTime * MoveVelocity);
	}
}
