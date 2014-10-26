using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Quadtree.Tests
{
    public class QuadEventArgsTests
    {
        [Fact()]
        public void WhiteQuadAddEventShouldBeCalledOnce()
        {
            var qt = new RegionQuadtree<int>(3);
            var times = 0;
            qt.OnQuadAdded += (s, a) =>
            {
                times++;
            };

            qt.Set(new Point2i(0, 0), 1);

            Assert.Equal(1, times);


            // Don't call event if value is same
            qt.Set(new Point2i(0, 0), 1);

            Assert.Equal(1, times);
        }

        [Fact()]
        public void WhiteQuadAddEventShouldBeCalledTwice()
        {
            var qt = new RegionQuadtree<int>(3);
            var times = 0;
            qt.OnQuadAdded += (s, a) =>
            {
                times++;
            };

            qt.Set(new Point2i(0, 0), 1);

            Assert.Equal(1, times);

            qt.Set(new Point2i(1, 0), 1);

            Assert.Equal(2, times);
        }

        [Fact()]
        public void WhiteQuadRemoveEventNotShouldBeCalled()
        {
            var qt = new RegionQuadtree<int>(3);
            var times = 0;
            qt.OnQuadRemoving += (s, a) =>
            {
                times++;
            };

            qt.Set(new Point2i(0, 0), 1);

            Assert.Equal(0, times);

            qt.Set(new Point2i(1, 0), 1);

            Assert.Equal(0, times);
        }

        [Fact()]
        public void WhiteQuadRemoveEventShouldBeCalledOnce()
        {
            var qt = new RegionQuadtree<int>(3);
            var times = 0;
            qt.OnQuadRemoving += (s, a) =>
            {
                times++;
            };

            qt.Set(new Point2i(0, 0), 1);
            qt.Unset(new Point2i(0, 0));

            // Remove event should not be called twice
            qt.Unset(new Point2i(0, 0));

            Assert.Equal(1, times);
        }

        [Fact()]
        public void WhiteQuadRemoveEventShouldBeCalledTwice()
        {
            var qt = new RegionQuadtree<int>(3);
            var times = 0;
            qt.OnQuadRemoving += (s, a) =>
            {
                times++;
            };

            qt.Set(new Point2i(0, 0), 1);
            qt.Set(new Point2i(0, 1), 1);

            qt.Unset(new Point2i(0, 0));

            Assert.Equal(1, times);

            qt.Unset(new Point2i(0, 1));

            Assert.Equal(2, times);
        }

        [Fact()]
        public void BlackQuadRemoveEventShouldBeCalledFourTimes()
        {
            var qt = new RegionQuadtree<int>(3);
            qt.Set(1);
            var times = 0;
            qt.OnQuadRemoving += (s, a) =>
            {
                times++;
            };

            qt.Unset(new Point2i(0, 0));

            // Remove event should not be called twice
            qt.Unset(new Point2i(0, 0));

            Assert.Equal(4, times);
        }

        [Fact()]
        public void BlackQuadRemoveEventShouldBeCalledSevenTimes()
        {
            var qt = new RegionQuadtree<int>(3);
            qt.Set(1);
            var times = 0;
            qt.OnQuadRemoving += (s, a) =>
            {
                times++;
            };

            qt.Unset(new Point2i(0, 0));
            qt.Unset(new Point2i(4, 4));

            Assert.Equal(7, times);
        }

        [Fact()]
        public void BlackQuadRemoveEventShouldBeCalledFiveTimes()
        {
            var qt = new RegionQuadtree<int>(3);
            qt.Set(1);
            var times = 0;
            qt.OnQuadRemoving += (s, a) =>
            {
                times++;
            };

            qt.Unset(new Point2i(0, 0));
            qt.Unset(new Point2i(0, 1));

            Assert.Equal(5, times);
        }

        [Fact()]
        public void BlackQuadAddEventShouldBeCalledTwelveTimes()
        {
            var qt = new RegionQuadtree<int>(3);
            qt.Set(1);
            var times = 0;
            qt.OnQuadAdded += (s, a) =>
            {
                times++;
            };

            qt.Unset(new Point2i(0, 0));

            Assert.Equal(12, times);

            // Remove event should not be called twice
            qt.Unset(new Point2i(0, 0));

            Assert.Equal(12, times);
        }

        [Fact()]
        public void BlackQuadAddEventShouldBeCalledTwentyTimes()
        {
            var qt = new RegionQuadtree<int>(3);
            qt.Set(1);
            var times = 0;
            qt.OnQuadAdded += (s, a) =>
            {
                times++;
            };

            qt.Unset(new Point2i(0, 0));
            qt.Unset(new Point2i(4, 4));

            Assert.Equal(20, times);
        }
    }
}