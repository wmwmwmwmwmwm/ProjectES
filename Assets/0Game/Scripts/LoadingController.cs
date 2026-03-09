using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SingletonManager;

public class LoadingController : SingleInstance<LoadingController>
{
	public Slider _Progress;
	public TMP_Text _Text;
}
