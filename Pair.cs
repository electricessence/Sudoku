using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sudoku
{
    public class Pair
    {
        public int kind; //0=in row, 1=in coll, 2=in box
        public int j;
        public int i;

        public Pair(int j, int i, int kind = 0)
        {
            this.j = j;
            this.i = i;
            this.kind = kind;
        }

        public override string ToString()
        {
            return $"{kind}:{j},{i}";
        }

        public override bool Equals(object obj)
        {
            var other = obj as Pair;
            return other.kind == this.kind
                && other.j == this.j
                && other.i == this.i;
        }

        public override int GetHashCode()
        {
            return 1000 * (kind + 1) + 100 * j + 10 * i;
        }
    }
}
