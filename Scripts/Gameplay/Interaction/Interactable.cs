using Godot;
using System;

public partial class Interactable : Area3D
{
    [Signal]
    public delegate void e_InteractedEventHandler(Player player);

    public override void _Ready()
	{
        e_Interacted += Interacted;
    }

    private void Interacted(Player player)
    {
        
    }
}
