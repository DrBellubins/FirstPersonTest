using Godot;
using System;
using System.Collections.Concurrent;

namespace MyUtils
{
    public class Vector3Wrapper
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3Wrapper() {}

        public Vector3Wrapper(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3Wrapper(Vector3 pos)
        {
            this.X = pos.X;
            this.Y = pos.Y;
            this.Z = pos.Z;
        }
    }
}

public static class Game
{
    public static Player Player;

    public static MyUtils.Vector3Wrapper PlayerPos = new MyUtils.Vector3Wrapper();

    public static Vector3 GetPlayerPosition()
    {
        return new Vector3(PlayerPos.X, PlayerPos.Y, PlayerPos.Z);
    }

    public static void SetPlayerPosition(Vector3 pos)
    {
        PlayerPos = new MyUtils.Vector3Wrapper(pos.X, pos.Y, pos.Z);
    }

    public static Vector2 GetNearestChunkCoord(Vector2 input)
    {
        int x = (int)MathF.Floor(input.X);
        int y = (int)MathF.Floor(input.Y);

        int xRem = x % 16;
        int yRem = y % 16;

        return new Vector2(x - xRem, y - yRem);
    }
}
