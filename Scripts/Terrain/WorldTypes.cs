using Godot;
using System;
using System.Collections.Generic;

// TODO: Add rock gen to chunk class
public enum Biomes
{
    DesertPlanes,
    DesertMountains,
    GrassPlanes
}

public class Chunk
{
    // Chunk data
    public string ID;
    public Vector2 Position;
    public Biomes Biome;

    // Mesh data
    public MeshInstance3D MeshInstance;
    public List<Vector3> VertexPositions = new List<Vector3>();

    public Chunk()
    {
        ID = Guid.NewGuid().ToString();
    }

    public Chunk(Vector2 position, Biomes biome, MeshInstance3D mesh)
    {
        ID = Guid.NewGuid().ToString();
        Position = position;
        Biome = biome;
        MeshInstance = mesh;
    }
}
