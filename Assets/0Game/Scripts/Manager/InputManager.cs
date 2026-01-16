using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class InputManager : Singleton<InputManager>
{
	InputActionsAsset InputActions;

	Direction4? _DashPressed;
	float _DashPressedTime;

	public Vector2 Movement => InputActions.Battle.Movement.ReadValue<Vector2>();
	public Vector2 Look => InputActions.Battle.Look.ReadValue<Vector2>();
	public event Action<Direction4> Dash;
	public InputAction Jump => InputActions.Battle.Jump;
	public InputAction NormalAttack => InputActions.Battle.NormalAttack;
	public InputAction Guard => InputActions.Battle.Guard;

	protected override void Init()
	{
		InputActions = new InputActionsAsset();
		InputActions.Enable();
	}

	void Update()
	{
		bool forwardPressed = InputActions.Battle.DashForward.WasPressedThisFrame();
		bool backwardPressed = InputActions.Battle.DashBackward.WasPressedThisFrame();
		bool leftPressed = InputActions.Battle.DashLeft.WasPressedThisFrame();
		bool rightPressed = InputActions.Battle.DashRight.WasPressedThisFrame();

		if (InputActions.Battle.DashForward.WasPressedThisFrame())
		{
			InvokeDash(Direction4.Up);
		}
		else if (InputActions.Battle.DashBackward.WasPressedThisFrame())
		{
			InvokeDash(Direction4.Down);
		}
		else if (InputActions.Battle.DashLeft.WasPressedThisFrame())
		{
			InvokeDash(Direction4.Left);
		}
		else if (InputActions.Battle.DashRight.WasPressedThisFrame())
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

		if (InputActions.Battle.DashForward.WasPressedThisFrame())
		{
			_DashPressed = Direction4.Up;
			_DashPressedTime = Time.time;
		}
		else if (InputActions.Battle.DashBackward.WasPressedThisFrame())
		{
			_DashPressed = Direction4.Down;
			_DashPressedTime = Time.time;
		}
		else if (InputActions.Battle.DashLeft.WasPressedThisFrame())
		{
			_DashPressed = Direction4.Left;
			_DashPressedTime = Time.time;
		}
		else if (InputActions.Battle.DashRight.WasPressedThisFrame())
		{
			_DashPressed = Direction4.Right;
			_DashPressedTime = Time.time;
		}
	}

}
