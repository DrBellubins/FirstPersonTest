using Godot;
using System;

[Tool]
public partial class DayNightCycle : Node3D
{
    [Export(PropertyHint.Range, "0,24,")]
    public float TimeOfDay;

    [ExportCategory("Various settings")] 
    [Export] public float TimeSpeed = 1f;
    [Export] public WorldEnvironment WorldEnvironment;
    [Export] public DirectionalLight3D DirectionalLight;

    [ExportCategory("Sky settings")]
    [Export] public DayNightPreset Preset;

	public override void _Process(double delta)
	{
        if (!Engine.IsEditorHint())
        {
            TimeOfDay += (float)delta * TimeSpeed;
            TimeOfDay %= 24;

            //Debug.Write($"Time of day: {TimeOfDay}");
        }

        updateLighting(TimeOfDay / 24f);
    }

    private void updateLighting(float timePercent)
    {
        DirectionalLight.LightColor = Preset.SunColor.Sample(timePercent);
        DirectionalLight.RotationDegrees = new Vector3((timePercent * 360f) + 90, 170, 0);

        WorldEnvironment.Environment.AmbientLightColor = Preset.SkyColor.Sample(timePercent);
        WorldEnvironment.Environment.FogLightColor = Preset.SkyColor.Sample(timePercent);
    }
}
