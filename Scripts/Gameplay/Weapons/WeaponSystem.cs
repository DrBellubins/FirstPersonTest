using Godot;
using System;

public partial class WeaponSystem : Node
{
    [Export] public PackedScene[] Weapons;

    [Signal]
    public delegate void e_PickedUpEventHandler(int weaponIndex);

	private Weapon currentWeapon;

    public override void _Ready()
	{
		e_PickedUp += pickedUp;
	}

	public override void _Process(double delta)
	{
		if (currentWeapon != null)
		{

		}
	}

	private void pickedUp(int weaponIndex)
	{
		currentWeapon = Weapons[weaponIndex].Instantiate<Weapon>();
		AddChild(currentWeapon);

        GD.PushWarning($"Picked up {currentWeapon.Name}");
    }
}
