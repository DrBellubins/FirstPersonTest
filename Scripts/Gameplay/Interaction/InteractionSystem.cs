using Godot;
using System;

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
                if (Input.IsActionJustPressed("interact"))
                {
					if (hitCol is Interactable)
					{
						var inter = (Interactable)hitCol;
						inter.EmitSignal("e_Interacted", Player);
					}

                    if (hitCol is Holdable)
					{
						if (!isHolding)
						{
							isHolding = true;
							heldObject = (Holdable)hitCol;
						}
						else
							isHolding = false;
                    }
                }
            }
        }
		else
		{
			if (isHolding && Input.IsActionJustPressed("interact"))
				isHolding = false;

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

			heldObject.GravityScale = 0f;

            heldObject.LinearVelocity = (b - a) * holdPower;

			heldObject.GlobalRotation = HoldPos.GlobalRotation;
			heldObject.AngularVelocity = Vector3.Zero;
            //heldObject.GlobalRotation = heldObject.GlobalRotation.Lerp(HoldPos.GlobalRotation, holdPower * (float)delta);
        }

        if (!isHolding && heldObject != null)
            heldObject.GravityScale = 1f;
    }
}
