using Godot;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using FastNoiseLite = FastNoise.FastNoiseLite;

public partial class WorldGen : Node3D
{
    [Export] public Material TerrainMaterial;

    // Settings
    public const int ChunkSize = 32;
    public const int RenderDistance = 16;
    public const int ChunkThreads = 4; // Should be a ^2

    private const int threadDivSize = RenderDistance / ChunkThreads;

    // Gen
    private ConcurrentDictionary<Vector2I, Chunk> chunks = new ConcurrentDictionary<Vector2I, Chunk>();
    private FastNoiseLite noise = new FastNoiseLite();

    // Various
    private Vector2I playerPos = new Vector2I();
    private Vector2I prevPlayerChunkPos = new Vector2I();

    private Vector2I test = new Vector2I();

    public override void _Ready()
	{
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetSeed(new Random().Next(int.MinValue, int.MaxValue));

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
        Debug.Write($"Num chunks: {chunks.Count}");
        Debug.Write($"{test}");
    }

    private async Task runChunkThreads()
    {
        if (ChunkThreads < 2)
        {
            await Task.Run(() => generateChunkRegion(Vector2I.Zero));
        }
        else
        {
            for (int x = 0; x < ChunkThreads / 2; x++)
            {
                for (int z = 0; z < ChunkThreads / 2; z++)
                {
                    await Task.Run(() => generateChunkRegion(new Vector2I(x, z)));
                    test = new Vector2I(x, z);
                }
            }
        }
    }

    private void generateChunkRegion(Vector2I regionIndex)
    {
        var regionPos = new Vector2I((regionIndex.X * threadDivSize) * ChunkSize,
            (regionIndex.Y * threadDivSize) * ChunkSize);

        var playerChunkPos = Game.GetNearestChunkCoord(playerPos);
        var halfChunkSize = ChunkSize / 2;

        for (int x = 0; x < threadDivSize; x++)
        {
            for (int z = 0; z < threadDivSize; z++)
            {
                var chunkPos = new Vector2I((regionPos.X + playerChunkPos.X) + ((x * ChunkSize) - (RenderDistance * halfChunkSize)),
                        (regionPos.Y + playerChunkPos.Y) + (z * ChunkSize) - (RenderDistance * halfChunkSize));

                drawDebugSphere(new Vector3(chunkPos.X, 0f, chunkPos.Y));

                generateChunk(chunkPos);
            }
        }
    }

    private Chunk generateChunk(Vector2I chunkPos)
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

        var arrayPlane = surfaceTool.Commit();
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

            if (vertNoise > 0.95f)
            {
                vertNoise = 0f;
            }

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
        meshInstance.Mesh = surfaceTool.Commit();

        // Collision
        var shape = new ConcavePolygonShape3D();
        shape.Data = meshInstance.Mesh.GetFaces();

        var body = new StaticBody3D();
        var col = new CollisionShape3D();

        col.Shape = shape;

        var ownderID = body.CreateShapeOwner(body);
        body.ShapeOwnerAddShape(ownderID, col.Shape);

        meshInstance.CallDeferred("add_child", body);
        CallDeferred("add_child", meshInstance);

        chunk.MeshInstance = meshInstance;

        col.Shape = shape;

        col.QueueFree();

        chunks.TryAdd(chunkPos, chunk);

        return chunk;
    }

    private void drawDebugSphere(Vector3 pos)
    {
        var instance = new MeshInstance3D();

        instance.Position = pos;

        var mesh = new SphereMesh();
        mesh.Radius = 0.1f;
        mesh.Height = 100f;

        instance.Mesh = mesh;

        CallDeferred("add_child", instance);
    }
}
