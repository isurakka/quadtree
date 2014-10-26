using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private RegionQuadtree<T> parent;

        private IReadOnlyCollection<RegionQuadtree<T>> Children
        {
            get
            {
                return new List<RegionQuadtree<T>>()
                {
                    northWest, northEast, southEast, southWest
                };
            }
        }

        private AABB2i aabb;
        public AABB2i AABB
        {
            get
            {
                return aabb;
            }
        }

        // TODO
        /*
        private bool reduce()
        {
            if (Type != QuadType.Grey)
                return false;

            var children = new List<RegionQuadtree<T>> { northWest, northEast, southEast, southWest };
            var allFull = children.All(q => q.Type == QuadType.Black);
            var allEmpty = children.All(q => q.Type == QuadType.White);

            //allFull = false;
            allEmpty = false; // empty reducing doesn't work so it is disabled

            if (allFull)
            {
                children.ForEach(q =>
                {
                    q.DestroyAct(q.value);
                    q.value = default(T);
                });
                value = CreateFunc(aabb);
            }

            if (allEmpty)
            {
                value = default(T);
            }

            if (allFull || allEmpty)
            {
                northWest = null;
                northEast = null;
                southEast = null;
                southWest = null;
                return true;
            }

            if (children.Any(q => q.reduce()))
                return reduce();

            return false;
        }
        */

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

        //public delegate void CreateQuadHandler(object sender, EventArgs args);
        public event EventHandler<QuadEventArgs<T>> OnQuadAdded;
        public event EventHandler<QuadEventArgs<T>> OnQuadRemoving;

        public class QuadEventArgs<T> : EventArgs
            where T: struct
        {
            private RegionQuadtree<T> quadTree;

            public AABB2i AABB
            {
                get { return quadTree.AABB; }
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
            this.value = null;
            this.parent = null;

            var size = (int)Math.Pow(2, resolution);
            if (size < 0)
                throw new ArgumentException("resolution is too high");

            this.aabb = new AABB2i(
                new Point2i(0, 0),
                new Point2i(size, size));
        }

        public RegionQuadtree(int resolution, T initialValue)
            : this(resolution)
        {
            this.value = initialValue;

            if (OnQuadAdded != null)
            {
                OnQuadAdded(this, new QuadEventArgs<T>(this));
            }
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
                var par = this;
                while (par != null)
                {
                    if (par.OnQuadAdded != null)
                    {
                        par.OnQuadAdded(this, new QuadEventArgs<T>(this));
                    }

                    par = par.parent;
                }
            }
        }

        public bool Set(Point2i point, T value)
        {
            return Set(ref point, ref value);
        }

        public bool Set(ref Point2i point, ref T value)
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
                this.value = value;

                if (OnQuadAdded != null)
                {
                    OnQuadAdded(this, new QuadEventArgs<T>(this));
                }

                return true;
            }

            foreach (var quad in Children)
            {
                if (quad.Set(ref point, ref value))
                {
                    if (OnQuadAdded != null)
                    {
                        OnQuadAdded(quad, new QuadEventArgs<T>(quad));
                    }

                    return true;
                }
            }

            return false;

            throw new InvalidOperationException("Set didn't fail nor succeed. This is not supposed to happen!");
        }

        public bool Unset(Point2i point)
        {
            return Unset(ref point);
        }

        public bool Unset(ref Point2i point)
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
                if (OnQuadRemoving != null)
                {
                    OnQuadRemoving(this, new QuadEventArgs<T>(this));
                }

                this.value = null;

                return true;
            }

            foreach (var quad in Children)
            {
                if (quad.Unset(ref point))
                {
                    if (OnQuadRemoving != null)
                    {
                        OnQuadRemoving(quad, new QuadEventArgs<T>(quad));
                    }

                    return true;
                }
            }

            return false;

            throw new InvalidOperationException("This is not supposed to happen!");
        }

        /*
        public bool SetCircle(ref Point2i point, ref int radius, ref bool exists)
        {
            for (int i = point.X - radius; i <= point.X + radius; i++)
            {
                for (int j = point.Y - radius; j <= point.Y + radius; j++)
                {
                    var currentPoint = new Point2i(i, j);
                    if ((point - currentPoint).Length() <= radius)
                    {
                        Set(ref currentPoint, ref exists);
                    }
                }
            }

            reduce();

            return true;
        }
        */

        private bool subdivide()
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

            if (value != null)
            {
                var par = this;
                while (par != null)
                {
                    if (par.OnQuadRemoving != null)
                    {
                        par.OnQuadRemoving(this, new QuadEventArgs<T>(this));
                    }

                    par = par.parent;
                }

                value = null;
            }

            return false;
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
