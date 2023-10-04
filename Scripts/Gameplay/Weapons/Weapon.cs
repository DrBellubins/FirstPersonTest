using Godot;
using System;

public partial class Weapon : Node3D
{
    [Export] public bool IsAutomatic;
    [Export] public float FireRate;
    [Export] public float ReloadTime;
}
