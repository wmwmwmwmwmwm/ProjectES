using Battle;
using KinematicCharacterController;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SingletonManager;

public class RedirectRootMotionToCharacter : MonoBehaviour
{
	public Animator _Animator;
	public Character _Character;

	void OnAnimatorMove()
	{
		_Character._RootMotionPosDelta += _Animator.deltaPosition;
		_Character._RootMotionRotDelta *= _Animator.deltaRotation;
	}
}
