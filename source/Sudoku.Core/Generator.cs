namespace Sudoku.Core;

// https://gist.github.com/fabiosoft/b41067106bebf1498399f4eb9826e4de

public class Generator : GeneratorBase
{
	public Generator(Func<int, int, int> nextIntFunction)
		: base(nextIntFunction) { }

	public Generator(Random rnd)
		: base(rnd) { }

	public Generator(int seed)
		: base(seed) { }

	public Generator() { }

	public override IEnumerable<char> Generate()
	{
		var grid = new int[9, 9];

		Init(grid);
		Update(grid, 10);

		for (int x = 0; x < 9; x++)
		{
			for (int y = 0; y < 9; y++)
			{
				yield return grid[x, y].ToNumChar();
			}
		}
	}

	public static IEnumerable<char> GenNew()
		=> new Generator().Generate();

	static void Init(int[,] grid)
	{
		for (int i = 0; i < 9; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				grid[i, j] = (i * 3 + i / 3 + j) % 9 + 1;
			}
		}
	}

	static void ChangeTwoCell(int[,] grid, int findValue1, int findValue2)
	{
		int xParm1, yParm1, xParm2, yParm2;
		xParm1 = yParm1 = xParm2 = yParm2 = 0;
		for (int i = 0; i < 9; i += 3)
		{
			for (int k = 0; k < 9; k += 3)
			{
				for (int j = 0; j < 3; j++)
				{
					for (int z = 0; z < 3; z++)
					{
						if (grid[i + j, k + z] == findValue1)
						{
							xParm1 = i + j;
							yParm1 = k + z;
						}

						if (grid[i + j, k + z] == findValue2)
						{
							xParm2 = i + j;
							yParm2 = k + z;
						}
					}
				}

				grid[xParm1, yParm1] = findValue2;
				grid[xParm2, yParm2] = findValue1;
			}
		}
	}

	void Update(int[,] grid, int shuffleLevel)
	{
		for (int repeat = 0; repeat < shuffleLevel; repeat++)
		{
			ChangeTwoCell(grid, NextInt(1, 10), NextInt(1, 10));
		}
	}
}