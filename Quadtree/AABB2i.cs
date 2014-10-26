using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quadtree
{
    public struct AABB2i
    {
        public Point2i LowerBound;
        public Point2i UpperBound;

        public AABB2i(Point2i min, Point2i max)
        {
            LowerBound = min;
            UpperBound = max;
        }

        public int Width
        {
            get { return UpperBound.X - LowerBound.X; }
        }

        public int Height
        {
            get { return UpperBound.Y - LowerBound.Y; }
        }

        public bool Intersects(AABB2i other)
        {
            var d1 = other.LowerBound - UpperBound;
            var d2 = LowerBound - other.UpperBound;

            if (d1.X > 0.0f || d1.Y > 0.0f)
                return false;

            if (d2.X > 0.0f || d2.Y > 0.0f)
                return false;

            return true;
        }

        public bool Contains(Point2i point)
        {
            return (point.X >= (LowerBound.X) && point.X < (UpperBound.X) &&
                   (point.Y >= (LowerBound.Y) && point.Y < (UpperBound.Y)));
        }
    }
}
