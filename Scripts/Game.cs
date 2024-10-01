using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;

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

        public static List<Vector3> LightPositions = new List<Vector3>();

        public static bool IsDebugVisible = true; // Change for default debug on/off

        private static Vector3 playerPos = new Vector3();
        public static Vector3 PlayerPos
        {
            get { return playerPos; }
            internal set { playerPos = value; }
        }
    
        public static Vector2 GetNearestCoord(Vector2 input, int numerator)
        {
            int x = (int)MathF.Floor(input.X);
            int y = (int)MathF.Floor(input.Y);
    
            int xRem = x % numerator;
            int yRem = y % numerator;
    
            return new Vector2(x - xRem, y - yRem);
        }
    
        public static Vector2I GetNearestCoord(Vector2I input, int numerator)
        {
            int xRem = input.X % numerator;
            int yRem = input.Y % numerator;
    
            return new Vector2I(input.X - xRem, input.Y - yRem);
        }

        public static bool InRange(float val,  float min, float max)
        {
            return val < max && val > min;
        }
    }
}