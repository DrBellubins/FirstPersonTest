using Godot;
using System;

public partial class Car : VehicleBody3D
{
    [Export] public float EnginePower = 300f;
    [Export] public float MaxSteerAngle = 45f;
    [Export] public AudioStreamPlayer3D audio;
    [Export] public Camera3D camera;

    public bool IsDriving;
    public bool IsBrakeOn;

	public override void _Process(double delta)
	{
		var deltaTime = (float)delta;

        camera.Current = IsDriving;

        if (IsDriving)
        {
            if (Input.IsActionJustPressed("toggle_brake"))
                IsBrakeOn = !IsBrakeOn;

            audio.VolumeDb = -5f;

            Steering = Mathf.MoveToward(Steering, (Input.GetAxis("drive_right", "drive_left") *
            Mathf.DegToRad(MaxSteerAngle)), 2.5f * deltaTime);

            EngineForce = Input.GetAxis("drive_backward", "drive_forward") * EnginePower;

            var linearMagnitude = MathF.Sqrt((LinearVelocity.X * LinearVelocity.X) +
                (LinearVelocity.Y * LinearVelocity.Y) + (LinearVelocity.Z * LinearVelocity.Z));

            if (Input.IsActionPressed("drive_backward") || Input.IsActionPressed("drive_forward"))
            {
                audio.PitchScale = Mathf.Clamp(MathF.Abs(linearMagnitude / EnginePower) *
                    (MathF.Abs(EngineForce) / EnginePower) * 4f, 0.25f, 1f);
            }
            else // TODO: stutters when letting of throttle
            {
                audio.PitchScale = Mathf.Clamp(MathF.Abs(linearMagnitude / EnginePower) * 4f, 0.25f, 1f);
            }
        }
        else
        {
            EngineForce = 0f;
            Steering = 0f;
            audio.VolumeDb = -80f;
        }

        // TODO: Car doesn't fully stop when braking (likely godot issue)
        if (IsBrakeOn)
        {
            var linearMagnitude = MathF.Sqrt((LinearVelocity.X * LinearVelocity.X) +
                (LinearVelocity.Y * LinearVelocity.Y) + (LinearVelocity.Z * LinearVelocity.Z));

            Brake = linearMagnitude;

            if (MathF.Abs(linearMagnitude) < 1f)
                Sleeping = true;
                
        }
        else
            Sleeping = false;
    }
}
