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
	private const int chunkSize = 16;
	private const int renderDistance = 16;

    private Material terrainMaterial;
    private FastNoiseLite noise = new FastNoiseLite();
    private Vector2 prevPlayerChunkPos;

    private Vector2 playerPos = new Vector2();

    // Multi-threading stuff
    private Thread chunkThread;
    private List<Vector2> chunkPositions = new List<Vector2>();
    private List<MeshInstance3D> chunks = new List<MeshInstance3D>();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        terrainMaterial = ResourceLoader.Load<Material>("res://Materials/Terrain.tres");

        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
		noise.Seed = new Random().Next(int.MaxValue);
		//noise.Seed = 999;
		noise.Frequency = 0.005f;

        chunkThread = new Thread(new ThreadStart(regenerateChunks));
        chunkThread.Start();
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        playerPos = new Vector2(Game.Player.GlobalPosition.X, Game.Player.GlobalPosition.Z);
    }

	private void generateChunk(Vector2 chunkPos)
	{
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
        
            vertex.Y = noise.GetNoise2D(vertex.X + chunkPos.X, vertex.Z + chunkPos.Y) * 2f;
        
            // Road
            if (chunkPos.X + vertex.X == -0.8888886f || chunkPos.X + vertex.X == -2.6666663f || chunkPos.X + vertex.X == -4.444444f
                || chunkPos.X + vertex.X == 0.8888892f || chunkPos.X + vertex.X == 2.666667f)
                vertex.Y = 0;
        
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
        
        //col.QueueFree();

        meshInstance.CallDeferred("add_child", body);
        CallDeferred("add_child", meshInstance);

        col.Shape = shape;

        chunks.Add(meshInstance);
        chunkPositions.Add(chunkPos);
    }
	
	private void regenerateChunks()
	{
        while (true)
        {
            var playerChunkPos = Game.GetNearestChunkCoord(playerPos);

            if (playerChunkPos.X != prevPlayerChunkPos.X || playerChunkPos.Y != prevPlayerChunkPos.Y)
            {
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

                for (int x = 0; x < renderDistance; x++)
                {
                    for (int z = 0; z < renderDistance; z++)
                    {
                        var chunkPos = new Vector2((int)playerChunkPos.X + ((x * chunkSize) - (renderDistance * 8)),
                            (int)playerChunkPos.Y + (z * chunkSize) - (renderDistance * 8));

                        if (!chunkPositions.Contains(chunkPos))
                            generateChunk(chunkPos);
                    }
                }
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

        AddChild(instance);
    }
}
