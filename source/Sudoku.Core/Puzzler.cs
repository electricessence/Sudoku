namespace Sudoku.Core;

//https://github.com/igortrofymov/SudokuGenerator

public class Puzzler(int numberOfSwaps)
{
	static readonly Random rnd = new();

	private int[,] _table = new int[9, 9];
	readonly int _numberOfSwaps = numberOfSwaps;

	public IEnumerable<char> MakePuzzle(ReadOnlySpan<char> input)
	{
		FillBoard(input);
		Swap();
		Hide();
		return MakeLine();
	}

	static int RandomGenerator(Random rnd, out int first, out int second, int maxValue = 3)
	{
		first = rnd.Next(0, maxValue);
		second = rnd.Next(0, maxValue);
		while (second == first)
		{
			second = rnd.Next(0, maxValue);
		}

		return rnd.Next(0, maxValue);
	}

	public void Swap()
	{
		for (int i = 0; i <= _numberOfSwaps; i++)
		{
			switch (rnd.Next(1, 5))
			{
				case 1:
					SwitchRowSmall();
					break;

				case 2:
					SwitchRowBig();
					break;

				case 3:
					SwitchRowCol();
					SwitchRowBig();
					SwitchRowCol();
					break;

				case 4:
					SwitchRowCol();
					SwitchRowSmall();
					SwitchRowCol();
					break;

				case 5:
					SwitchRowCol();
					break;
			}
		}
	}

	void SwitchRowCol()
	{
		int[,] newArr = new int[9, 9];
		for (int i = 0; i < 9; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				newArr[i, j] = _table[j, i];
			}
		}
		_table = newArr;
	}

	void SwitchRowSmall()
	{
		int rowBig = RandomGenerator(rnd, out int firstRowToSwap, out int secondRowToSwap);
		var newArr = new int[9];
		for (int i = 0; i < 9; i++)
		{
			var rowBig3 = rowBig * 3;
			var firstRow = rowBig3 + firstRowToSwap;
			var secondRow = rowBig3 + secondRowToSwap;
			newArr[i] = _table[firstRow, i];
			_table[firstRow, i] = _table[secondRow, i];
			_table[secondRow, i] = newArr[i];
		}
	}

	void SwitchRowBig()
	{
		RandomGenerator(rnd, out int firstRowToSwap, out int secondRowToSwap);
		var newArr = new int[9];
		for (int k = 0; k < 3; k++)
		{
			for (int i = 0; i < 9; i++)
			{
				var firstRow = firstRowToSwap * 3 + k;
				var secondRow = secondRowToSwap * 3 + k;
				newArr[i] = _table[firstRow, i];
				_table[firstRow, i] = _table[secondRow, i];
				_table[secondRow, i] = newArr[i];
			}
		}
	}

	public void Hide()
	{
		for (int i = 0; i < 9; i++)
		{
			int hidedInRow = rnd.Next(4, 7);
			List<int> toHide = [];
			for (int h = 0; h <= hidedInRow; h++)
			{
				int rand = rnd.Next(0, 9);
				while (toHide.Contains(rand))
				{
					rand = rnd.Next(0, 9);
				}

				toHide.Add(rand);
			}

			foreach (var item in toHide)
			{
				_table[i, item] = 0;
			}
		}
	}

	public void FillBoard(ReadOnlySpan<char> game)
	{
		if (game.Length != 81)
			throw new Exception($"Input game string should have length = 81, but was {game.Length}!");

		for (int n = 0; n < 81; n++)
		{
			char c = game[n];
			if (c is (< '1' or > '9') and not '.')
				throw new Exception("Input game string should have values 0-9 and . only!");
		}

		for (int n = 0; n < 81; n++)
		{
			char c = game[n];
			int i = n / 9;
			int j = n % 9;
			_table[i, j] = c == '.' ? 0 : c - '0';
		}
	}

	private IEnumerable<char> MakeLine()
	{
		for (int i = 0; i < 9; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				var value = _table[i, j];
				yield return value == 0 ? '.' : value.ToNumChar();
			}
		}
	}
}