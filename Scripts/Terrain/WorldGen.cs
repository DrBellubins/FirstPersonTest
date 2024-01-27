using Godot;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using FastNoiseLite = FastNoise.FastNoiseLite;
using System.Timers;
using System.Diagnostics.Metrics;
using System.Collections.Generic;

using MyUtils;

public partial class WorldGen : Node3D
{
    [Export] public Material TerrainMaterial;

    // Settings
    public const int ChunkSize = 32;
    public const int RenderDistance = 16;

    private const int threadDivSize = RenderDistance / 2;
    private const int halfChunkSize = ChunkSize / 2;

    // Gen
    private ConcurrentDictionary<Vector2I, Chunk> chunks = new ConcurrentDictionary<Vector2I, Chunk>();

    // Various
    private Vector2I playerChunkPos = new Vector2I();
    private Vector2I prevPlayerChunkPos = new Vector2I();

    public override void _Ready()
	{
        runChunkThreads();

        RenderingServer.SetDebugGenerateWireframes(true);
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventKey && Input.IsKeyPressed(Key.P))
        {
            var vp = GetViewport();
            vp.DebugDraw = (Viewport.DebugDrawEnum)(((int)vp.DebugDraw + 1) % 5);
        }
    }

    public override void _Process(double delta)
	{
        playerChunkPos = Game.GetNearestChunkCoord(new Vector2I((int)Game.PlayerPos.X, (int)Game.PlayerPos.Z));

        Debug.Write($"Chunk regions: {counter}");
        Debug.Write($"Num chunks: {chunks.Count}");
    }

    private void runChunkThreads()
    {
        var seed = new Random().Next(int.MinValue, int.MaxValue);

        var thread1 = new Thread(() => generateChunkRegion(9999, 0, 0));
        var thread2 = new Thread(() => generateChunkRegion(9999, 0, 1));
        var thread3 = new Thread(() => generateChunkRegion(9999, 1, 1));
        var thread4 = new Thread(() => generateChunkRegion(9999, 1, 0));

        thread1.Start();
        thread2.Start();
        thread3.Start();
        thread4.Start();
    }

    int counter = 0;
    private void generateChunkRegion(int seed, int x, int z)
    {
        var noise = new FastNoiseLite();
        noise.SetSeed(seed);

        var regionPos = new Vector2I(x * (threadDivSize * ChunkSize), z * (threadDivSize * ChunkSize));
        var isFirstGen = true;

        while (true)
        {
            Thread.Sleep(25);

            for (int cx = 0; cx < threadDivSize; cx++)
            {
                for (int cz = 0; cz < threadDivSize; cz++)
                {
                    var chunkPos = new Vector2I((regionPos.X + playerChunkPos.X) + ((cx * ChunkSize) - (RenderDistance * halfChunkSize)),
                            (regionPos.Y + playerChunkPos.Y) + (cz * ChunkSize) - (RenderDistance * halfChunkSize));

                    //drawRegionBorder(chunkPos, ChunkSize);

                    if (distanceTo(chunkPos, playerChunkPos) < (RenderDistance * halfChunkSize)
                        && !containsChunk(chunkPos))
                    {
                        if (isFirstGen)
                            generateChunk(noise, chunkPos);

                        if ((playerChunkPos.X != prevPlayerChunkPos.X || playerChunkPos.Y != prevPlayerChunkPos.Y))
                            generateChunk(noise, chunkPos);
                    }
                }
            }

            isFirstGen = false;
            prevPlayerChunkPos = playerChunkPos;
        }
    }

    private Chunk generateChunk(FastNoiseLite noise, Vector2I chunkPos)
    {
        var chunk = new Chunk();
        chunk.Biome = Biomes.DesertPlanes; // TODO: procedurally gen biomes
        chunk.Position = chunkPos;

        var plane = new PlaneMesh();
        plane.Size = new Vector2(ChunkSize, ChunkSize);
        plane.SubdivideDepth = ChunkSize / 2;
        plane.SubdivideWidth = ChunkSize / 2;

        plane.Material = TerrainMaterial;

        var surfaceTool = new SurfaceTool();
        var dataTool = new MeshDataTool();

        surfaceTool.CreateFrom(plane, 0);

        var arrayPlane = surfaceTool.Commit(new ArrayMesh());
        var error = dataTool.CreateFromSurface(arrayPlane, 0);

        for (int i = 0; i < dataTool.GetVertexCount(); i++)
        {
            var vertex = dataTool.GetVertex(i);

            noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            noise.SetFrequency(0.005f);
            var vertNoise = noise.GetNoise(chunkPos.X + vertex.X, chunkPos.Y + vertex.Z) * 4f;

            noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            noise.SetFrequency(0.1f);
            vertNoise += noise.GetNoise(chunkPos.X + vertex.X, chunkPos.Y + vertex.Z) * 2f;

            vertex.Y = vertNoise;

            chunk.VertexPositions.Add(vertex);
            dataTool.SetVertex(i, vertex);
        }

        arrayPlane.ClearSurfaces();

        dataTool.CommitToSurface(arrayPlane);
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        surfaceTool.CreateFrom(arrayPlane, 0);
        surfaceTool.GenerateNormals();
        surfaceTool.GenerateTangents();

        var meshInstance = new MeshInstance3D();

        meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.DoubleSided;
        meshInstance.Position = new Vector3(chunkPos.X, 0f, chunkPos.Y);
        meshInstance.Mesh = surfaceTool.Commit(new ArrayMesh());

        // Collision
        var shape = new ConcavePolygonShape3D();
        shape.Data = meshInstance.Mesh.GetFaces();

        var body = new StaticBody3D();
        var col = new CollisionShape3D();

        col.Shape = shape;

        var ownderID = body.CreateShapeOwner(body);
        body.ShapeOwnerAddShape(ownderID, col.Shape);

        meshInstance.AddChild(body);
        CallThreadSafe("add_child", meshInstance);

        chunk.MeshInstance = meshInstance;

        col.Shape = shape;

        col.QueueFree();

        chunks.TryAdd(chunkPos, chunk);

        return chunk;
    }

    private bool containsChunk(Vector2I query)
    {
        bool success = false;

        foreach (var item in chunks)
        {
            var pos = item.Key;
            var chunk = item.Value;

            if (pos.X == query.X && pos.Y == query.Y)
                success = true;
        }

        return success;
    }

    private float distanceTo(Vector2I from, Vector2I to)
    {
        return (to - from).Length();
    }

    private void drawDebugSphere(Vector2I pos)
    {
        var instance = new MeshInstance3D();

        instance.Position = new Vector3(pos.X - halfChunkSize, 0f, pos.Y - halfChunkSize);

        var mesh = new SphereMesh();
        mesh.Radius = 0.3f;
        mesh.Height = 100f;
        mesh.RadialSegments = 3;

        instance.Mesh = mesh;

        CallDeferred("add_child", instance);
    }

    private void drawRegionBorder(Vector2I pos, float regionSize)
    {
        var halfRegionSize = regionSize / 2;

        for (int x = 0; x < 2; x++)
        {
            for (int z = 0; z < 2; z++)
            {
                var instance = new MeshInstance3D();

                instance.Position = new Vector3((pos.X - halfChunkSize) + (x * regionSize) - halfRegionSize,
                    0f, (pos.Y - halfChunkSize) + (z * regionSize) - halfRegionSize);

                var mesh = new SphereMesh();
                mesh.Radius = 0.3f;
                mesh.Height = 100f;
                mesh.RadialSegments = 3;

                instance.Mesh = mesh;

                CallDeferred("add_child", instance);
            }
        }
    }
}
