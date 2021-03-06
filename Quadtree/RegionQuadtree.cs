﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QDO = Quadtree.QuadDirectionOperation;

namespace Quadtree
{
    public class RegionQuadtree<T> : IEnumerable<T>
        where T: struct
    {
        private int depth;

        private T? value;
        private RegionQuadtree<T>[] quads;

        private RegionQuadtree<T> parent;

        private int resolution;
        public int Resolution
        {
            get { return resolution; }
        }

        private AABB2i aabb;
        public AABB2i AABB
        {
            get { return aabb; }
        }

        private RegionQuadtree<T> this[QuadDirection d]
        {
            get
            {
                switch (d)
                {
                    case QuadDirection.NorthWest:
                        return quads[0];
                    case QuadDirection.NorthEast:
                        return quads[1];
                    case QuadDirection.SouthEast:
                        return quads[2];
                    case QuadDirection.SouthWest:
                        return quads[3];
                    default:
                        throw new ArgumentException("Not valid quad direction");
                }
            }
            set
            {
                switch (d)
                {
                    case QuadDirection.NorthWest:
                        quads[0] = value;
                        break;
                    case QuadDirection.NorthEast:
                        quads[1] = value;
                        break;
                    case QuadDirection.SouthEast:
                        quads[2] = value;
                        break;
                    case QuadDirection.SouthWest:
                        quads[3] = value;
                        break;
                    default:
                        throw new ArgumentException("Not valid quad direction");
                }
            }
        }

        public QuadType Type
        {
            get
            {
                if (value == null && quads != null)
                    return QuadType.Grey;

                if (value != null && quads == null)
                    return QuadType.Black;

                if (value == null && quads == null)
                    return QuadType.White;

                throw new InvalidOperationException("Quadtree state is broken.");
            }
        }

        public event EventHandler<QuadEventArgs<T>> OnQuadAdded;
        public event EventHandler<QuadEventArgs<T>> OnQuadRemoving;
        public event EventHandler<QuadChangedEventArgs<T>> OnQuadChanged;
        public event EventHandler<QuadExpandEventArgs<T>> OnExpand;

        private bool autoExpand = false;
        public bool AutoExpand 
        { 
            get
            {
                return autoExpand;
            }
            set
            {
                autoExpand = value;
            }
        }

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

        public class QuadChangedEventArgs<T> : QuadEventArgs<T>
            where T : struct
        {
            private T oldValue;
            public T OldValue
            {
                get { return oldValue; }
            }

            public QuadChangedEventArgs(RegionQuadtree<T> quadTree, T oldValue)
                : base(quadTree)
            {
                this.oldValue = oldValue;
            }
        }

        public class QuadExpandEventArgs<T> : EventArgs
            where T : struct
        {
            private RegionQuadtree<T> newRoot;
            public RegionQuadtree<T> NewRoot
            {
                get { return newRoot; }
            }

            private Point2i offset;
            public Point2i Offset
            {
                get
                {
                    return offset;
                }
            }

            public QuadExpandEventArgs(RegionQuadtree<T> newRoot, Point2i offset)
            {
                this.newRoot = newRoot;
                this.offset = offset;
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
                propagateEvent(EventType.Added);
            }
        }

        private enum EventType
        {
            Added,
            Removing,
            Changed,
        }

        private void propagateEvent(EventType eventType, RegionQuadtree<T> start = null, T? oldValue = null)
        {
            var always = start ?? this;
            var par = start ?? this;
            while (par != null)
            {
                if (eventType == EventType.Added)
                {
                    if (par.OnQuadAdded != null)
                    {
                        par.OnQuadAdded(par, new QuadEventArgs<T>(always));
                    }
                }
                else if (eventType == EventType.Removing)
                {
                    if (par.OnQuadRemoving != null)
                    {
                        par.OnQuadRemoving(par, new QuadEventArgs<T>(always));
                    }
                }
                else
                {
                    if (par.OnQuadChanged != null)
                    {
                        if (oldValue == null)
                        {
                            throw new ArgumentException("Old value shouldn't be null");
                        }

                        par.OnQuadChanged(par, new QuadChangedEventArgs<T>(always, oldValue.Value));
                    }
                }

                par = par.parent;
            }
        }

        private RegionQuadtree<T> findRoot()
        {
            if (parent != null)
            {
                return parent.findRoot();
            }

            return this;
        }

        public bool Set(T value)
        {
            return Set(ref value);
        }

        public bool Set(ref T value)
        {
            var anySet = setInternal(ref value);
            if (anySet)
            {
                unsubdivide();
            }
            return anySet;
        }

        private bool setInternal(ref T value)
        {
            if (Type == QuadType.Black)
            {
                if (this.value.Value.Equals(value))
                {
                    return false;
                }
                else
                {
                    var oldValue = this.value;
                    this.value = value;
                    propagateEvent(EventType.Changed, null, oldValue);
                    return true;
                }
                
            }

            // TODO: Figure out why unsetInternal can't be used here.
            Unset();
            //unsetInternal();

            this.value = value;

            propagateEvent(EventType.Added);

            return true;
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

                    if (testAABB.Contains(currentPoint))
                        continue;

                    if ((point - currentPoint).Length() < radius)
                    {
                        if (isPointOutside(currentPoint))
                        {
                            if (findRoot() == this && AutoExpand)
                            {
                                RegionQuadtree<T> newRoot = this;
                                if (newRoot.isPointOutside(currentPoint))
                                {
                                    newRoot = newRoot.ExpandFromCenter();
                                    point += new Point2i(newRoot.aabb.Width / 4, newRoot.aabb.Height / 4);
                                }

                                return newRoot.SetCircle(point, radius, value);
                            }
                        }
                    }
                }
            }

