using DG.Tweening;
using UnityEngine;

public class ParticleTimeSetter : MonoBehaviour
{
	public float Value1Time, Value2Time;

	Material ThisParticleMaterial;
	float Value1, Value2;

	void Start()
	{
		ThisParticleMaterial = GetComponent<Renderer>().material;
		DOTween.To(() => Value1, x => Value1 = x, 1f, Value1Time).SetEase(Ease.Linear).SetLoops(-1).OnUpdate(() =>
		{
			ThisParticleMaterial.SetFloat("_Value1", Value1);
		});
		DOTween.To(() => Value2, x => Value2 = x, 10f, Value2Time).SetEase(Ease.Linear).SetLoops(-1).OnUpdate(() =>
		{
			ThisParticleMaterial.SetFloat("_Value2", Value2);
		});
	}
}
