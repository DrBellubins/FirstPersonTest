using Godot;
using System;
using static MyUtils.Game;

public partial class DoorInteract : Interactable
{
    [Export] public Node3D Parent;
    [Export] public Node3D Hinge;
    [Export] public Node3D Ray1;
    [Export] public Node3D Ray2;
	[Export] public float OpenAngle;
    [Export] public float Speed;

    private bool isOpening;
    private bool openOutward;
    private float currentAngle;

    private Vector3 angle;

    public override void _Ready()
	{
        e_Interacted += Interacted;
    }

	public override void _Process(double delta)
	{
		var deltaTime = (float)delta;
		var openAngle = Mathf.DegToRad(OpenAngle);

        currentAngle = Hinge.Rotation.Y;

        openAngle = openOutward ? -openAngle : openAngle;

        Debug.Write($"currentRot: {currentAngle}");
        Debug.Write($"openAngle: {openAngle}");
        Debug.Write($"angle: {angle}");

        if (isOpening)
		{
            // TODO: Errors out when negative (only when openOutward conditional)
            var lerpAngle = Mathf.LerpAngle(currentAngle, openAngle, Speed * deltaTime);
			var openVec = new Vector3(0f, openAngle, 0f);

            Hinge.Rotation = new Vector3(0f, lerpAngle, 0f);
            Hinge.Rotation = Hinge.Rotation.Clamp(-openVec, openVec);
        }
		else // Closing
		{
            var lerpAngle = Mathf.LerpAngle(currentAngle, 0f, Speed * deltaTime);
            var openVec = new Vector3(0f, openAngle, 0f);

            Hinge.Rotation = new Vector3(0f, lerpAngle, 0f);
            Hinge.Rotation = Hinge.Rotation.Clamp(-openVec, openVec);
        }
    }

    public void Interacted(Player m_player)
	{
        // TODO: Make relative to door rotation
        angle = Parent.GlobalPosition.DirectionTo(m_player.GlobalPosition);

        openOutward = angle.Z > 0f;

        isOpening = !isOpening;
	}
}
