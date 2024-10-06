namespace Sudoku.Core;

public enum What { number, user_guess, user_cand };

public class UndoData
{
	public int x = -1;
	public int y = -1;
	public int val;
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
		y = str[0] - '0';
		x = str[1] - '0';
		val = str[2] - '0';

		// note: if number is wrong, enum will be this number!
		what = (What)(str[3] - '0');

		add = str[4] - '0' != 0;
	}

	public override string ToString()
	{
		var addInt = add ? 1 : 0;
		return $"{y}{x}{val}{(int)what}{addInt}";
	}
}
