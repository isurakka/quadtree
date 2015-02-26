using FarseerPhysics;
using Microsoft.Xna.Framework;
using SFML.Window;

namespace Quadtree.Examples
{
    public static class ConvertManager
    {
        public static float SimToDisplay(this float meter)
        {
            //return meter * OneMeter;
            return ConvertUnits.ToDisplayUnits(meter);
        }

        public static Vector2f SimToDisplay(this Vector2f meter)
        {
            //return meter * OneMeter;

            return ConvertUnits.ToDisplayUnits(meter.ToXNA()).ToSFML();
        }

        public static float DisplayToSim(this float pixel)
        {
            //return pixel / OneMeter;

            return ConvertUnits.ToSimUnits(pixel);
        }

        public static Vector2f DisplayToSim(this Vector2f pixel)
        {
            //return pixel / OneMeter;

            return ConvertUnits.ToSimUnits(pixel.ToXNA()).ToSFML();
        }

        public static Vector2f ToSFML(this Vector2 vec)
        {
            return new Vector2f(vec.X, vec.Y);
        }

        public static Vector2 ToXNA(this Vector2f vec)
        {
            return new Vector2(vec.X, vec.Y);
        }
    }
}