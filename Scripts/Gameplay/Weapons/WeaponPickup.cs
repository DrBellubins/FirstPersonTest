using Godot;
using System;

public partial class WeaponPickup : Interactable
{
    [Export] public int WeaponIndex;

	public override void _Ready()
	{
        e_Interacted += Interacted;
    }

    public void Interacted(Player m_player)
    {
        m_player.WeaponSystem.EmitSignal("e_PickedUp", WeaponIndex);
        GetParent().QueueFree();
    }
}
