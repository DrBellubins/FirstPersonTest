using Godot;
using System;

public partial class Car : VehicleBody3D
{
    [Export] public float EnginePower = 300f;
    [Export] public float MaxSteerAngle = 45f;

    public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
		var deltaTime = (float)delta;

        Steering = Mathf.MoveToward(Steering, (Input.GetAxis("drive_right", "drive_left") *
			Mathf.DegToRad(MaxSteerAngle)), 2.5f * deltaTime);

        EngineForce = Input.GetAxis("drive_backward", "drive_forward") * EnginePower;
    }
}
