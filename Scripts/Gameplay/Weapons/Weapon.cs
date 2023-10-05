using Godot;
using System;

public partial class Weapon : Node3D
{
    [Export] public bool IsAutomatic;
    [Export] public int MaxAmmo;
    [Export] public int MagazineSize;

    public int CurrentAmmo;
    public int CurrentMag;

    public override void _Ready()
    {
        CurrentAmmo = MaxAmmo;
        CurrentMag = MagazineSize;
    }
}
