using Godot;
using System;

public partial class Holdable : RigidBody3D
{
	[Export] public AudioStreamPlayer3D HitAudioPlayer;
	[Export] public float RespawnHeight = -20f;

	private Vector3 spawnPos;
	private Vector3 spawnRot;

    public override void _Ready()
	{
		BodyEntered += OnBodyEntered;

		spawnPos = Position;
		spawnRot = Rotation;
	}

    public override void _Process(double delta)
    {
        if (Position.Y < RespawnHeight)
		{
			LinearVelocity = Vector3.Zero;
			AngularVelocity = Vector3.Zero;

            Position = spawnPos;
            Rotation = spawnRot;
        }
    }

    public void OnBodyEntered(Node body)
	{
		if (!HitAudioPlayer.Playing)
            HitAudioPlayer.Play();
	}
}
