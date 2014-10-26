using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quadtree
{
    public enum QuadType
    {
        White,  // Also known as empty quad with no children
        Black,  // Also known as full quad with no children
        Grey,   // Also known as quad which have black and white children
    }
}
