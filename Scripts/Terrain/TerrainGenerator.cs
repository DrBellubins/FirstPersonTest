using Godot;
using Godot.NativeInterop;
using Godot.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FastNoiseLite = FastNoise.FastNoiseLite;

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

public partial class TerrainGenerator : Node3D
{
    [ExportCategory("General")]
    [Export] public PackedScene[] Rocks;
    [Export] public PackedScene[] Buildings;

    [ExportCategory("UI")]
    [Export] public Label loadingText;
    [Export] public ProgressBar loadingBar;
    [Export] public ColorRect blurRect;

    // Settings
	public const int ChunkSize = 32;
	public const int RenderDistance = 8;

    // Various
    private FastNoiseLite noise = new FastNoiseLite();
    private Material terrainMaterial;
    private Vector2 prevPlayerChunkPos;

    private Vector2 playerPos = new Vector2();
    private bool isPlayerFrozen = false;
    private bool isFirstGen = false;

    private List<CollisionShape3D> rockCols = new List<CollisionShape3D>();

    // Multi-threading stuff
    private Thread chunkThread;

    // TODO: One chunk thread is fine, any > 1 makes chunks gen over eachother...
    //private Thread[] chunkThreads = new Thread[4];
    private Thread[,] cThreads = new Thread[1, 1];

    private ConcurrentDictionary<Vector2, Chunk> chunks = new ConcurrentDictionary<Vector2, Chunk>();

    private List<Node3D> rocks = new List<Node3D>();
    private List<Vector2> rockPositions = new List<Vector2>();

    public override void _Ready()
	{
        for (int i = 0; i < Rocks.Length; i++)
        {
            var rockInstance = (Node3D)Rocks[i].Instantiate();
            var rockMesh = rockInstance.GetChild<MeshInstance3D>(0);

            rockMesh.CreateConvexCollision(true, true);
            rockCols.Add(rockMesh.GetChild<StaticBody3D>(0).GetChild<CollisionShape3D>(0));
        }

        terrainMaterial = ResourceLoader.Load<Material>("res://Materials/Terrain.tres");

        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetSeed(new Random().Next(int.MaxValue));
        //simplex.Seed = 999;

        isFirstGen = true;


        var threadDivSize = (RenderDistance / cThreads.Length);

        for (int x = 0; x < cThreads.GetLength(0); x++)
        {
            for (int z = 0; z < cThreads.GetLength(1); z++)
            {
                cThreads[x, z] = new Thread(() => regenerateChunks(x * threadDivSize, z * threadDivSize));
                cThreads[x, z].Start();
            }
        }

        isPlayerFrozen = true;

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
        playerPos = new Vector2(Game.GetPlayerPosition().X, Game.GetPlayerPosition().Z);

        Game.Player.IsFrozen = isPlayerFrozen;
        loadingText.Visible = isPlayerFrozen;
        loadingBar.Visible = isPlayerFrozen;
        //blurRect.Visible = isPlayerFrozen; // TODO: Tonemap overrides blur canvas or vice versa

        Debug.Write($"Num chunks: {chunks.Count}");

        var progress = ((double)chunks.Count / ((double)RenderDistance * (double)RenderDistance)) * 100.0d;
        loadingBar.Value = progress;
    }

    private void regenerateChunks(int threadX, int threadZ)
	{
        while (true)
        {
            Thread.Sleep(25);

            var playerChunkPos = Game.GetNearestChunkCoord(playerPos);
            var halfChunkSize = ChunkSize / 2;

            // Clear chunks
            /*for (int i = 0; i < chunks.Count; i++)
            {
                if (chunks[i].Position.DistanceTo(playerPos) > (RenderDistance * halfChunkSize))
                {
                    if (chunks[i].MeshInstance != null)
                        chunks[i].MeshInstance.CallDeferred("free");

                    //chunks.RemoveAt(i);
                    //chunkPositions.RemoveAt(i);
                }
            }*/

            // Clear rocks/trees
            /*for (int i = 0; i < rocks.Count; i++)
            {
                if (rockPositions[i].DistanceTo(playerPos) > (RenderDistance * halfChunkSize))
                {
                    rocks[i].CallDeferred("free");

                    rocks.RemoveAt(i);
                    rockPositions.RemoveAt(i);
                }
            }*/

            for (int x = 0; x < RenderDistance; x++)
            {
                // Start from center
                int iX = (RenderDistance / 2) + (x % 2 == 0 ? x / 2 : -(x / 2 + 1));

                for (int z = 0; z < RenderDistance; z++)
                {
                    //Thread.Sleep(1000);

                    int iZ = (RenderDistance / 2) + (z % 2 == 0 ? z / 2 : -(z / 2 + 1));

                    var chunkPos = new Vector2((int)playerChunkPos.X + ((x * ChunkSize) - (RenderDistance * halfChunkSize)),
                        (int)playerChunkPos.Y + (z * ChunkSize) - (RenderDistance * halfChunkSize));

                    //Debug.Write($"agj: {threadOffset}");
                    //var threadDivSize = (RenderDistance / cThreads.Length);
                    chunkPos = new Vector2(chunkPos.X + threadX, chunkPos.Y + threadZ);

                    if (chunkPos.DistanceTo(playerPos) < (RenderDistance * halfChunkSize))
                    {
                        Chunk genChunk = null;

                        if (isFirstGen)
                        {
                            if (!chunks.ContainsKey(chunkPos))
                                genChunk = generateChunk(chunkPos);

                            //generateRock(chunkPos);
                        }
                        else
                        {
                            if ((playerChunkPos.X != prevPlayerChunkPos.X || playerChunkPos.Y != prevPlayerChunkPos.Y))
                            {
                                //if (!chunks.ContainsKey(chunkPos))
                                //    genChunk = generateChunk(chunkPos);

                                //if (!chunkPositions.Contains(chunkPos))
                                //    genChunk = generateChunk(chunkPos);

                                //if (!rockPositions.Contains(chunkPos))
                                //    generateRock(chunkPos);
                            }
                        }
                    }
                }
            }

            isFirstGen = false;
            isPlayerFrozen = false;

            prevPlayerChunkPos = playerChunkPos;
        }
    }

