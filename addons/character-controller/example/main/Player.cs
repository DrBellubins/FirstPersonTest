using System;
using Godot;

public partial class Player : FPSController3D
{
    [Export] public Camera3D Camera;
    [Export] public WeaponSystem WeaponSystem;
	[Export] public float ZoomFOV;
	[Export] public string InputBackActionName { get; set; } = "move_backward";
	[Export] public string InputForwardActionName { get; set; } = "move_forward";
	[Export] public string InputLeftActionName { get; set; } = "move_left";
	[Export] public string InputRightActionName { get; set; } = "move_right";
	[Export] public string InputSprintActionName { get; set; } = "move_sprint";
	[Export] public string InputJumpActionName { get; set; } = "move_jump";
	[Export] public string InputCrouchActionName { get; set; } = "move_crouch";
	[Export] public string InputFlyModeActionName { get; set; } = "move_fly_mode";
	[Export] public Godot.Environment UnderwaterEnv { get; set; }

	public bool IsDriving = false;
	public bool IsFrozen = false;

	private float _normalFov;

    public CollisionShape3D collider;

	public override void _Ready()
	{
        collider = (CollisionShape3D)GetChild(0);

        Input.MouseMode = Input.MouseModeEnum.Captured;
		Setup();
		Emerged += OnControllerEmerged;
		Submerged += OnControllerSubemerged;

		_normalFov = camera.Fov;
	}

	public override void _Process(double delta)
	{
        Camera.Current = !IsDriving;

        if (!IsDriving)
		{
            //Debug.Write($"Position: {Position}");

            // Controller
            bool IsValidInput = Input.MouseMode == Input.MouseModeEnum.Captured;

            if (IsValidInput)
            {
                if (Input.IsActionJustPressed(InputFlyModeActionName))
                    FlyAbility.SetActive(!FlyAbility.IsActived());

                collider.Disabled = FlyAbility.IsActived();

                Vector2 InputAxis = Vector2.Zero;

                if (Input.IsActionPressed("move_right") || Input.IsActionPressed("move_left") ||
                    Input.IsActionPressed("move_forward") || Input.IsActionPressed("move_backward"))
                {
                    InputAxis = new Vector2(Input.GetActionStrength("move_forward") - Input.GetActionStrength("move_backward"),
                        Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left"));
                }

                //Vector2 InputAxis = Input.GetVector(InputBackActionName, InputForwardActionName, InputLeftActionName, InputRightActionName);
                bool InputJump = Input.IsActionJustPressed(InputJumpActionName);
                bool InputCrouch = Input.IsActionPressed(InputCrouchActionName);
                bool InputSprint = Input.IsActionPressed(InputSprintActionName);
                bool InputSwimDown = Input.IsActionPressed(InputCrouchActionName);
                bool InputSwimUp = Input.IsActionPressed(InputJumpActionName);

                if (!IsFrozen)
                    Move((float)delta, InputAxis, InputJump, InputCrouch, InputSprint, InputSwimDown, InputSwimUp);
            }
            else
            {
                Move((float)delta);
            }

            // Camera
            float deltaTime = (float)delta;

            if (Input.IsActionPressed("zoom"))
                camera.Fov = Mathf.Lerp(camera.Fov, ZoomFOV, deltaTime * FovChangeSpeed);
            else
                camera.Fov = Mathf.Lerp(camera.Fov, _normalFov, deltaTime * FovChangeSpeed);

            if (Input.IsActionPressed("look_right") || Input.IsActionPressed("look_left") ||
                Input.IsActionPressed("look_up") || Input.IsActionPressed("look_down"))
            {
                var axis = new Vector2(Input.GetActionStrength("look_right") - Input.GetActionStrength("look_left"),
                    Input.GetActionStrength("look_down") - Input.GetActionStrength("look_up")) * 15f;

                RotateHead(axis, true);
            }
        }
	}

	/*public override void _Process(double delta)
	{
        Camera.Current = !IsDriving;

        if (!IsDriving)
		{
            float deltaTime = (float)delta;

            if (Input.IsActionPressed("zoom"))
                camera.Fov = Mathf.Lerp(camera.Fov, ZoomFOV, deltaTime * FovChangeSpeed);
            else
                camera.Fov = Mathf.Lerp(camera.Fov, _normalFov, deltaTime * FovChangeSpeed);

            if (Input.IsActionPressed("look_right") || Input.IsActionPressed("look_left") ||
                Input.IsActionPressed("look_up") || Input.IsActionPressed("look_down"))
            {
                var axis = new Vector2(Input.GetActionStrength("look_right") - Input.GetActionStrength("look_left"),
                    Input.GetActionStrength("look_down") - Input.GetActionStrength("look_up")) * 15f;

                RotateHead(axis, true);
            }
        }
    }*/

	public override void _Input(InputEvent _event)
	{
		if (!IsDriving)
		{
            // Mouse look (only if the mouse is captured).
            if (_event is InputEventMouseMotion eventMouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                RotateHead(eventMouseMotion.Relative, false);
            }
        }
	}

	private void OnControllerEmerged()
	{
		camera.Environment = null;
	}

	private void OnControllerSubemerged()
	{
		camera.Environment = UnderwaterEnv;
	}
}
