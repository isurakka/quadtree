using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quadtree
{
    public enum QuadDirection
    {
        West,
        NorthWest,
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
    }

    public static class QuadDirectionOperation
    {
        private static readonly QuadDirection[] opside = new QuadDirection[]
        {
            QuadDirection.East,
            QuadDirection.SouthEast,
            QuadDirection.South,
            QuadDirection.SouthWest,
            QuadDirection.West,
            QuadDirection.NorthWest,
            QuadDirection.North,
            QuadDirection.NorthEast,
        };

        private static readonly QuadDirection[] cside = new QuadDirection[]
        {
            QuadDirection.NorthWest,
            QuadDirection.North,
            QuadDirection.NorthEast,
            QuadDirection.East,
            QuadDirection.SouthEast,
            QuadDirection.South,
            QuadDirection.SouthWest,
            QuadDirection.West,
        };

        private static readonly QuadDirection[] ccside = new QuadDirection[]
        {
            QuadDirection.SouthWest,
            QuadDirection.West,
            QuadDirection.NorthWest,
            QuadDirection.North,
            QuadDirection.NorthEast,
            QuadDirection.East,
            QuadDirection.SouthEast,
            QuadDirection.South,
        };

        public static QuadDirection OpSide(QuadDirection a)
        {
            return opside[(int)a];
        }

        public static QuadDirection CSide(QuadDirection a)
        {
            return cside[(int)a];
        }

        public static QuadDirection CCSide(QuadDirection a)
        {
            return ccside[(int)a];
        }

        public static QuadDirection Quad(QuadDirection a, QuadDirection b)
        {
            // Assume user always gives proper values so no validity checks

            int max;
            int min;

            if (a > b)
            {
                max = (int)a;
                min = (int)b;
            }
            else
            {
                max = (int)b;
                min = (int)a;
            }

            if (min == 1)
            {
                return QuadDirection.West;
            }
            else if (min == 0)
            {
                return QuadDirection.SouthWest;
            }

            return (QuadDirection)(max - 1);
        }
    }
}