    private Chunk generateChunk(Vector2 chunkPos)
    {
        var chunk = new Chunk();
        chunk.Biome = Biomes.DesertPlanes; // TODO: procedurally gen biomes
        chunk.Position = chunkPos;

        var plane = new PlaneMesh();
        plane.Size = new Vector2(ChunkSize, ChunkSize);
        plane.SubdivideDepth = ChunkSize / 2;
        plane.SubdivideWidth = ChunkSize / 2;

        plane.Material = terrainMaterial;

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

        // Must happen before mesh creations
        //generateBuilding(chunkPos, dataTool);

        arrayPlane.ClearSurfaces();

        dataTool.CommitToSurface(arrayPlane);
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        surfaceTool.CreateFrom(arrayPlane, 0);
        surfaceTool.GenerateNormals();
        surfaceTool.GenerateTangents();

        var meshInstance = new MeshInstance3D();

        meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.DoubleSided;
        //meshInstance.ProcessThreadGroup = ProcessThreadGroupEnum.SubThread;
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

    /*private void generateRock(Vector2 chunkPos)
    {
        // Generate rocks/trees
        var noise = simplex.GetNoise2D(chunkPos.X, chunkPos.Y) * 2;

        simplex.Frequency = 0.025f;

        var indexNoise = simplex.GetNoise3D(chunkPos.X, noise * 2f, chunkPos.Y) * 2f;
        var rockIndex = (int)Mathf.Clamp(Mathf.Abs(indexNoise) * (float)Rocks.Length, 0f, (float)Rocks.Length - 1f);

        var pScenes = Rocks[rockIndex];

        if (noise < -0.5f)
        {
            var rock = (Node3D)pScenes.Instantiate();

            var rockBody = new StaticBody3D();
            var rockCol = new CollisionShape3D();

            rockCol.Shape = rockCols[rockIndex].Shape;

            rockCol.QueueFree();

            var rockOwnderID = rockBody.CreateShapeOwner(rockBody);
            rockBody.ShapeOwnerAddShape(rockOwnderID, rockCol.Shape);

            rock.CallDeferred("add_child", rockBody);

            var pos = new Vector3(chunkPos.X, noise, chunkPos.Y);

            rock.Position = pos;
            rock.Rotation = new Vector3(0f, noise * 5f, 0f);

            rocks.Add(rock);
            rockPositions.Add(chunkPos);

            CallDeferred("add_child", rock);
        }
    }*/

    /*private void generateBuilding(Vector2 chunkPos, MeshDataTool meshData)
    {
        var building = (Building)Buildings[0].Instantiate();

        var worldChunkPos = new Vector3(chunkPos.X, 0f, chunkPos.Y);

        building.Position = worldChunkPos;

        CallDeferred("add_child", building);

        Debug.Write($"{building.BuildingEdges.Count}");

        for (int i = 0; i < meshData.GetVertexCount(); i++)
        {
            var vertex = meshData.GetVertex(i);

            for (int ii = 0; ii < building.BuildingEdges.Count; ii++)
            {
                if (istVertexClose(worldChunkPos + vertex, building.BuildingEdges[ii], 1f))
                {
                    meshData.SetVertex(i, building.BuildingEdges[ii]);
                    Debug.Write($"building edge {ii}: {building.BuildingEdges[ii]}");
                }
            }
        }
    }*/

    private bool concurrentContains(ConcurrentBag<Vector2> posBag, Vector2 pos)
    {
        for (int i = 0; i < posBag.Count; i++)
        {
            Vector2 searchPos;
            posBag.TryPeek(out searchPos);

            bool found = searchPos == pos;

            return found;
        }

        return false;
    }

    private Chunk? getChunkAtPos(Vector2 pos)
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            Chunk outChunk;
            bool found = chunks.TryGetValue(pos, out outChunk);

            return outChunk;

            //if (chunks[i].Position == pos)
            //    return chunks[i];
        }

        return null;
    }

    // Each vertex is 1 meter apart
    private bool istVertexClose(Vector3 vertex, Vector3 pos, float weight)
    {
        var terrainVertex = new Vector2(vertex.X, vertex.Z);
        var terrainPos = new Vector2(pos.X, pos.Z);

        if (terrainPos.DistanceTo(terrainPos) < weight)
        {
            return true;
        }

        return false;
    }

	private void drawDebugSphere(Vector3 pos)
	{
		var instance = new MeshInstance3D();

        instance.Position = pos;

		var mesh = new SphereMesh();
		mesh.Radius = 0.1f;
		mesh.Height = 0.2f;

		instance.Mesh = mesh;

        CallDeferred("add_child", instance);
    }
}
