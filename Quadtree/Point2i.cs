using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quadtree
{
    public struct Point2i
    {
        public int X;
        public int Y;

        public Point2i(int x, int y)
        {
            X = x;
            Y = y;
        }

        public float Length()
        {
            return (float)Math.Sqrt(X * X + Y * Y);
        }

        public static Point2i operator -(Point2i v)
        {
            return new Point2i(-v.X, -v.Y);
        }

        public static Point2i operator -(Point2i v1, Point2i v2)
        {
            return new Point2i(v1.X - v2.X, v1.Y - v2.Y);
        }

        public static Point2i operator +(Point2i v1, Point2i v2)
        {
            return new Point2i(v1.X + v2.X, v1.Y + v2.Y);
        }

        public static Point2i operator *(Point2i v1, int mult)
        {
            return new Point2i(v1.X * mult, v1.Y * mult);
        }

        public static Point2i operator *(int mult, Point2i v1)
        {
            return new Point2i(v1.X * mult, v1.Y * mult);
        }

        public override string ToString()
        {
            return "[" + X + "i, " + Y + "i]";
        }
    }
}
