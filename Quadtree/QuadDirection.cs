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
        internal static readonly QuadDirection[] QuadDirections = Enum.GetValues(typeof(QuadDirection)).Cast<QuadDirection>().ToArray();
        internal static readonly QuadDirection[] Sides = QuadDirections.Where((q) => (int)q % 2 == 0).ToArray();
        internal static readonly QuadDirection[] Quadrants = QuadDirections.Where((q) => (int)q % 2 != 0).ToArray();

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
            QuadDirection.North,
            QuadDirection.North,
            QuadDirection.East,
            QuadDirection.East,
            QuadDirection.South,
            QuadDirection.South,
            QuadDirection.West,
            QuadDirection.West,
        };

        private static readonly QuadDirection[] ccside = new QuadDirection[]
        {
            QuadDirection.South,
            QuadDirection.South,
            QuadDirection.West,
            QuadDirection.West,
            QuadDirection.North,
            QuadDirection.North,
            QuadDirection.East,
            QuadDirection.East,
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
            QuadDirection max;
            QuadDirection min;

            if (a > b)
            {
                max = a;
                min = b;
            }
            else
            {
                max = b;
                min = a;
            }

            switch (min)
            {
                case QuadDirection.West:
                    if (max == QuadDirection.North)
                    {
                        return QuadDirection.NorthWest;
                    }
                    else
                    {
                        return QuadDirection.SouthWest;
                    }
                case QuadDirection.North:
                    return QuadDirection.NorthEast;
                case QuadDirection.East:
                    return QuadDirection.SouthEast;
                default:
                    throw new ArgumentException();
            }
        }
    }
}
