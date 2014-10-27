using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quadtree
{
    public class RegionQuadtree<T> : IEnumerable<T>
        where T: struct
    {
        private readonly int resolution;
        private readonly int depth;

        private T? value;
        private RegionQuadtree<T> northWest; // 2
        private RegionQuadtree<T> northEast; // 3
        private RegionQuadtree<T> southEast; // 1
        private RegionQuadtree<T> southWest; // 0

        // For convince (iterate through children)
        private List<RegionQuadtree<T>> children;

        private RegionQuadtree<T> parent;

        private AABB2i aabb;
        public AABB2i AABB
        {
            get { return aabb; }
        }

        public QuadType Type
        {
            get
            {
                if (value == null && northWest != null)
                    return QuadType.Grey;

                if (value != null && northWest == null)
                    return QuadType.Black;

                if (value == null && northWest == null)
                    return QuadType.White;

                throw new InvalidOperationException("Quadtree state is broken.");
            }
        }

        public event EventHandler<QuadEventArgs<T>> OnQuadAdded;
        public event EventHandler<QuadEventArgs<T>> OnQuadRemoving;

        public class QuadEventArgs<T> : EventArgs
            where T: struct
        {
            private RegionQuadtree<T> quadTree;

            public AABB2i AABB
            {
                get { return quadTree.aabb; }
            }

            public T Value
            {
                get { return quadTree.value.Value; }
            }

            public QuadEventArgs(RegionQuadtree<T> quadTree)
            {
                this.quadTree = quadTree;
            }
        }

        public RegionQuadtree(int resolution)
        {
            if (resolution < 0)
                throw new ArgumentException("Resolution can't be negative.");

            this.resolution = resolution;
            this.depth = 0;

            var size = (int)Math.Pow(2, resolution);
            if (size < 0)
                throw new ArgumentException("resolution is too high");

            this.aabb = new AABB2i(
                new Point2i(0, 0),
                new Point2i(size, size));
        }

        private RegionQuadtree(int resolution, int depth, T? value, RegionQuadtree<T> parent, AABB2i aabb)
        {
            this.resolution = resolution;
            this.depth = depth;
            this.value = value;
            this.parent = parent;
            this.aabb = aabb;

            if (value != null)
            {
                propagateEvent(true);
            }
        }

        private void propagateEvent(bool added, RegionQuadtree<T> start = null)
        {
            var always = start ?? this;
            var par = start ?? this;
            while (par != null)
            {
                var eve = added ? par.OnQuadAdded : par.OnQuadRemoving;
                if (eve != null)
                {
                    eve(par, new QuadEventArgs<T>(always));
                }

                par = par.parent;
            }
        }

        public bool Set(T value)
        {
            return Set(ref value);
        }

        public bool Set(ref T value)
        {
            var ret = setInternal(ref value);
            if (ret)
            {
                unsubdivide();
            }
            return ret;
        }

        private bool setInternal(ref T value)
        {
            if (Type == QuadType.Black && this.value.Value.Equals(value))
            {
                return false;
            }

            unsetInternal();

            this.value = value;

            propagateEvent(true);

            return true;
        }

        public bool SetCircle(Point2i point, int radius, T value)
        {
            var rectSize = (int)(radius / Math.Sqrt(2));   
            var testAABB = new AABB2i(point - new Point2i(rectSize, rectSize), point + new Point2i(rectSize, rectSize));
            bool anySet = false;
            for (int i = point.X - radius; i <= point.X + radius; i++)
            {
                for (int j = point.Y - radius; j <= point.Y + radius; j++)
                {
                    var currentPoint = new Point2i(i, j);

                    if (testAABB.Contains(currentPoint))
                        continue;

                    if ((point - currentPoint).Length() < radius)
                    {
                        Debug.WriteLine(Type);
                        if (setInternal(ref currentPoint, ref value))
                        {
                            anySet = true;
                        }
                        Debug.WriteLine(Type);
                    }
                }
            }

            Debug.WriteLine(Type);
            var ret = setAABBInternal(ref testAABB, ref value);
            Debug.WriteLine(Type);
            if (anySet || ret)
            {
                unsubdivide();
            }

            return ret;
        }

        public bool SetAABB(AABB2i aabb, T value)
        {
            return SetAABB(ref aabb, ref value);
        }

        public bool SetAABB(ref AABB2i aabb, ref T value)
        {
            var ret = setAABBInternal(ref aabb, ref value);
            if (ret)
            {
                unsubdivide();
            }

            return ret;
        }

        private bool setAABBInternal(ref AABB2i aabb, ref T value)
        {
            bool contains = aabb.Contains(ref this.aabb);
            bool intersects = aabb.Intersects(ref this.aabb);

            if (!contains && intersects)
            {
                bool subdivided = false;
                var canSub = depth < resolution;
                if (canSub && Type != QuadType.Grey)
                {
                    subdivide();
                    subdivided = true;
                }

                bool anyChild = false;
                if (canSub)
                {
                    foreach (var quad in children)
                    {
                        if (quad.setAABBInternal(ref aabb, ref value))
                        {
                            anyChild = true;
                        }
                    }
                }
                else
                {
                    Debug.WriteLine(Type);
                    return false;
                }

                Debug.WriteLine(Type);
                return anyChild || subdivided;
            }
            else if (contains)
            {
                Debug.WriteLine(Type);
                return setInternal(ref value);
            }
            else
            {
                Debug.WriteLine(Type);
                return false;
            }

            throw new InvalidOperationException("Set didn't fail nor succeed. This is not supposed to happen!");
        }

        public bool Set(Point2i point, T value)
        {
            return Set(ref point, ref value);
        }

        public bool Set(ref Point2i point, ref T value)
        {
            var ret = setInternal(ref point, ref value);
            if (ret)
            {
                unsubdivide();
            }
            return ret;
        }

        private bool setInternal(ref Point2i point, ref T value)
        {
            if (!aabb.Contains(point))
                return false;

            if (Type == QuadType.Black && this.value.Value.Equals(value))
            {
                return false;
            }

            if (depth < resolution)
            {
                if (Type != QuadType.Grey)
                {
                    subdivide();
                }
            }
            else
            {
                return setInternal(ref value);
            }

            foreach (var quad in children)
            {
                if (quad.setInternal(ref point, ref value))
                {
                    return true;
                }
            }

            return false;

            throw new InvalidOperationException("Set didn't fail nor succeed. This is not supposed to happen!");
        }

        public bool Unset()
        {
            var ret = unsetInternal();
            if (ret)
            {
                unsubdivide();
            }

            return ret;
        }

        private bool unsetInternal()
        {
            if (Type == QuadType.White)
            {
                return false;
            }

            if (Type == QuadType.Black)
            {
                propagateEvent(false);

                this.value = null;

                return true;
            }

            if (Type == QuadType.Grey)
            {
                bool any = false;
                foreach (var quad in children)
                {
                    if (quad.unsetInternal())
                    {
                        any = true;
                    }
                }

                return any;
            }

            throw new InvalidOperationException("This is not supposed to happen!");
        }

        public bool Unset(Point2i point)
        {
            return Unset(ref point);
        }

        public bool Unset(ref Point2i point)
        {
            var ret = unsetInternal(ref point);
            unsubdivide();
            return ret;
        }

        private bool unsetInternal(ref Point2i point)
        {
            if (!aabb.Contains(point))
                return false;

            if (Type == QuadType.White)
            {
                return false;
            }

            if (depth < resolution)
            {
                if (Type != QuadType.Grey)
                {
                    subdivide();
                }
            }
            else
            {
                propagateEvent(false);

                this.value = null;

                return true;
            }

            foreach (var quad in children)
            {
                if (quad.Unset(ref point))
                {
                    return true;
                }
            }

            return false;

            throw new InvalidOperationException("This is not supposed to happen!");
        }

        private void subdivide()
        {
            northWest = new RegionQuadtree<T>(resolution, depth + 1, value, this, new AABB2i(
                aabb.LowerBound,
                aabb.LowerBound + new Point2i(aabb.Width / 2, aabb.Height / 2)));
            northEast = new RegionQuadtree<T>(resolution, depth + 1, value, this, new AABB2i(
                aabb.LowerBound + new Point2i(aabb.Width / 2, 0),
                aabb.LowerBound + new Point2i(aabb.Width, aabb.Height / 2)));
            southEast = new RegionQuadtree<T>(resolution, depth + 1, value, this, new AABB2i(
                aabb.LowerBound + new Point2i(aabb.Width / 2, aabb.Height / 2),
                aabb.LowerBound + new Point2i(aabb.Width, aabb.Height)));
            southWest = new RegionQuadtree<T>(resolution, depth + 1, value, this, new AABB2i(
                aabb.LowerBound + new Point2i(0, aabb.Height / 2),
                aabb.LowerBound + new Point2i(aabb.Width / 2, aabb.Height)));

            children = new List<RegionQuadtree<T>>()
            {
                northWest, northEast, southEast, southWest
            };

            if (this.value != null)
            {
                propagateEvent(false);

                value = null;
            }
        }

        private bool unsubdivide()
        {
            if (Type != QuadType.Grey)
                return false;

            var northWestValue = northWest.value;

            bool allBlack = true;
            bool allWhite = true;
            foreach (var child in children)
            {
                if (!(child.Type == QuadType.Black && northWestValue.Equals(child.value.Value)))
                {
                    allBlack = false;
                }

                if (!(child.Type == QuadType.White))
                {
                    allWhite = false;
                }

                if (!allBlack && !allWhite)
                {
                    break;
                }
            }

            if (allBlack)
            {
                RegionQuadtree<T> par;
                foreach (var quad in children)
                {
                    propagateEvent(false, quad);
                }

                this.value = northWestValue;

                propagateEvent(true);
            }
            else if (!allWhite)
            {
                bool anySub = false;
                foreach (var quad in children)
                {
                    //if (quad.unsubdivide())
                    //{
                    //    anySub = true;
                    //}
                }

                if (anySub)
                {
                    return unsubdivide();
                }

                return false;
            }

            northWest = null;
            northEast = null;
            southEast = null;
            southWest = null;
            children = null;

            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            switch (Type)
            {
                case QuadType.Black:
                    yield return value.Value;
                    break;
                case QuadType.Grey:
                    foreach (var item in northWest)
                    {
                        yield return item;
                    }

                    foreach (var item in northEast)
                    {
                        yield return item;
                    }

                    foreach (var item in southWest)
                    {
                        yield return item;
                    }

                    foreach (var item in southEast)
                    {
                        yield return item;
                    }

                    break;
                case QuadType.White:
                default:
                    break;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
