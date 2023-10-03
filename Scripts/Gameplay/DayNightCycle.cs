using Godot;
using System;

public partial class DayNightCycle : Node3D
{
    [ExportCategory("Various settings")] 
    [Export] public float TimeSpeed;
    [Export] public WorldEnvironment environment;

    [ExportCategory("Sun settings")]
	[Export] public float SunIntensity;
    [Export] public float MoonIntensity;
    [Export(PropertyHint.ColorNoAlpha)] public Color SunColor;
    [Export(PropertyHint.ColorNoAlpha)] public Color MoonColor;

    [ExportCategory("Sky settings")]
    [Export(PropertyHint.ColorNoAlpha)] public Color SkyDayColor;
    [Export(PropertyHint.ColorNoAlpha)] public Color SkyNightColor;

	private bool isDaytime;
	private DirectionalLight3D sunMoon;

    public override void _Ready()
	{
		sunMoon = (DirectionalLight3D)GetChild(0);
		//isDaytime = true;
	}

	public override void _Process(double delta)
	{
		var rotation = Mathf.RadToDeg(this.Rotation.X);

        this.Rotate(Vector3.Right, TimeSpeed * (float)delta);

        if (rotation > 90f)
		{
            this.RotateX(135f);

			isDaytime = !isDaytime;
        }

		if (isDaytime)
		{
			sunMoon.LightEnergy = SunIntensity;
            sunMoon.LightColor = SunColor;

            environment.Environment.BackgroundEnergyMultiplier = 0.15f;
            environment.Environment.BackgroundColor = SkyDayColor;

            environment.Environment.FogLightColor = SkyDayColor;
            environment.Environment.FogLightEnergy = 0.15f;
        }	
		else
		{
            sunMoon.LightEnergy = MoonIntensity;
            sunMoon.LightColor = MoonColor;

            environment.Environment.BackgroundEnergyMultiplier = 0.05f;
            environment.Environment.BackgroundColor = SkyNightColor;

            environment.Environment.FogLightColor = SkyNightColor;
            environment.Environment.FogLightEnergy = 0.05f;
        }
    }
}
