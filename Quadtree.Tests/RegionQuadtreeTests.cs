using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quadtree;
using Xunit;
namespace Quadtree.Tests
{
    public class RegionQuadtreeTests
    {
        [Fact()]
        public void AABBShouldHaveValidSize()
        {
            var qt = new RegionQuadtree<int>(3);

            Assert.Equal(8, qt.AABB.Width);
            Assert.Equal(8, qt.AABB.Height);
        }

        [Fact()]
        public void ResolutionBoundaryShouldSuccess()
        {
            Assert.DoesNotThrow(() => new RegionQuadtree<int>(30));
            Assert.DoesNotThrow(() => new RegionQuadtree<int>(0));
        }

        [Fact()]
        public void ResolutionOutsideBoundaryFail()
        {
            Assert.Throws<ArgumentException>(() => new RegionQuadtree<int>(31));
        }

        [Fact()]
        public void NegativeResolutionShouldFail()
        {
            Assert.Throws<ArgumentException>(() => new RegionQuadtree<int>(-1));
        }

        [Fact()]
        public void SetInitialValueOnConstruct()
        {
            var qt = new RegionQuadtree<int>(3);
            qt.Set(1);
            Assert.Equal(QuadType.Black, qt.Type);
        }

        [Fact()]
        public void QuadTypeShouldBeWhite()
        {
            var qt = new RegionQuadtree<int>(3);
            Assert.Equal(QuadType.White, qt.Type);
        }

        [Fact()]
        public void QuadTypeShouldBeBlack()
        {
            var qt = new RegionQuadtree<int>(0);
            qt.Set(new Point2i(0, 0), 1);

            Assert.Equal(QuadType.Black, qt.Type);
        }

        [Fact()]
        public void QuadTypeShouldBeGrey()
        {
            var qt = new RegionQuadtree<int>(3);
            qt.Set(new Point2i(0, 0), 1);

            Assert.Equal(QuadType.Grey, qt.Type);
        }

        [Fact()]
        public void SetInsideShouldSuccess()
        {
            var qt = new RegionQuadtree<int>(3);
            var success1 = qt.Set(new Point2i(0, 0), 1);
            var success2 = qt.Set(new Point2i(7, 7), 1);

            Assert.True(success1);
            Assert.True(success2);
        }

        [Fact()]
        public void SetOutsideShouldFail()
        {
            var qt = new RegionQuadtree<int>(3);
            var success1 = qt.Set(new Point2i(8, 8), 1);
            var success2 = qt.Set(new Point2i(-1, -1), 1);

            Assert.False(success1);
            Assert.False(success2);
        }

        [Fact()]
        public void WhiteQuadEnumeration()
        {
            var qt = new RegionQuadtree<int>(3);
            qt.Set(new Point2i(0, 0), 1);

            Assert.Equal(1, qt.Count());
            Assert.Equal(1, qt.ElementAt(0));

            qt.Set(new Point2i(4, 4), 2);

            Assert.Equal(2, qt.Count());
            Assert.Equal(1, qt.ElementAt(0));
            Assert.Equal(2, qt.ElementAt(1));
        }

        [Fact()]
        public void BlackQuadEnumeration()
        {
            var qt = new RegionQuadtree<int>(3);
            qt.Set(1);
            Assert.Equal(1, qt.Count());
            Assert.Equal(1, qt.ElementAt(0));

            qt.Set(new Point2i(0, 0), 2);

            Assert.True(qt.Contains(2));
            Assert.Equal(10, qt.Count());
            Assert.Equal(9, qt.Count((i) => i == 1));
        }

        [Fact()]
        public void TraverseTest()
        {
            var qt = new RegionQuadtree<int>(2);
            qt.Set(new Point2i(0, 0), 1);
            qt.Set(new Point2i(1, 0), 2);
            var t = qt.Traverse().ToList();
            Assert.Equal(2, t.Count);

            qt = new RegionQuadtree<int>(3);
            qt.Set(new Point2i(0, 0), 1);
            qt.Set(new Point2i(7, 7), 2);
            qt.Set(new Point2i(1, 0), 1);
            qt.Set(new Point2i(0, 5), 2);
            qt.Set(new Point2i(3, 6), 2);
            t = qt.Traverse().ToList();
            Assert.Equal(5, t.Count);
        }

        [Fact()]
        public void CCLTest()
        {
            var qt = new RegionQuadtree<int>(3);
            qt.Set(new Point2i(0, 0), 1);
            qt.Set(new Point2i(1, 0), 1);
            qt.Set(new Point2i(0, 2), 1);
            qt.Set(new Point2i(1, 2), 1);
            var r = qt.CCL();
            Assert.Equal(2, r.Count);
            Assert.Equal(2, r[0].Count);
            Assert.Equal(2, r[1].Count);

            qt = new RegionQuadtree<int>(3);
            qt.Set(new Point2i(0, 0), 1);
            qt.Set(new Point2i(1, 0), 1);
            qt.Set(new Point2i(2, 0), 1);
            qt.Set(new Point2i(2, 1), 1);
            qt.Set(new Point2i(2, 2), 1);
            qt.Set(new Point2i(2, 3), 1);
            r = qt.CCL();
            Assert.Equal(1, r.Count);
            Assert.Equal(6, r[0].Count);

            qt = new RegionQuadtree<int>(3);
            qt.Set(new Point2i(0, 0), 1);
            qt.Set(new Point2i(2, 0), 1);
            qt.Set(new Point2i(4, 0), 1);
            qt.Set(new Point2i(0, 2), 1);
            qt.Set(new Point2i(2, 2), 1);
            qt.Set(new Point2i(4, 2), 1);
            r = qt.CCL();
            Assert.Equal(6, r.Count);
            for (int i = 0; i < 6; i++)
            {
                Assert.Equal(1, r[i].Count);
            }
        }

        [Fact()]
        public void CCLSmallerNeighbor()
        {
            var qt = new RegionQuadtree<int>(2);
            qt.Set(1);
            qt.Set(new Point2i(0, 0), 2);
            //qt.Set(new Point2i(3, 0), 2);
            //qt.Set(new Point2i(3, 3), 2);
            //qt.Set(new Point2i(1, 2), 1);
            var r = qt.CCL();
            Assert.Equal(1, r.Count);
        }
    }
}
