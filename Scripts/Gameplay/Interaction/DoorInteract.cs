using Godot;
using System;

using static MyUtils.Game;

public partial class DoorInteract : Interactable
{
    [Export] public Node3D Hinge;
    [Export] public Node3D Ray1;
    [Export] public Node3D Ray2;
	[Export] public float OpenAngle;
    [Export] public float Speed;

	private float currentAngle;

    public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
		var deltaTime = (float)delta;
		var openAngle = Mathf.DegToRad(OpenAngle);

        currentAngle = Hinge.Rotation.Y;

		if (currentAngle < openAngle)
			Hinge.Rotation = new Vector3(0f, LerpAngle(currentAngle, openAngle, Speed * deltaTime), 0f);

		Debug.Write($"currentRot: {currentAngle}, {openAngle}");
	}

    public void Interacted(Player m_player)
	{

	}
}
