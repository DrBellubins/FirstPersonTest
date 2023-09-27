using Godot;
using System;

public partial class Holdable : RigidBody3D
{
	[Export] public AudioStreamPlayer3D HitAudioPlayer;

    public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	public void OnBodyEntered(Node body)
	{
		if (!HitAudioPlayer.Playing)
			HitAudioPlayer.Play();
	}
}
