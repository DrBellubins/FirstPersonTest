using Godot;
using System;

using MyUtils;

public partial class CarInteract : Interactable
{
	private Car car;
	private Player player;
	private Node3D exitPos;

	public override void _Ready()
	{
		car = GetParent<Car>();
		exitPos = GetChild<Node3D>(0);

		e_Interacted += Interacted;
	}

	public override void _Process(double delta)
	{
		if (car.IsDriving && Input.IsActionJustPressed("interact"))
		{
			player.IsDriving = false;
			car.IsDriving = false;

			player.Position = exitPos.GlobalPosition;
		}
	}

    public void Interacted(Player m_player)
    {
		Game.CurrentCar = car;

        player = m_player;
		player.IsDriving = true;
		car.IsDriving = true;
    }
}
