namespace Sudoku.Core;

public readonly record struct Pair
{
	public int J { get; }
	public int I { get; }
	public int Kind { get; } //0=in row, 1=in coll, 2=in box

	public Pair(int j, int i, int kind = 0)
	{
		J = j;
		I = i;
		Kind = kind;
	}
}
