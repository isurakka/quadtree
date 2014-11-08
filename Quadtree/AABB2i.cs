using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quadtree
{
    public struct AABB2i : IEquatable<AABB2i>
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

        public bool Intersects(ref AABB2i other)
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

        public bool Contains(ref AABB2i aabb)
        {
            bool result = true;
            result = result && LowerBound.X <= aabb.LowerBound.X;
            result = result && LowerBound.Y <= aabb.LowerBound.Y;
            result = result && aabb.UpperBound.X <= UpperBound.X;
            result = result && aabb.UpperBound.Y <= UpperBound.Y;
            return result;
        }

        /*
        public bool Overlap(ref AABB2i aabb)
        {
            bool lowerOverlap = LowerBound.X <= aabb.LowerBound.X && LowerBound.Y <= aabb.LowerBound.Y;
            bool upperOverlap = aabb.UpperBound.X <= UpperBound.X && aabb.UpperBound.Y <= UpperBound.Y;

            return (lowerOverlap || upperOverlap) && !(lowerOverlap && upperOverlap);
        }
        */

        public void Combine(ref AABB2i aabb)
        {
            LowerBound = new Point2i(Math.Min(LowerBound.X, aabb.LowerBound.X), Math.Min(LowerBound.Y, aabb.LowerBound.Y));
            UpperBound = new Point2i(Math.Max(UpperBound.X, aabb.UpperBound.X), Math.Max(UpperBound.Y, aabb.UpperBound.Y));
        }

        public static bool operator ==(AABB2i v1, AABB2i v2)
        {
            return v1.Equals(v2);
        }

        public static bool operator !=(AABB2i v1, AABB2i v2)
        {
            return !v1.Equals(v2);
        }

        public bool Equals(AABB2i other)
        {
            return LowerBound == other.LowerBound && UpperBound == other.UpperBound;
        }

        public override bool Equals(object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
                return false;

            var p = (AABB2i)obj;
            return Equals(p);
        }

        public override int GetHashCode()
        {
            return LowerBound.GetHashCode() ^ UpperBound.GetHashCode();
        }

        public override string ToString()
        {
            return "{" + LowerBound + ", " + UpperBound + "}";
        }
    }
}
