namespace Sudoku
{
    public enum What { number, user_guess, user_cand };

    public class UndoData
    {
        public int x = -1;
        public int y = -1;
        public int val = 0;
        public bool add = true;
        public What what = What.number;

        public UndoData(int y, int x, int val, What what = What.number, bool add = false)
        {
            this.y = y;
            this.x = x;
            this.val = val;
            this.what = what;
            this.add = add;
        }

        public UndoData(string str)
        {
            y = int.Parse(str[0].ToString());
            x = int.Parse(str[1].ToString());
            val = int.Parse(str[2].ToString());

            // note: if number is wrong, enum will be this number!
            what = (What)int.Parse(str[3].ToString());

            add = int.Parse(str[4].ToString()) == 0 ? false : true;
        }

        public override string ToString()
        {
            var addInt = add ? 1 : 0;
            return $"{y}{x}{val}{(int)what}{addInt}";
        }
    }
}
