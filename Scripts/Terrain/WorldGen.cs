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

    private const int threadDivSize = RenderDistance / 2;
    private const int halfChunkSize = ChunkSize / 2;

    // Gen
    private ConcurrentDictionary<Vector2I, Chunk> chunks = new ConcurrentDictionary<Vector2I, Chunk>();
    private int seed;
    private FastNoiseLite singleThreadedNoise = new FastNoiseLite();

    // Various
    private Vector2I playerThreadPos = new Vector2I();
    private Vector2I prevPlayerThreadPos = new Vector2I();

    private Vector2I playerChunkPos = new Vector2I();
    private Vector2I prevPlayerChunkPos = new Vector2I();

    public override void _Ready()
	{
        singleThreadedNoise.SetSeed(seed);

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
        playerThreadPos = Game.GetNearestCoord(new Vector2I((int)Game.PlayerPos.X, (int)Game.PlayerPos.Z), threadDivSize * ChunkSize);
        playerChunkPos = Game.GetNearestCoord(new Vector2I((int)Game.PlayerPos.X, (int)Game.PlayerPos.Z), ChunkSize);

        var debugTex = generateDebugTex(new Vector2(Game.PlayerPos.X - 64f, Game.PlayerPos.Z - 64f), 128);
        debugTex.Draw(textureRect.GetCanvasItem(), Vector2.Zero);

        if ((playerThreadPos.X != prevPlayerThreadPos.X || playerThreadPos.Y != prevPlayerThreadPos.Y))
            createDebugSphere(playerThreadPos);

        playerIcon.RotationDegrees = Game.Player.RotationDegrees.Y - 90f; // Hacky hardcoded offset for player starting rot

        Debug.Write($"Num chunks: {chunks.Count}");
        Debug.Write($"threadPos: {playerChunkPos.X}, {playerChunkPos.Y}");
    }

    #region Threading

    private void runChunkThreads()
    {
        seed = new Random().Next(int.MinValue, int.MaxValue);

        var thread1 = new Thread(() => generateChunkRegion(seed, 0, 0));
        var thread2 = new Thread(() => generateChunkRegion(seed, 0, 1));
        var thread3 = new Thread(() => generateChunkRegion(seed, 1, 1));
        var thread4 = new Thread(() => generateChunkRegion(seed, 1, 0));

        thread1.Start();
        thread2.Start();
        thread3.Start();
        thread4.Start();
    }

    private void generateChunkRegion(int seed, int x, int z)
    {
        var noise = new FastNoiseLite();
        noise.SetSeed(seed);

        var regionPos = new Vector2I(x * (threadDivSize * ChunkSize), z * (threadDivSize * ChunkSize));

        var isFirstGen = true;

        while (true)
        {
            Thread.Sleep(25);

            // TODO: Chunk regions aren't generated with player pos offset
            for (int cx = 0; cx < threadDivSize; cx++)
            {
                for (int cz = 0; cz < threadDivSize; cz++)
                {
                    var chunkPos = new Vector2I((regionPos.X + playerChunkPos.X) + ((cx * ChunkSize) - (RenderDistance * halfChunkSize)),
                            (regionPos.Y + playerChunkPos.Y) + (cz * ChunkSize) - (RenderDistance * halfChunkSize));

                    //createRegionBorder(chunkPos, ChunkSize);

                    if (chunkPos.DistanceTo(playerChunkPos) < (RenderDistance * halfChunkSize)
                        && !chunks.ContainsKey(chunkPos))
                    {
                        if (isFirstGen)
                        {
                            chunks.TryAdd(chunkPos, generateChunk(noise, chunkPos));
                        }

                        //if ((playerThreadPos.X != prevPlayerThreadPos.X || playerThreadPos.Y != prevPlayerThreadPos.Y))
                        //{
                        //    generateChunk(noise, chunkPos);
                        //}

                        if ((playerChunkPos.X != prevPlayerChunkPos.X || playerChunkPos.Y != prevPlayerChunkPos.Y))
                        {
                            chunks.TryAdd(chunkPos, generateChunk(noise, chunkPos));
                            //createRegionBorder(regionPos, threadDivSize * ChunkSize);
                        }
                    }
                }
            }

            isFirstGen = false;
            prevPlayerThreadPos = playerThreadPos;
            prevPlayerChunkPos = playerChunkPos;
        }
    }

    #endregion

    #region ChunkGen

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

        //meshInstance.ProcessThreadGroup = ProcessThreadGroupEnum.SubThread;
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

    private Texture2D generateDebugTex(Vector2 uv, int resolution)
    {
        byte[,] noiseData = new byte[resolution, resolution];
        byte[] colData = new byte[resolution * resolution];

        int index = 0;
        for (var x = 0; x < resolution; x++)
        {
            for (var y = 0; y < resolution; y++)
            {
                //var s_noise = singleThreadedNoise.GetNoise(uv.X + x, uv.Y + y) * 0.5f + 0.5f;
                var s_noise = (generateTerrainNoise(singleThreadedNoise, new Vector2(uv.X + x, uv.Y + y)) / 6f) * 0.5f + 0.5f;
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

    bool hasDrawnRB = false;
    private void createRegionBorder(Vector2I pos, float regionSize)
    {
        if (true)
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

                    hasDrawnRB = true;
                }
            }
        }
    }

    #endregion
}
