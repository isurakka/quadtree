using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quadtree
{
    public class DisjointSet<T>
    {
        public T Item { get; set; }

        private DisjointSet<T> root;
        private int rank = 0;

        public DisjointSet(T item)
            : this()
        {
            Item = item;
        }

        public DisjointSet()
        {
            root = this;
        }

        public DisjointSet<T> Find()
        {
            if (this.root != this)
                this.root = this.root.Find();

            return this.root;
        }

        public void Union(DisjointSet<T> other)
        {
            var thisRoot = Find();
            var otherRoot = other.Find();

            if (thisRoot == otherRoot)
                return;

            if (thisRoot.rank < otherRoot.rank)
                thisRoot.root = otherRoot;
            else if (thisRoot.rank > otherRoot.rank)
                otherRoot.root = thisRoot;
            else
            {
                otherRoot.root = thisRoot;
                thisRoot.rank++;
            }
        }

        public override string ToString()
        {
            return Item.ToString();
        }
    }
}
