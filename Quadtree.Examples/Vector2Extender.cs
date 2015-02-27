using System;
using System.Diagnostics;
using SFML.Window;

namespace Quadtree.Examples
{
    public static class Vector2fExtender
    {
        public static Vector2f ToF(this Vector2i vec)
        {
            return new Vector2f(vec.X, vec.Y);
        }

        public static Vector2i ToI(this Vector2f vec)
        {
            return new Vector2i((int)vec.X, (int)vec.Y);
        }

        public static Vector2f Normalize(this Vector2f vec)
        {
            Debug.Assert(!float.IsNaN(vec.X));
            Debug.Assert(!float.IsNaN(vec.Y));
            var len = (float)Math.Sqrt(Math.Pow(vec.X, 2) + Math.Pow(vec.Y, 2));
            Debug.Assert(!float.IsNaN(len));
            return new Vector2f(len != 0f ? vec.X / len : 0f, len != 0f ? vec.Y / len : 0f);
        }

        public static float DotProduct(this Vector2f vec1, Vector2f vec2)
        {
            return vec1.X * vec2.X + vec1.Y * vec2.Y;
        }

        public static float GetRotation(this Vector2f v)
        {
            var norm = v.Normalize();
            return MathExtender.RadianToDegree((float)Math.Atan2(norm.Y, norm.X));
        }

        public static Vector2f RotateDegrees(this Vector2f vec, float angle)
        {
            angle = MathExtender.DegreeToRadian(angle);
            return vec.RotateRadians(angle);
        }

        public static Vector2f RotateRadians(this Vector2f vec, float angle)
        {
            var newVec = new Vector2f(0, 0);
            double vecX = vec.X;
            double vecY = vec.Y;
            newVec.X = (float)(vecX * Math.Cos(angle) - vecY * Math.Sin(angle));
            newVec.Y = (float)(vecX * Math.Sin(angle) + vecY * Math.Cos(angle));
            return newVec;
        }
    }
}