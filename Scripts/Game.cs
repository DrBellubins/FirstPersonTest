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

    public static class Game
    {
        public static Player Player;
        public static Car CurrentCar;

        private static Vector3 playerPos = new Vector3();
        public static Vector3 PlayerPos
        {
            get { return playerPos; }
            internal set { playerPos = value; }
        }
    
        public static Vector2 GetNearestChunkCoord(Vector2 input)
        {
            int x = (int)MathF.Floor(input.X);
            int y = (int)MathF.Floor(input.Y);
    
            int xRem = x % TerrainGenerator.ChunkSize;
            int yRem = y % TerrainGenerator.ChunkSize;
    
            return new Vector2(x - xRem, y - yRem);
        }
    
        public static Vector2I GetNearestChunkCoord(Vector2I input)
        {
            int xRem = input.X % TerrainGenerator.ChunkSize;
            int yRem = input.Y % TerrainGenerator.ChunkSize;
    
            return new Vector2I(input.X - xRem, input.Y - yRem);
        }
    }
}