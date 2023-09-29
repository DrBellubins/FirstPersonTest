using Godot;
using System;

// TODO: If held item's pos is identical to hold pos when dropped, it floats
// TODO: Items glitch through walls
// TODO: Can't drop item if not looking at it
public partial class InteractionSystem : Node3D
{
	[Export] public Player Player;
	[Export] public Marker3D HoldPos;
	[Export] public RayCast3D Ray;
	[Export] public NinePatchRect Cursor;

	private bool isHolding;
	private Holdable heldObject;
	private float holdPower = 10f;

	private Control ui;

	public override void _Ready()
	{
        ui = Cursor.GetParent<Control>();
    }

	public override void _Process(double delta)
	{
		float deltaTime = (float)delta;
		var hitCol = Ray.GetCollider();

		Ray.Enabled = !Player.IsDriving;

		if (Ray.IsColliding())
		{
			Cursor.Size = Cursor.Size.Lerp(new Vector2(10f, 10f), 10f * deltaTime);
            Cursor.Position = Cursor.Position.Lerp(new Vector2(-5f, -5f), 10f * deltaTime);

			if (hitCol != null)
			{
				// TODO: If player gets out and is looking at enter trigger,
				// player will enter and exit car rapidly
                if (Input.IsActionJustPressed("interact"))
                {
					if (hitCol is Interactable)
					{
						var inter = (Interactable)hitCol;
						inter.EmitSignal("e_Interacted", Player);
					}

					if (hitCol is Holdable)
					{
                        isHolding = !isHolding;
					    heldObject = (Holdable)hitCol;
					}
                }
            }
        }
		else
		{
            Cursor.Size = Cursor.Size.Lerp(new Vector2(5f, 5f), 10f * deltaTime);
            Cursor.Position = Cursor.Position.Lerp(new Vector2(-2.5f, -2.5f), 10f * deltaTime);
        }
    }

	public override void _PhysicsProcess(double delta)
	{
		if (isHolding && heldObject != null)
		{
			var a = heldObject.GlobalPosition;
			var b = HoldPos.GlobalPosition;

            heldObject.LinearVelocity = (b - a) * holdPower;

			heldObject.GlobalRotation = HoldPos.GlobalRotation;
			heldObject.AngularVelocity = Vector3.Zero;
            //heldObject.GlobalRotation = heldObject.GlobalRotation.Lerp(HoldPos.GlobalRotation, holdPower * (float)delta);
        }
	}
}
