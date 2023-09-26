using Godot;
using System;
using System.Collections.Concurrent;

public class Game
{
	public static Player Player;

    public static Vector3 GetNearestChunkCoord(Vector3 input)
    {
        int x = (int)MathF.Floor(input.X);
        int z = (int)MathF.Floor(input.Z);

        int xRem = x % 16;
        int zRem = z % 16;

        return new Vector3(x - xRem, 0.0f, z - zRem);
    }
}

public partial class GlobalVariables : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Game.Player = GetParent().GetNode<Player>("Player");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

/// <summary>
/// This class lets you safely call some code to access the Node/Scene tree from a Task / worker thread.  For example updating UI labels etc to tell the user some work is complete.
/// Godot 4.1 is more strict than 4.0 around threads.  It throws errors if you access nodes from threads other then the main thread running _Process().
/// </summary>
public class MainThreadInvoker
{
    private static ConcurrentQueue<Action> deferredActionQueue = new ConcurrentQueue<Action>();

    private static double deltaTotal = 0;

    // This is the interval in seconds that we check the concurrent queue. For example 0.1 will check 10 times per second.
    public static double CheckIntervalSeconds = 0.1;

    /// <summary>
    /// Call this passing an action from your Task or worker Thread.  The action will be run on the main Process thread (eg: updating UI etc).
    /// Therefore you should avoid doing too much processing in this action to ensure the scene remains responsive.
    /// Note: no parameters are needed because you can use the usual C# 'capture' feature to access any variables in scope before calling this.
    /// </summary>
    /// <param name="action"></param>
    public static void InvokeOnMainThread(Action action)
    {
        deferredActionQueue.Enqueue(action);
    }


    /// <summary>
    /// This method should be called from the your scene _Process() method, passing it the delta value. 
    /// It only checks the queue every so often to avoid wasting processing time.  Interval is configurable by changing the value of the CheckIntervalSeconds field
    /// </summary>
    /// <param name="delta"></param>
    public static void ProcessMainThreadQueue(double delta)
    {
        deltaTotal += delta;

        if (deltaTotal > CheckIntervalSeconds)
        {
            deltaTotal = deltaTotal - CheckIntervalSeconds;

            // NB: All actions in the queue will be processed now on the main Process thread, so you should avoid big processing task here to keep responsiveness.
            while (deferredActionQueue.TryDequeue(out Action action))
            {
                action();
            };
        }
    }
}