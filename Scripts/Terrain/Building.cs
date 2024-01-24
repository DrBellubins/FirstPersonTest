using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class Building : Area3D
{
	[Export] public Area3D Boundry;

    private CollisionShape3D shape;

    public override void _EnterTree()
    {
        shape = (CollisionShape3D)GetChild(0);
    }
    private void OnBuildingBodyEntered(PhysicsBody3D body)
    {

    }

    public override void _PhysicsProcess(double delta)
	{
	}
}
