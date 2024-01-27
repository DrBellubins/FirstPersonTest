using Godot;
using MyUtils;
using System;

[Tool]
public partial class ToonMaterial : MeshInstance3D
{
	public override void _Process(double delta)
	{
        var transform = new Transform3D();

        if (Game.LightPositions.Count > 0)
            transform[0] = Game.LightPositions[0];

        (Mesh._SurfaceGetMaterial(0) as ShaderMaterial).SetShaderParameter("Light_Positions", transform);
    }
}
