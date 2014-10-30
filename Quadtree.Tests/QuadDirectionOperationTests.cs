using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quadtree;
using Xunit;
using QDO = Quadtree.QuadDirectionOperation;

namespace Quadtree.Tests
{
    public class QuadDirectionOperationTests
    {
        [Fact()]
        public void OpSideTest()
        {
            Assert.Equal(QuadDirection.West, QDO.OpSide(QuadDirection.East));
            Assert.Equal(QuadDirection.North, QDO.OpSide(QuadDirection.South));
            Assert.Equal(QuadDirection.East, QDO.OpSide(QuadDirection.West));
            Assert.Equal(QuadDirection.South, QDO.OpSide(QuadDirection.North));
        }

        [Fact()]
        public void CSideTest()
        {
            Assert.Equal(QuadDirection.East, QDO.CSide(QuadDirection.North));
            Assert.Equal(QuadDirection.South, QDO.CSide(QuadDirection.East));
            Assert.Equal(QuadDirection.West, QDO.CSide(QuadDirection.South));
            Assert.Equal(QuadDirection.North, QDO.CSide(QuadDirection.West));
        }

        [Fact()]
        public void CCSideTest()
        {
            Assert.Equal(QuadDirection.West, QDO.CCSide(QuadDirection.North));
            Assert.Equal(QuadDirection.North, QDO.CCSide(QuadDirection.East));
            Assert.Equal(QuadDirection.East, QDO.CCSide(QuadDirection.South));
            Assert.Equal(QuadDirection.South, QDO.CCSide(QuadDirection.West));
        }

        [Fact()]
        public void QuadTest()
        {
            Assert.Equal(QuadDirection.NorthWest, QDO.Quad(QuadDirection.North, QuadDirection.West));
            Assert.Equal(QuadDirection.NorthWest, QDO.Quad(QuadDirection.West, QuadDirection.North));

            Assert.Equal(QuadDirection.NorthEast, QDO.Quad(QuadDirection.North, QuadDirection.East));
            Assert.Equal(QuadDirection.NorthEast, QDO.Quad(QuadDirection.East, QuadDirection.North));

            Assert.Equal(QuadDirection.SouthEast, QDO.Quad(QuadDirection.South, QuadDirection.East));
            Assert.Equal(QuadDirection.SouthEast, QDO.Quad(QuadDirection.East, QuadDirection.South));

            Assert.Equal(QuadDirection.SouthWest, QDO.Quad(QuadDirection.South, QuadDirection.West));
            Assert.Equal(QuadDirection.SouthWest, QDO.Quad(QuadDirection.West, QuadDirection.South));
        }
    }
}
