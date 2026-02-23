using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class InputManager : Singleton<InputManager>
{
	InputActionsAsset InputActions;

	Direction4? _DashPressed;
	float _DashPressedTime;

	public InputAction Forward => InputActions.Battle.Forward;
	public InputAction Backward => InputActions.Battle.Backward;
	public InputAction Left => InputActions.Battle.Left;
	public InputAction Right => InputActions.Battle.Right;
	public InputAction Movement => InputActions.Battle.Movement;
	public InputAction Look => InputActions.Battle.Look;
	public event Action<Direction4> Dash;
	public InputAction Jump => InputActions.Battle.Jump;
	public InputAction Guard => InputActions.Battle.Guard;
	public InputAction NormalAttack => InputActions.Battle.NormalAttack;
	public InputAction SpecialAttack => InputActions.Battle.SpecialAttack;
	public InputAction Skill1 => InputActions.Battle.Skill1;
	public InputAction Skill2 => InputActions.Battle.Skill2;
	public InputAction Ultimate => InputActions.Battle.Ultimate;
	public InputAction Character1 => InputActions.Battle.Character1;
	public InputAction Character2 => InputActions.Battle.Character2;

	protected override void Init()
	{
		InputActions = new InputActionsAsset();
		InputActions.Enable();
	}

	void Update()
	{
		bool forwardPressed = Forward.WasPressedThisFrame();
		bool backwardPressed = Backward.WasPressedThisFrame();
		bool leftPressed = Left.WasPressedThisFrame();
		bool rightPressed = Right.WasPressedThisFrame();

		if (forwardPressed)
		{
			InvokeDash(Direction4.Up);
		}
		else if (backwardPressed)
		{
			InvokeDash(Direction4.Down);
		}
		else if (leftPressed)
		{
			InvokeDash(Direction4.Left);
		}
		else if (rightPressed)
		{
			InvokeDash(Direction4.Right);
		}

		void InvokeDash(Direction4 dir)
		{
			if (_DashPressed == dir && Time.time - _DashPressedTime < 0.3f)
			{
				Dash.Invoke(dir);
			}
			else
			{
				_DashPressed = null;
			}
		}

		if (forwardPressed)
		{
			_DashPressed = Direction4.Up;
			_DashPressedTime = Time.time;
		}
		else if (backwardPressed)
		{
			_DashPressed = Direction4.Down;
			_DashPressedTime = Time.time;
		}
		else if (leftPressed)
		{
			_DashPressed = Direction4.Left;
			_DashPressedTime = Time.time;
		}
		else if (rightPressed)
		{
			_DashPressed = Direction4.Right;
			_DashPressedTime = Time.time;
		}
	}

}
