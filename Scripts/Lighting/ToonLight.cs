using Godot;
using MyUtils;
using System;

[Tool]
public partial class ToonLight : OmniLight3D
{
    public override void _Ready()
	{
        if (Visible)
        {
            Game.LightPositions.Add(GlobalPosition);
            GD.PushWarning($"Added light pos: {GlobalPosition}");
        }
    }
}
