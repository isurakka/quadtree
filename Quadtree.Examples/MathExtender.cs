using System;
using System.Diagnostics;

namespace Quadtree.Examples
{
    public static class MathExtender
    {
        private const float piDivHalf = (float)(Math.PI / 180d);
        private const float halfDivPi = (float)(180d / Math.PI);
        private static readonly float PIf = (float)Math.PI;

        public static float DegreeToRadian(float degree)
        {
            Debug.Assert(!float.IsNaN(degree) || !float.IsNegativeInfinity(degree) || !float.IsPositiveInfinity(degree));
            return degree * piDivHalf;
        }

        public static float RadianToDegree(float radian)
        {
            Debug.Assert(!float.IsNaN(radian) || !float.IsNegativeInfinity(radian) || !float.IsPositiveInfinity(radian));
            return radian * halfDivPi;
        }

        public static float CosineInterpolate(float a, float b, float x)
        {
            var ft = x * PIf;
            var f = (1f - (float)Math.Cos(ft)) * 0.5f;

            return a * (1f - f) + b * f;
        }

        public static float IEEERemainder(float a, float b)
        {
            return (float)Math.IEEERemainder(a, b);
        }

        public static float NormalizeDegrees(float deg)
        {
            while (deg < 0f)
                deg += 360f;

            deg %= 360f;
            return deg;
        }
    }
}