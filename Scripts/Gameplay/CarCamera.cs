using Godot;
using System;

public partial class CarCamera : Camera3D
{
    [Export] public float Sensitivity = 2.0f;
    [Export] public float ReturnTime = 1.0f;
    [Export] public float ReturnSpeed = 1.0f;
    [Export] public float ViewBob = 0.05f;
    [Export] public float ViewBobMax = 0.35f;
    [Export] public float VerticalAngleLimit = 90.0f;
    [Export] public float HorizontalAngleLimit = 90.0f;

    private VehicleBody3D car;
	private Vector3 rotation = new Vector3();
    private float returnTimer = 0f;

    public override void _Ready()
	{
        car = GetOwner<VehicleBody3D>();
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

	public override void _Process(double delta)
	{
        float deltaTime = (float)delta;

        returnTimer += deltaTime;

        if (returnTimer > ReturnTime)
            rotation = rotation.Lerp(Vector3.Zero, ReturnSpeed * deltaTime);

        Position = new Vector3(Position.X, Mathf.Clamp(car.LinearVelocity.Y * ViewBob, 
            -ViewBobMax, ViewBobMax), Position.Z);

        if (Input.IsActionPressed("look_left") || Input.IsActionPressed("look_right") ||
            Input.IsActionPressed("look_up") || Input.IsActionPressed("look_down"))
        {
            var axis = new Vector2(Input.GetAxis("look_left", "look_right"), Input.GetAxis("look_up", "look_down")) * 15f;
            RotateCamera(axis);

            returnTimer = 0f;
        }

        Rotation = new Vector3(rotation.X, rotation.Y, Rotation.Z);
    }

    public override void _Input(InputEvent _event)
    {
        // Mouse look (only if the mouse is captured).
        if (_event is InputEventMouseMotion eventMouseMotion
            && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            RotateCamera(eventMouseMotion.Relative);
            returnTimer = 0f;
        }
    }

    public void RotateCamera(Vector2 inputAxis)
    {
        // Horizontal mouse look.
        rotation.Y = Mathf.Clamp(rotation.Y - inputAxis.X * (Sensitivity / 1000),
            -Mathf.DegToRad(HorizontalAngleLimit), Mathf.DegToRad(HorizontalAngleLimit));

        // Vertical mouse look.
        rotation.X = Mathf.Clamp(rotation.X - inputAxis.Y * (Sensitivity / 1000),
            -Mathf.DegToRad(VerticalAngleLimit), Mathf.DegToRad(VerticalAngleLimit));

        Rotation = new Vector3(rotation.X, rotation.Y, Rotation.Z);
    }
}