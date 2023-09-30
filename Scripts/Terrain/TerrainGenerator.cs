using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public partial class TerrainGenerator : Node3D
{
    [ExportCategory("General")]
    [Export] public PackedScene[] Rocks;

    [ExportCategory("UI")]
    [Export] public Label loadingText;
    [Export] public ProgressBar loadingBar;
    [Export] public ColorRect blurRect;

    // Settings
	private const int chunkSize = 16;
	private const int renderDistance = 16;

    // Various
    private Material terrainMaterial;
    private FastNoiseLite simplex = new FastNoiseLite();
    private Vector2 prevPlayerChunkPos;

    private Vector2 playerPos = new Vector2();
    private bool isPlayerFrozen = false;

    private List<CollisionShape3D> rockCols = new List<CollisionShape3D>();

    // Multi-threading stuff
    private Thread chunkThread;
    
    private List<MeshInstance3D> chunks = new List<MeshInstance3D>();
    private List<Vector2> chunkPositions = new List<Vector2>();

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

        simplex.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        //simplex.Seed = new Random().Next(int.MaxValue);
        simplex.Seed = 999;

        chunkThread = new Thread(new ThreadStart(regenerateChunks));
        chunkThread.Start();

        isPlayerFrozen = true;
    }

	public override void _Process(double delta)
	{
        playerPos = new Vector2(Game.GetPlayerPosition().X, Game.GetPlayerPosition().Z);

        Game.Player.IsFrozen = isPlayerFrozen;
        loadingText.Visible = isPlayerFrozen;
        loadingBar.Visible = isPlayerFrozen;
        blurRect.Visible = isPlayerFrozen;

        var progress = ((double)chunkPositions.Count / ((double)renderDistance * (double)renderDistance)) * 100.0d;
        loadingBar.Value = progress;
    }

	private void generateChunk(Vector2 chunkPos)
	{
        simplex.Frequency = 0.005f;

        var plane = new PlaneMesh();
        plane.Size = new Vector2(chunkSize, chunkSize);
        plane.SubdivideDepth = chunkSize / 2;
        plane.SubdivideWidth = chunkSize / 2;
        
        plane.Material = terrainMaterial;
        
        var surfaceTool = new SurfaceTool();
        var dataTool = new MeshDataTool();
        
        surfaceTool.CreateFrom(plane, 0);
        
        var arrayPlane = surfaceTool.Commit();
        var error = dataTool.CreateFromSurface(arrayPlane, 0);
        
        for (int i = 0; i < dataTool.GetVertexCount(); i++)
        {
            var vertex = dataTool.GetVertex(i);
        
            vertex.Y = simplex.GetNoise2D(vertex.X + chunkPos.X, vertex.Z + chunkPos.Y) * 2f;
        
            // Road
            //if (chunkPos.X + vertex.X == -0.8888886f || chunkPos.X + vertex.X == -2.6666663f || chunkPos.X + vertex.X == -4.444444f
            //    || chunkPos.X + vertex.X == 0.8888892f || chunkPos.X + vertex.X == 2.666667f)
            //    vertex.Y = 0;
        
            dataTool.SetVertex(i, vertex);
        }
        
        arrayPlane.ClearSurfaces();
        
        dataTool.CommitToSurface(arrayPlane);
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        surfaceTool.CreateFrom(arrayPlane, 0);
        surfaceTool.GenerateNormals();
        //surfaceTool.GenerateTangents();
        
        var meshInstance = new MeshInstance3D();
        
        //meshInstance.CastShadow = ShadowCastingSetting.DoubleSided;
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

        col.Shape = shape;

        col.QueueFree();

        chunks.Add(meshInstance);
        chunkPositions.Add(chunkPos);

        // TODO: Only spawns last rock in array
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

        //drawDebugSphere(new Vector3(chunkPos.X, noise, chunkPos.Y));
    }
	
	private void regenerateChunks()
	{
        while (true)
        {
            var playerChunkPos = Game.GetNearestChunkCoord(playerPos);

            // Clear chunks
            for (int i = 0; i < chunks.Count; i++)
            {
                if (chunkPositions[i].DistanceTo(playerPos) > (renderDistance * 8))
                {
                    chunks[i].CallDeferred("free");

                    chunks.RemoveAt(i);
                    chunkPositions.RemoveAt(i);
                }
            }

            // Clear rocks/trees
            for (int i = 0; i < rocks.Count; i++)
            {
                if (rockPositions[i].DistanceTo(playerPos) > (renderDistance * 8))
                {
                    rocks[i].CallDeferred("free");

                    rocks.RemoveAt(i);
                    rockPositions.RemoveAt(i);
                }
            }

            if (playerChunkPos.X != prevPlayerChunkPos.X || playerChunkPos.Y != prevPlayerChunkPos.Y)
            {
                for (int x = 0; x < renderDistance; x++)
                {
                    for (int z = 0; z < renderDistance; z++)
                    {
                        var chunkPos = new Vector2((int)playerChunkPos.X + ((x * chunkSize) - (renderDistance * 8)),
                            (int)playerChunkPos.Y + (z * chunkSize) - (renderDistance * 8));

                        if (!chunkPositions.Contains(chunkPos) && !rockPositions.Contains(chunkPos))
                            generateChunk(chunkPos);
                    }
                }

                isPlayerFrozen = false;
            }

            prevPlayerChunkPos = playerChunkPos;
        }
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