            bool anyOuterSet = false;
            for (int i = point.X - radius; i <= point.X + radius; i++)
            {
                for (int j = point.Y - radius; j <= point.Y + radius; j++)
                {
                    var currentPoint = new Point2i(i, j);

                    if (testAABB.Contains(currentPoint))
                        continue;

                    if ((point - currentPoint).Length() < radius)
                    {
                        anyOuterSet |= setInternal(ref currentPoint, ref value);
                    }
                }
            }

            var anyAABBSet = setAABBInternal(ref testAABB, ref value);
            if (anyOuterSet || anyAABBSet)
            {
                unsubdivide();
            }

            return anyOuterSet || anyAABBSet;
        }

        public bool SetAABB(AABB2i aabb, T value)
        {
            return SetAABB(ref aabb, ref value);
        }

        public bool SetAABB(ref AABB2i aabb, ref T value)
        {
            var setAny = setAABBInternal(ref aabb, ref value);

            if (setAny)
            {
                unsubdivide();
            }

            return setAny;
        }

        private bool setAABBInternal(ref AABB2i aabb, ref T value)
        {
            bool otherContains = aabb.Contains(ref this.aabb);
            bool otherIntersects = aabb.Intersects(ref this.aabb);

            if (!otherContains && otherIntersects)
            {
                if (Type == QuadType.Black && this.value.Value.Equals(value))
                {
                    return false;
                }

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
                    foreach (var quad in quads)
                    {
                        anyChild |= quad.setAABBInternal(ref aabb, ref value);
                    }
                }

                return anyChild || subdivided;
            }
            else if (otherContains)
            {
                return setInternal(ref value);
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
            var anySet = setInternal(ref point, ref value);
            if (anySet)
            {
                unsubdivide();
            }

            return anySet;
        }

        private bool setInternal(ref Point2i point, ref T value)
        {
            if (isPointOutside(point))
            {
                return false;
            }

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

            foreach (var quad in quads)
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
            var anyUnset = unsetInternal();
            if (anyUnset)
            {
                unsubdivide();
            }

            return anyUnset;
        }

        private bool unsetInternal()
        {
            if (Type == QuadType.White)
            {
                return false;
            }

            if (Type == QuadType.Black)
            {
                propagateEvent(EventType.Removing);

                this.value = null;

                return true;
            }

            if (Type == QuadType.Grey)
            {
                bool any = false;
                foreach (var quad in quads)
                {
                    any |= quad.unsetInternal();
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
            var anyUnset = unsetInternal(ref point);
            if (anyUnset)
            {
                unsubdivide();
            }

            return anyUnset;
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
                propagateEvent(EventType.Removing);

                this.value = null;

                return true;
            }

            foreach (var quad in quads)
            {
                if (quad.unsetInternal(ref point))
                {
                    return true;
                }
            }

            return false;

            throw new InvalidOperationException("This is not supposed to happen!");
        }

        public bool UnsetAABB(AABB2i aabb)
        {
            return UnsetAABB(ref aabb);
        }

        public bool UnsetAABB(ref AABB2i aabb)
        {
            var unsetAny = unsetAABBInternal(ref aabb);

            if (unsetAny)
            {
                unsubdivide();
            }

            return unsetAny;
        }

        private bool unsetAABBInternal(ref AABB2i aabb)
        {
            bool otherContains = aabb.Contains(ref this.aabb);
            bool otherIntersects = aabb.Intersects(ref this.aabb);

            if (!otherContains && otherIntersects)
            {
                if (Type == QuadType.White)
                {
                    return false;
                }

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
                    foreach (var quad in quads)
                    {
                        anyChild |= quad.unsetAABBInternal(ref aabb);
                    }
                }

                return anyChild || subdivided;
            }
            else if (otherContains)
            {
                return unsetInternal();
            }
            else
            {
                return false;
            }

            throw new InvalidOperationException("Set didn't fail nor succeed. This is not supposed to happen!");
        }

        public bool UnsetCircle(Point2i point, int radius)
        {
            var rectSize = (int)(radius / Math.Sqrt(2));
            var testAABB = new AABB2i(
                point - new Point2i(rectSize - 1, rectSize - 1),
                point + new Point2i(rectSize - 1, rectSize - 1));

            bool anyOuterUnset = false;
            for (int i = point.X - radius; i <= point.X + radius; i++)
            {
                for (int j = point.Y - radius; j <= point.Y + radius; j++)
                {
                    var currentPoint = new Point2i(i, j);

                    if (testAABB.Contains(currentPoint))
                        continue;

                    if ((point - currentPoint).Length() <= radius)
                    {
                        anyOuterUnset |= unsetInternal(ref currentPoint);
                    }
                }
            }

            var anyAABBUnset = unsetAABBInternal(ref testAABB);
            if (anyOuterUnset || anyAABBUnset)
            {
                unsubdivide();
            }

            return anyOuterUnset || anyAABBUnset;
        }

        private void subdivide()
        {
            var oldValue = value;
            if (this.value != null)
            {
                propagateEvent(EventType.Removing);

                value = null;
            }

            quads = new RegionQuadtree<T>[4];
            quads[0] = new RegionQuadtree<T>(resolution, depth + 1, oldValue, this, new AABB2i(
                aabb.LowerBound,
                aabb.LowerBound + new Point2i(aabb.Width / 2, aabb.Height / 2)));
            quads[1] = new RegionQuadtree<T>(resolution, depth + 1, oldValue, this, new AABB2i(
                aabb.LowerBound + new Point2i(aabb.Width / 2, 0),
                aabb.LowerBound + new Point2i(aabb.Width, aabb.Height / 2)));
            quads[2] = new RegionQuadtree<T>(resolution, depth + 1, oldValue, this, new AABB2i(
                aabb.LowerBound + new Point2i(aabb.Width / 2, aabb.Height / 2),
                aabb.LowerBound + new Point2i(aabb.Width, aabb.Height)));
            quads[3] = new RegionQuadtree<T>(resolution, depth + 1, oldValue, this, new AABB2i(
                aabb.LowerBound + new Point2i(0, aabb.Height / 2),
                aabb.LowerBound + new Point2i(aabb.Width / 2, aabb.Height)));
        }

        private bool unsubdivide()
        {
            if (Type != QuadType.Grey)
                return false;

            var northWestValue = quads[0].value;

            bool allBlack = true;
            bool allWhite = true;
            foreach (var child in quads)
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
                foreach (var quad in quads)
                {
                    propagateEvent(EventType.Removing, quad);
                }

                this.value = northWestValue;

                propagateEvent(EventType.Added);
            }
            else if (!allWhite)
            {
                bool anySub = false;
                foreach (var quad in quads)
                {
                    anySub |= quad.unsubdivide();
                }

                if (anySub)
                {
                    return unsubdivide();
                }

                return false;
            }

            quads = null;

            return true;
        }

        public void MoveTo(RegionQuadtree<T> other)
        {
            /*
            var foundPosition = false;
            var myPosition = 0;
            for (int i = 0; i < parent.quads.Length; i++)
            {
                if (parent.quads[i] == this)
                {
                    myPosition = i;
                    foundPosition = true;
                    break;
                }
            }

            if (!foundPosition)
            {
                throw new InvalidOperationException("Parent doesn't have this quadtree as child!");
            }
            */

            foreach (var quadtree in (IEnumerable<RegionQuadtree<T>>)this)
            {
                other.SetAABB(quadtree.AABB, quadtree.value.Value);
            }

            Unset();
        }

        public IEnumerator<T> GetEnumerator()
        {
            switch (Type)
            {
                case QuadType.Black:
                    yield return value.Value;
                    break;
                case QuadType.Grey:
                    foreach (var item in quads)
                    {
                        foreach (var subItem in item)
                        {
                            yield return subItem;
                        }
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

        IEnumerable<RegionQuadtree<T>> GetBlackRegions()
        {
            switch (Type)
            {
                case QuadType.Black:
                    yield return this;
                    break;
                case QuadType.Grey:
                    foreach (var item in quads)
                    {
                        foreach (var item2 in item.GetBlackRegions())
                        {
                            yield return item2;
                        }
                    }
                    break;
                case QuadType.White:
                default:
                    break;
            }
        }

        /*
        IEnumerator<RegionQuadtree<T>> IEnumerable<RegionQuadtree<T>>.GetEnumerator()
        {
            switch (Type)
            {
                case QuadType.Black:
                    yield return this;
                    break;
                case QuadType.Grey:
                    foreach (var item in quads)
                    {
                        foreach (var item2 in ((IEnumerable<RegionQuadtree<T>>)item))
                        {
                            yield return item2;
                        }
                    }
                    break;
                case QuadType.White:
                default:
                    break;
            }
        }
         */

        public IEnumerable<RegionQuadtree<T>> Traverse()
        {
            var a = new RegionQuadtree<T>[8];
            return traverseInternal(a);
        }

        private IEnumerable<RegionQuadtree<T>> traverseInternal(RegionQuadtree<T>[] a)
        {
            var sides = QDO.Sides;

            var t = new RegionQuadtree<T>[8];
            if (Type == QuadType.Grey)
            {
                for (int i = 0; i < 4; i++)
                {
                    var d = sides[i];

                    t[(int)d] = soni(a[(int)d], QDO.Quad(QDO.OpSide(d), QDO.CSide(d)));
                    t[(int)QDO.Quad(d, QDO.CSide(d))] = soni(a[(int)QDO.Quad(d, QDO.CSide(d))], QDO.Quad(QDO.OpSide(d), QDO.CCSide(d)));
                    t[(int)QDO.CSide(d)] = soni(a[(int)QDO.CSide(d)], QDO.Quad(d, QDO.CCSide(d)));
                    t[(int)QDO.Quad(QDO.OpSide(d), QDO.CSide(d))] = soni(a[(int)QDO.CSide(d)], QDO.Quad(QDO.OpSide(d), QDO.CCSide(d)));
                    t[(int)QDO.OpSide(d)] = this[QDO.Quad(QDO.OpSide(d), QDO.CSide(d))];
                    t[(int)QDO.Quad(QDO.OpSide(d), QDO.CCSide(d))] = this[QDO.Quad(QDO.OpSide(d), QDO.CCSide(d))];
                    t[(int)QDO.CCSide(d)] = this[QDO.Quad(d, QDO.CCSide(d))];
                    t[(int)QDO.Quad(d, QDO.CCSide(d))] = soni(a[(int)d], QDO.Quad(QDO.OpSide(d), QDO.CCSide(d)));

                    foreach (var item in this[QDO.Quad(d, QDO.CSide(d))].traverseInternal(t))
                    {
                        yield return item;
                    }
                }
            }
            else if (Type == QuadType.Black)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (a[i] != null && a[i].Type == QuadType.White)
                    {
                        a[i] = null;
                    }
                }

                yield return this;
            }
        }

        public bool RemoveQuadtree(RegionQuadtree<T> other)
        {
            return removeQuadtreeInternal(other);
        }

        private bool removeQuadtreeInternal(RegionQuadtree<T> other)
        {
            if (other == this)
            {
                var unset = unsetInternal();

                if (unset)
                {
                    if (parent != null)
                    {
                        parent.unsubdivide();
                    }
                    else
                    {
                        unsubdivide();
                    }
                }

                return true;
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    if (this.quads[i].removeQuadtreeInternal(other))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool isPointOutside(Point2i point)
        {
            return !aabb.Contains(point);
        }

        public RegionQuadtree<T> ExpandFromCenter()
        {
            if (Type != QuadType.Grey)
            {
                subdivide();
            }

            Action<RegionQuadtree<T>, Action<RegionQuadtree<T>>> doForEach = null;
            doForEach = (qt, f) =>
            {
                f(qt);

                if (qt.Type == QuadType.Grey)
                {
                    foreach (var dir in QDO.Quadrants)
                    {
                        doForEach(qt[dir], f);
                    }
                }
            };

            doForEach(this, (qt) =>
            {
                if (qt.Type == QuadType.Black)
                {
                    qt.propagateEvent(EventType.Removing);
                }
            });

            var newRoot = new RegionQuadtree<T>(resolution + 1);
            newRoot.AutoExpand = AutoExpand;
            newRoot.subdivide();
            foreach (var child in newRoot.quads)
            {
                child.subdivide();
            }

            var offset = new Point2i(newRoot.aabb.Width / 4, newRoot.aabb.Height / 4);

            var offsets = new Dictionary<QuadDirection, Point2i>
            {
                { QuadDirection.NorthWest, new Point2i(quads[0].aabb.Width / 2, quads[0].aabb.Height / 2) },
                { QuadDirection.SouthEast, new Point2i(quads[0].aabb.Width / 2, quads[0].aabb.Height / 2) },
                { QuadDirection.NorthEast, new Point2i(quads[0].aabb.Width / 2, quads[0].aabb.Height / 2) },
                { QuadDirection.SouthWest, new Point2i(quads[0].aabb.Width / 2, quads[0].aabb.Height / 2) },
            };

            // Set children

            Action<RegionQuadtree<T>, RegionQuadtree<T>> expandProcessor = null;
            expandProcessor = (qt, parent) =>
            {
                // Do this
                qt.parent = parent;
                qt.depth = qt.parent.depth + 1;
                qt.resolution = qt.parent.resolution;

                // Do children
                if (qt.Type == QuadType.Grey)
                {
                    foreach (var dir in QDO.Quadrants)
                    {
                        switch (dir)
                        {
                            case QuadDirection.NorthWest:
                                qt[dir].aabb.LowerBound = qt.aabb.LowerBound;
                                qt[dir].aabb.UpperBound = qt.aabb.LowerBound + new Point2i(qt.aabb.Width / 2, qt.aabb.Height / 2);
                                break;
                            case QuadDirection.NorthEast:
                                qt[dir].aabb.LowerBound = qt.aabb.LowerBound + new Point2i(qt.aabb.Width / 2, 0);
                                qt[dir].aabb.UpperBound = qt.aabb.LowerBound + new Point2i(qt.aabb.Width, qt.aabb.Height / 2);
                                break;
                            case QuadDirection.SouthEast:
                                qt[dir].aabb.LowerBound = qt.aabb.LowerBound + new Point2i(qt.aabb.Width / 2, qt.aabb.Height / 2);
                                qt[dir].aabb.UpperBound = qt.aabb.LowerBound + new Point2i(qt.aabb.Width, qt.aabb.Height);
                                break;
                            case QuadDirection.SouthWest:
                                qt[dir].aabb.LowerBound = qt.aabb.LowerBound + new Point2i(0, qt.aabb.Height / 2);
                                qt[dir].aabb.UpperBound = qt.aabb.LowerBound + new Point2i(qt.aabb.Width / 2, qt.aabb.Height);
                                break;
                            default:
                                throw new ArgumentException("Invalid direction");
                        }

                        expandProcessor(qt[dir], qt);
                    }
                }
            };

            foreach (var quadDirection in QDO.Quadrants)
            {
                var oldQuad = newRoot[quadDirection][QDO.OpSide(quadDirection)];

                newRoot[quadDirection][QDO.OpSide(quadDirection)] = this[quadDirection];

                var newQuad = newRoot[quadDirection][QDO.OpSide(quadDirection)];

                newQuad.aabb = oldQuad.aabb;
                expandProcessor(newQuad, oldQuad.parent);
            }

            // Call the event
            if (OnExpand != null)
            {
                OnExpand(this, new QuadExpandEventArgs<T>(newRoot, offset));
            }

            // Move events
            // TODO: Test this works properly
            newRoot.OnQuadAdded = OnQuadAdded;
            OnQuadAdded = null;
            newRoot.OnQuadRemoving = OnQuadRemoving;
            OnQuadRemoving = null;
            newRoot.OnQuadChanged = OnQuadChanged;
            OnQuadChanged = null;

            doForEach(newRoot, (qt) =>
            {
                if (qt.Type == QuadType.Black)
                {
                    qt.propagateEvent(EventType.Added);
                }
            });

            newRoot.OnExpand = OnExpand;

            return newRoot;
        }

        public List<List<RegionQuadtree<T>>> FindConnectedComponents(bool quadrants = true)
        {
            var linked = new List<DisjointSet<int>>();
            var labels = new Dictionary<RegionQuadtree<T>, int>();
            cclInternal(new RegionQuadtree<T>[8], linked, labels, QDO.Sides, quadrants);
            cclInternal(new RegionQuadtree<T>[8], linked, labels, QDO.Sides.Reverse().ToArray(), quadrants);

            var convert = new Dictionary<int, int>();
            var regions = new List<List<RegionQuadtree<T>>>();
            foreach (var pair in labels)
            {
                var item = linked[pair.Value].Find().Item;

                if (!convert.ContainsKey(item))
                {
                    convert.Add(item, regions.Count);
                    regions.Add(new List<RegionQuadtree<T>>());
                }

                regions[convert[item]].Add(pair.Key);
            }

            for (int i = 0; i < regions.Count; i++)
            {
                regions[i].Sort(new Comparison<RegionQuadtree<T>>((v1, v2) => v1.depth.CompareTo(v2.depth)));
            }

            return regions;
        }

        private void cclInternal(RegionQuadtree<T>[] a, List<DisjointSet<int>> linked, Dictionary<RegionQuadtree<T>, int> labels, QuadDirection[] sides, bool quadrants)
        {
            var t = new RegionQuadtree<T>[8];
            if (Type == QuadType.Grey)
            {
                for (int i = 0; i < 4; i++)
                {
                    var d = sides[i];

                    t[(int)d] = soni(a[(int)d], QDO.Quad(QDO.OpSide(d), QDO.CSide(d)));
                    t[(int)QDO.CSide(d)] = soni(a[(int)QDO.CSide(d)], QDO.Quad(d, QDO.CCSide(d))); 
                    t[(int)QDO.OpSide(d)] = this[QDO.Quad(QDO.OpSide(d), QDO.CSide(d))]; 
                    t[(int)QDO.CCSide(d)] = this[QDO.Quad(d, QDO.CCSide(d))];

                    if (quadrants)
                    {
                        t[(int)QDO.Quad(d, QDO.CSide(d))] = soni(a[(int)QDO.Quad(d, QDO.CSide(d))], QDO.Quad(QDO.OpSide(d), QDO.CCSide(d)));
                        t[(int)QDO.Quad(QDO.OpSide(d), QDO.CSide(d))] = soni(a[(int)QDO.CSide(d)], QDO.Quad(QDO.OpSide(d), QDO.CCSide(d)));
                        t[(int)QDO.Quad(QDO.OpSide(d), QDO.CCSide(d))] = this[QDO.Quad(QDO.OpSide(d), QDO.CCSide(d))];
                        t[(int)QDO.Quad(d, QDO.CCSide(d))] = soni(a[(int)d], QDO.Quad(QDO.OpSide(d), QDO.CCSide(d)));
                    }

                    this[QDO.Quad(d, QDO.CSide(d))].cclInternal(t, linked, labels, sides, quadrants);
                }
            }
            else if (Type == QuadType.Black)
            {
                bool noNeighbors = true;
                var neighborLabels = new List<int>();
                for (int i = 0; i < 8; i++)
                {
                    if (a[i] != null && a[i].Type == QuadType.White)
                    {
                        a[i] = null;
                    }

                    if (a[i] != null)
                    {
                        if (a[i].Type == QuadType.Black && labels.ContainsKey(a[i]))
                        {
                            neighborLabels.Add(labels[a[i]]);
                            noNeighbors = false;
                        }
                        else if (a[i].Type == QuadType.Grey)
                        {
                            var share = getAllWhoShareEdge((QuadDirection)i, QuadType.Black);
                            foreach (var shareBlack in share)
                            {
                                if (labels.ContainsKey(shareBlack))
                                {
                                    neighborLabels.Add(labels[shareBlack]);
                                    noNeighbors = false;
                                }
                            }
                        }
                    }
                }

                if (noNeighbors)
                {
                    var nextLabel = linked.Count;
                    linked.Add(new DisjointSet<int>(nextLabel));
                    labels[this] = nextLabel;
                }
                else
                {
                    labels[this] = neighborLabels.Min();
                    for (int i = 0; i < neighborLabels.Count; i++)
                    {
                        for (int j = 0; j < neighborLabels.Count; j++)
                        {
                            linked[neighborLabels[i]].Union(linked[neighborLabels[j]]);
                        }
                    }
                }
                
            }
        }

        private List<RegionQuadtree<T>> getAllWhoShareEdge(QuadDirection direction, QuadType type)
        {
            QuadDirection[] dir;
            var op = QDO.OpSide(direction);
            if ((int)direction % 2 == 0)
            {
                dir = new QuadDirection[] { QDO.Quad(op, QDO.CSide(op)), QDO.Quad(op, QDO.CCSide(op)) };
            }
            else
            {
                dir = new QuadDirection[] { QDO.OpSide(op) };
            }

            if (type == QuadType.Grey)
            {
                throw new ArgumentException("Grey isn't allowed!");
            }

            var share = new List<RegionQuadtree<T>>();
            getAllWhoShareEdgeInternal(type, this, share, dir);

            return share;
        }

        private void getAllWhoShareEdgeInternal(QuadType type, RegionQuadtree<T> original, List<RegionQuadtree<T>> share, QuadDirection[] dir)
        {
            if (this.Type == type && this.aabb.Intersects(ref original.aabb))
            {
                share.Add(this);
            }
            else if (this.Type == QuadType.Grey)
            {
                for (int i = 0; i < dir.Length; i++)
                {
                    this[dir[i]].getAllWhoShareEdgeInternal(type, original, share, dir);
                }
            }
        }

        private static RegionQuadtree<T> soni(RegionQuadtree<T> p, QuadDirection q)
        {
            if (p != null && p.Type == QuadType.Grey)
            {
                return p[q];
            }

            return p;

        }
    }
}
