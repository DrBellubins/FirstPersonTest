using Godot;
using System;

[Tool]
public partial class DayNightPreset : Resource
{
    [Export] public Gradient SunColor;
    [Export] public Gradient SkyColor;
}
