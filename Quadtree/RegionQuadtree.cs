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
                        par.OnQuadAdded(par, new QuadEventArgs<T>(this));
                    }

                    par = par.parent;
                }
            }
        }

        public void Set(T value)
        {
            Set(ref value);
        }

        public void Set(ref T value)
        {
            if (Type == QuadType.Black && this.value.Value.Equals(value))
            {
                return;
            }

            Unset();

            RegionQuadtree<T> par;
            
            this.value = value;

            par = this;
            while (par != null)
            {
                if (par.OnQuadAdded != null)
                {
                    par.OnQuadAdded(par, new QuadEventArgs<T>(this));
                }

                par = par.parent;
            }
        }

        public bool SetCircle(Point2i point, int radius, T value)
        {
            var rectSize = (int)(radius / Math.Sqrt(2));   
            var testAABB = new AABB2i(point - new Point2i(rectSize, rectSize), point + new Point2i(rectSize, rectSize));
            for (int i = point.X - radius; i <= point.X + radius; i++)
            {
                for (int j = point.Y - radius; j <= point.Y + radius; j++)
                {
                    var currentPoint = new Point2i(i, j);
                    if ((point - currentPoint).Length() < rectSize)
                        continue;

                    if ((point - currentPoint).Length() < radius)
                    {
                        Set(ref currentPoint, ref value);
                    }
                }
            }
            var ret = setAABBInternal(ref testAABB, ref value);
            unsubdivide();
            return ret;
        }

        public bool SetAABB(AABB2i aabb, T value)
        {
            return SetAABB(ref aabb, ref value);
        }

        public bool SetAABB(ref AABB2i aabb, ref T value)
        {
            var ret = setAABBInternal(ref aabb, ref value);
            unsubdivide();
            return ret;
        }

        private bool setAABBInternal(ref AABB2i aabb, ref T value, Predicate<AABB2i> predicate = null)
        {
            bool contains = aabb.Contains(ref this.aabb);
            bool intersects = aabb.Intersects(ref this.aabb);

            if (!contains && intersects)
            {
                var canSub = depth < resolution;
                if (canSub && Type != QuadType.Grey)
                {
                    subdivide();
                }

                if (canSub)
                {
                    foreach (var quad in Children)
                    {
                        quad.setAABBInternal(ref aabb, ref value, predicate);
                    }
                }
                else
                {
                    return false;
                }

                return true;
            }
            else if (contains)
            {
                if (predicate != null && !predicate(this.aabb))
                {
                    return false;
                }

                Set(ref value);
                return true;
            }
            else
            {
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
            unsubdivide();
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
                RegionQuadtree<T> par;
                if (this.value != null && !value.Equals(this.value.Value))
                {
                    par = this;
                    while (par != null)
                    {
                        if (par.OnQuadRemoving != null)
                        {
                            par.OnQuadRemoving(par, new QuadEventArgs<T>(this));
                        }

                        par = par.parent;
                    }
                }

                this.value = value;

                par = this;
                while (par != null)
                {
                    if (par.OnQuadAdded != null)
                    {
                        par.OnQuadAdded(par, new QuadEventArgs<T>(this));
                    }

                    par = par.parent;
                }

                

                return true;
            }

            foreach (var quad in Children)
            {
                if (quad.Set(ref point, ref value))
                {
                    return true;
                }
            }

            return false;

            throw new InvalidOperationException("Set didn't fail nor succeed. This is not supposed to happen!");
        }

        public void Unset()
        {
            if (Type == QuadType.White)
            {
                return;
            }

            if (Type == QuadType.Black)
            {
                var par = this;
                while (par != null)
                {
                    if (par.OnQuadRemoving != null)
                    {
                        par.OnQuadRemoving(par, new QuadEventArgs<T>(this));
                    }

                    par = par.parent;
                }

                this.value = null;
            }

            if (Type == QuadType.Grey)
            {
                foreach (var quad in Children)
                {
                    quad.Unset();
                }
            }

            unsubdivide();
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
                var par = this;
                while (par != null)
                {
                    if (par.OnQuadRemoving != null)
                    {
                        par.OnQuadRemoving(par, new QuadEventArgs<T>(this));
                    }

                    par = par.parent;
                }

                this.value = null;

                return true;
            }

            foreach (var quad in Children)
            {
                if (quad.Unset(ref point))
                {
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

            if (value != null)
            {
                var par = this;
                while (par != null)
                {
                    if (par.OnQuadRemoving != null)
                    {
                        par.OnQuadRemoving(par, new QuadEventArgs<T>(this));
                    }

                    par = par.parent;
                }

                value = null;
            }
        }

        private bool unsubdivide()
        {
            if (Type != QuadType.Grey)
                return false;

            var northWestValue = northWest.value;
            var allBlack = Children.All(q => q.Type == QuadType.Black && northWestValue.Equals(q.value.Value));
            var allWhite = Children.All(q => q.Type == QuadType.White);

            if (allBlack)
            {
                RegionQuadtree<T> par;
                foreach (var quad in Children)
                {
                    par = quad;
                    while (par != null)
                    {
                        if (par.OnQuadRemoving != null)
                        {
                            par.OnQuadRemoving(par, new QuadEventArgs<T>(quad));
                        }

                        par = par.parent;
                    }
                }

                this.value = northWestValue;

                par = this;
                while (par != null)
                {
                    if (par.OnQuadAdded != null)
                    {
                        par.OnQuadAdded(par, new QuadEventArgs<T>(this));
                    }

                    par = par.parent;
                }
            }
            else if (!allWhite)
            {
                bool anySub = false;
                foreach (var quad in Children)
                {
                    if (quad.unsubdivide())
                    {
                        anySub = true;
                    }
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
