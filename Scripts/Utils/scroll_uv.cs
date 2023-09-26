using Godot;
using System;

public partial class scroll_uv : Node
{
    [Export]
    private Vector3 MoveSpeed;

    private MeshInstance3D meshInstance;
    private StandardMaterial3D material;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        meshInstance = GetChild<MeshInstance3D>(0);
        material = (StandardMaterial3D)meshInstance.GetActiveMaterial(0);
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        float deltaTime = (float)delta;
        update_uv(new Vector3(MoveSpeed.X * deltaTime, MoveSpeed.Y * deltaTime, MoveSpeed.Z * deltaTime));
	}

    public void update_uv(Vector3 uv)
    {
        material.Uv1Offset += uv;
        meshInstance.MaterialOverride = material;
    }
}
