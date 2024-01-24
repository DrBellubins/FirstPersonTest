using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class Building : Node3D
{
    public List<Vector3> BuildingEdges = new List<Vector3>();

    [Export] public Array<Marker3D> buildingMarkers = new Array<Marker3D>();

    private bool firstLoad = true;
    private bool loadedBullshit;

	public override void _Process(double delta)
	{
        loadedBullshit = GetChildCount() > 0;

        if (loadedBullshit && firstLoad)
        {
            Debug.Write($"Bullshit loaded");

            for (int i = 0; i < buildingMarkers.Count; i++)
                BuildingEdges.Add(buildingMarkers[i].GlobalPosition);

            firstLoad = false;
        }
	}
}
