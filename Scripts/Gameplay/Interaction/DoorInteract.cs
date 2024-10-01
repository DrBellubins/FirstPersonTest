using Godot;
using MyUtils;
using System;
using System.Collections.Generic;
using static MyUtils.Game;

public partial class DoorInteract : Interactable
{
    [ExportGroup("Main settings")]
    [Export] public Node3D Parent;
    [Export] public Node3D Hinge;
    [Export] public Node3D Ray1;
    [Export] public Node3D Ray2;
	[Export] public float OpenAngle;
    [Export] public float Speed;
    [Export] public bool IsLocked;

    [ExportGroup("Audio settings")]
    [Export] public AudioStreamPlayer3D AudioPlayer;
    [Export] public AudioStream[] Sounds;

    private bool isOpen;
    private bool openOutward;
    private bool hasBeenOpened;
    private float currentAngle;

    private Vector3 angle;

    public override void _Ready()
	{
        e_Interacted += Interacted;
        hasBeenOpened = false;
    }

    private bool playedCloseSound = false;
	public override void _Process(double delta)
	{
		var deltaTime = (float)delta;
		var openAngle = Mathf.DegToRad(OpenAngle);

        currentAngle = Hinge.Rotation.Y;

        openAngle = openOutward ? -openAngle : openAngle;

        Debug.Write($"currentRot: {currentAngle}");
        Debug.Write($"openAngle: {openAngle}");
        Debug.Write($"angle: {angle}");

        if (isOpen)
		{
            hasBeenOpened = true;

            var lerpAngle = Mathf.LerpAngle(currentAngle, openAngle, Speed * deltaTime);
			var openVec = new Vector3(0f, openAngle, 0f);

            Hinge.Rotation = new Vector3(0f, lerpAngle, 0f);

            if (openOutward)
                Hinge.Rotation = Hinge.Rotation.Clamp(openVec, -openVec);
            else
                Hinge.Rotation = Hinge.Rotation.Clamp(Vector3.Zero, openVec);
        }
		else // Closing
		{
            // TODO: Opening door front-wise then back-wise teleports to close position
            var lerpAngle = Mathf.LerpAngle(currentAngle, 0f, Speed * deltaTime);
            var openVec = new Vector3(0f, openAngle, 0f);

            Hinge.Rotation = new Vector3(0f, lerpAngle, 0f);

            if (openOutward)
                Hinge.Rotation = Hinge.Rotation.Clamp(openVec, -openVec);
            else
                Hinge.Rotation = Hinge.Rotation.Clamp(Vector3.Zero, openVec);

            if (InRange(currentAngle, Mathf.DegToRad(-5f), Mathf.DegToRad(5f)) && hasBeenOpened && !playedCloseSound)
            {
                AudioPlayer.Stream = Sounds[1];
                AudioPlayer.Play();

                playedCloseSound = true;
            }
        }
    }

    public void Interacted(Player m_player)
	{
        if (!IsLocked)
        {
            // TODO: Make relative to door rotation
            angle = Parent.GlobalPosition.DirectionTo(m_player.GlobalPosition);

            openOutward = angle.Z > 0f;

            isOpen = !isOpen;

            if (isOpen)
            {
                AudioPlayer.Stream = Sounds[0];
                AudioPlayer.Play();

                playedCloseSound = false;
            }
        }
        else
        {
            AudioPlayer.Stream = Sounds[2];
            AudioPlayer.Play();
        }
    }
}
