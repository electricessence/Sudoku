using Open.Collections;
using Spectre.Console;
using Spectre.Console.Rendering;
using Sudoku.Core;
using System.Numerics;

namespace Sudoku.Console;
internal static class Utility
{
	public static bool Intersects<T>(HashSet<T> source, ReadOnlyMemory<T> other)
	{
		foreach (var s in other.Span)
		{
			if (!source.Add(s))
				return true;
		}
		return false;
	}

	public static IEnumerable<int[][]> GroupSets(int square)
	{
		var size = square * square;
		var sourceSet = Enumerable.Range(1, size).ToArray();
		var subsets = sourceSet.Subsets(square).ToArray();
		var setChecker = new HashSet<int>(size);
		return subsets
			.SubsetsBuffered(square)
			.Where(group =>
			{
				setChecker.Clear();
				foreach (var subset in group.Span)
				{
					if (Intersects(setChecker, subset))
						return false;
				}

				return true;
			})
			.Select(g => g.ToArray())
			.ToArray();
	}

	public static Grid GroupGrid(this int[][] source)
	{
		var grid = new Grid();

		for (var i = 0; i < source[0].Length; i++)
			grid.AddColumn();

		foreach (var row in source)
			grid.AddRow(row.Select(i => i == 0 ? "" : i.ToString()).ToArray());

		return grid;
	}

	public static Grid GroupGrid<T>(this GridSegment<T> source)
		where T : INumber<T>
	{
		var grid = new Grid();

		for (var i = 0; i < source.ColCount; i++)
			grid.AddColumn();

		for (var y = 0; y < source.RowCount; y++)
		{
			var row = new IRenderable[source.ColCount];
			for (var x = 0; x < source.ColCount; x++)
			{
				var value = source[x, y];
				row[x] = new Text(value == T.Zero ? "" : value.ToString()!);
			}
			grid.AddRow(row);
		}

		return grid;
	}

	public static IEnumerable<T[][]> PossibleRemainingGroups<T>(T[][][] groupSets, IGrid<T> source, int square)
		=> groupSets
		.SelectMany(s => s.Permutations())
		.Where(p =>
		{
			for (var i = 0; i < square; i++)
			{
				var aSub = source.GetRow(i);
				foreach (var e in p[i])
				{
					if (aSub.Contains(e))
						return false;
				}
			}

			return true;
		});

	public static void RenderGrid<T>(this Grid<T> grid, int square)
		where T : INumber<T>
	{
		var fullGrid = new Grid();
		for (var i = 0; i < square; i++)
			fullGrid.AddColumn();

		for (var y = 0; y < square; y++)
		{
			var row = new IRenderable[square];

			for (var x = 0; x < square; x++)
			{
				row[x] = Utility.GroupGrid(grid.GetSubGrid(x * square, y * square, square, square));
			}

			fullGrid.AddRow(row);
		}

		AnsiConsole.Write(fullGrid);
	}

	public static int[][] ScrambleValues(this int[][] group, Random rnd)
	{
		foreach (var row in group)
		{
			Array.Sort(row, (_, __) => rnd.Next());
		}

		return group;
	}
}
