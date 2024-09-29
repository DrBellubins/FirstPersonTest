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
    [Export] public TextureRect textureRect;
    [Export] public Sprite2D playerIcon;

    // Settings
    public const int ChunkSize = 32;
    public const int RenderDistance = 8;

    private const int halfChunkSize = ChunkSize / 2;

    // Gen
    private int seed;
    private ConcurrentDictionary<Vector2I, Chunk> chunks = new ConcurrentDictionary<Vector2I, Chunk>();

    // Various
    private Vector2I playerChunkPos = new Vector2I();
    private Vector2I prevPlayerChunkPos = new Vector2I();

    private FastNoiseLite debugNoise = new FastNoiseLite();

    public override void _Ready()
	{
        runChunkThreads();

        debugNoise.SetSeed(seed);
    }

    public override void _Process(double delta)
	{
        playerChunkPos = Game.GetNearestCoord(new Vector2I((int)Game.PlayerPos.X, (int)Game.PlayerPos.Z), ChunkSize);

        if (Game.IsDebugVisible)
        {
            textureRect.Visible = true;

            var debugTex = generateDebugTex(debugNoise, new Vector2(Game.PlayerPos.X - 64f, Game.PlayerPos.Z - 64f), 128);
            textureRect.Texture = debugTex;
        }
        else
        {
            textureRect.Visible = false;
        }

        if ((playerChunkPos.X != prevPlayerChunkPos.X || playerChunkPos.Y != prevPlayerChunkPos.Y))
        {
            foreach (var c in chunks)
            {
                var chunk = c.Value;

                if (chunk.Position.DistanceTo(playerChunkPos) > (RenderDistance * halfChunkSize))
                {
                    var result = chunks.TryRemove(chunk.Position, out _);

                    if (result)
                        chunk.MeshInstance.QueueFree();
                }
            }
        }

        Debug.Write($"Num chunks: {chunks.Count}");
        Debug.Write($"playerChunkPos: {playerChunkPos.X}, {playerChunkPos.Y}");

        playerIcon.RotationDegrees = Game.Player.RotationDegrees.Y - 90f; // Hacky hardcoded offset for player starting rot
    }

    #region Threading

    private void runChunkThreads()
    {
        seed = new Random().Next(int.MinValue, int.MaxValue);

        var terrainThread = new Thread(() => regenerateTerrainChunks());

        terrainThread.Start();
    }

    private void regenerateTerrainChunks()
    {
        var chunk = new Chunk();
        var noise = new FastNoiseLite();
        noise.SetSeed(seed);

        var isFirstGen = true;

        while (true)
        {
            Thread.Sleep(25);

            for (int cx = 0; cx < RenderDistance; cx++)
            {
                for (int cz = 0; cz < RenderDistance; cz++)
                {
                    var chunkPos = new Vector2I(playerChunkPos.X + ((cx * ChunkSize) - (RenderDistance * halfChunkSize)),
                            playerChunkPos.Y + (cz * ChunkSize) - (RenderDistance * halfChunkSize));

                    if (chunkPos.DistanceTo(playerChunkPos) < (RenderDistance * halfChunkSize))
                    {
                        if (!chunks.ContainsKey(chunkPos))
                        {
                            if (isFirstGen)
                            {
                                chunk = generateTerrainChunk(noise, chunkPos);
                                chunks.TryAdd(chunkPos, chunk);
                            }

                            if ((playerChunkPos.X != prevPlayerChunkPos.X || playerChunkPos.Y != prevPlayerChunkPos.Y))
                            {
                                chunk = generateTerrainChunk(noise, chunkPos);
                                chunks.TryAdd(chunkPos, chunk);
                            }
                        }
                    }
                }
            }

            isFirstGen = false;

            prevPlayerChunkPos = playerChunkPos;
        }
    }

    #endregion

    #region ChunkGen

    private Chunk generateTerrainChunk(FastNoiseLite noise, Vector2I chunkPos)
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
            var pos = new Vector2(chunkPos.X + vertex.X, chunkPos.Y + vertex.Z);

            vertex.Y = generateTerrainNoise(noise, pos);

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
        CallDeferred("add_child", meshInstance);

        chunk.MeshInstance = meshInstance;

        col.Shape = shape;

        col.QueueFree();

        chunks.TryAdd(chunkPos, chunk);

        return chunk;
    }

    private float generateTerrainNoise(FastNoiseLite noise, Vector2 pos)
    {
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(0.005f);
        var hillNoise = noise.GetNoise(pos.X, pos.Y) * 4f;

        noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        noise.SetFrequency(0.1f);
        hillNoise += noise.GetNoise(pos.X, pos.Y) * 2f;

        //float dist = Mathf.SmoothStep(1f - pos.DistanceTo(Vector2.Zero), 1f, 0.1f) * 10f;

        return hillNoise;
    }

    #endregion

    #region Debug

    private Texture2D generateDebugTex(FastNoiseLite noise, Vector2 uv, int resolution)
    {
        byte[,] noiseData = new byte[resolution, resolution];
        byte[] colData = new byte[resolution * resolution];

        int index = 0;
        for (var x = 0; x < resolution; x++)
        {
            for (var y = 0; y < resolution; y++)
            {
                var s_noise = (generateTerrainNoise(noise, new Vector2(uv.X + x, uv.Y + y)) / 6f) * 0.5f + 0.5f;
                var b_noise = (byte)Mathf.Round(s_noise * 255f);

                noiseData[x, y] = b_noise;

                colData[index++] = noiseData[x, y]; // Need 1d array for image creation
            }
        }

        var noiseImg = Image.CreateFromData(resolution, resolution, false, Image.Format.R8, colData);

        return ImageTexture.CreateFromImage(noiseImg);
    }

    private void createDebugSphere(Vector2I pos)
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

    #endregion
}
