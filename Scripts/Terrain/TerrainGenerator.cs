using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// TODO: Make chunk gen multi-threaded
public partial class TerrainGenerator : MeshInstance3D
{
	private const int chunkSize = 16;
	private const int renderDistance = 8;

	private int sqrtRenderDistance;
    private FastNoiseLite noise = new FastNoiseLite();
    private Vector3 prevNearestChunkPos;

    private MeshInstance3D[,] renderedChunks = new MeshInstance3D[16, 16];
    //private List<Node> chunkBuffer = new List<Node>();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        sqrtRenderDistance = (int)MathF.Sqrt(renderedChunks.Length);
        //sqrtRenderDistance = (int)MathF.Sqrt(renderDistance);

        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
		noise.Seed = new Random().Next(int.MaxValue);
		//noise.Seed = 999;
		noise.Frequency = 0.005f;

        regenerateChunks(Vector3.Zero);

        /*chunkThread = new Thread(() =>
        {
            regenerateChunks(Vector3.Zero);
        });

		chunkThread.Start();*/
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        MainThreadInvoker.ProcessMainThreadQueue(delta);

        var nearestChunkPos = Game.GetNearestChunkCoord(Game.Player.Position);

		if (nearestChunkPos.X != prevNearestChunkPos.X || nearestChunkPos.Z != prevNearestChunkPos.Z)
		{
            //regenerateChunks(nearestChunkPos);

            /*chunkThread = new Thread(() =>
			{
				regenerateChunks(nearestChunkPos);
			});

            chunkThread.Start();*/
		}
			

		prevNearestChunkPos = nearestChunkPos;
	}

	private MeshInstance3D generateChunk(int x, int z)
	{
        var chunkTask = Task.Factory.StartNew(() =>
        {
            var plane = new PlaneMesh();
            plane.Size = new Vector2(chunkSize, chunkSize);
            plane.SubdivideDepth = chunkSize / 2;
            plane.SubdivideWidth = chunkSize / 2;

            plane.Material = ResourceLoader.Load<Material>("res://Materials/Terrain.tres");

            var surfaceTool = new SurfaceTool();
            var dataTool = new MeshDataTool();

            surfaceTool.CreateFrom(plane, 0);

            var arrayPlane = surfaceTool.Commit();
            var error = dataTool.CreateFromSurface(arrayPlane, 0);

            for (int i = 0; i < dataTool.GetVertexCount(); i++)
            {
                var vertex = dataTool.GetVertex(i);

                vertex.Y = noise.GetNoise2D(vertex.X + x, vertex.Z + z) * 2f;

                // Road
                if (x + vertex.X == -0.8888886f || x + vertex.X == -2.6666663f || x + vertex.X == -4.444444f
                    || x + vertex.X == 0.8888892f || x + vertex.X == 2.666667f)
                    vertex.Y = 0;

                dataTool.SetVertex(i, vertex);
            }

            arrayPlane.ClearSurfaces();

            dataTool.CommitToSurface(arrayPlane);
            surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
            surfaceTool.CreateFrom(arrayPlane, 0);
            surfaceTool.GenerateNormals();
            surfaceTool.GenerateTangents();

            var meshInstance = new MeshInstance3D();
            this.CallDeferred("add_child", meshInstance);

            //meshInstance.CastShadow = ShadowCastingSetting.DoubleSided;
            meshInstance.ProcessThreadGroup = ProcessThreadGroupEnum.SubThread;
            meshInstance.Position = new Vector3(x, 0f, z);
            meshInstance.Mesh = surfaceTool.Commit();

            // Collision
            var shape = new ConcavePolygonShape3D();
            shape.Data = meshInstance.Mesh.GetFaces();

            var body = new StaticBody3D();
            var col = new CollisionShape3D();

            body.CallDeferred("add_child", col);
            meshInstance.CallDeferred("add_child", body);

            col.Shape = shape;
        });

        return new MeshInstance3D();
    }
	
	private void regenerateChunks(Vector3 playerChunkPos)
	{
        // should prevent duplicate chunks in buffer (might not)
        var chunkBuffer = GetChildren().ToList().Distinct().ToList();

        for (int x = 0; x < sqrtRenderDistance; x++)
        {
            for (int z = 0; z < sqrtRenderDistance; z++)
            {
                var chunkPos = new Vector3I((int)playerChunkPos.X + ((x * chunkSize) - (sqrtRenderDistance * 8)),
                    0, (int)playerChunkPos.Z + (z * chunkSize) - (sqrtRenderDistance * 8));

                var bufferChunk = chunkBuffer.Find(x => x == getRenderedChunkAtPos(chunkPos));

                MeshInstance3D returnChunk;

                if (bufferChunk != null)
                {
                    
                    returnChunk = (MeshInstance3D)bufferChunk;
                }
                else
                {
                    // TODO: Doesn't seem centered
                    returnChunk = generateChunk(chunkPos.X, chunkPos.Z);
                }

                renderedChunks[x, z] = returnChunk;
            }
        }

        //clearChunks();
    }

    private MeshInstance3D? getRenderedChunkAtPos(Vector3 position)
    {
        MeshInstance3D retChunk = new MeshInstance3D();
        bool foundChunk = false;

        var chunkBuffer = GetChildren();

        for (int i = 0; i < chunkBuffer.Count; i++)
        {
            var chunk = (MeshInstance3D)chunkBuffer[i];

            if (chunk.Position == position)
            {
                foundChunk = true;
                retChunk = chunk;
            }
        }

        if (foundChunk)
            return retChunk;
        else
            return null;
    }

    private void clearChunks()
	{
        var chunkBuffer = GetChildren().ToList();

        for (int i = 0; i < chunkBuffer.Count; i++)
        {
            var mesh = (MeshInstance3D)chunkBuffer[i];

            if (mesh.Position.DistanceTo(Game.Player.Position) > 32f)
            {
                chunkBuffer[i].Free();
            }
        }

        //foreach (var child in GetChildren())
        //    child.Free();
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
