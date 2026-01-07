using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
	InputActionsAsset InputActions;

	protected override void Init()
	{
		InputActions = new InputActionsAsset();
		InputActions.Enable();
	}

	void Update()
	{
		
	}

	public Vector2 Movement => InputActions.Gameplay.Movement.ReadValue<Vector2>();
	public Vector2 Look => InputActions.Gameplay.Look.ReadValue<Vector2>();
	public InputAction Jump => InputActions.Gameplay.Jump;
	public InputAction NormalAttack => InputActions.Gameplay.NormalAttack;
	public InputAction Guard => InputActions.Gameplay.Guard;

}
