using CombinationElimination;
using Open.Collections;
using Spectre.Console;
using Spectre.Console.Rendering;
using Sudoku.Core;
using System.Numerics;

namespace Sudoku.Console;
internal static class Utility
{
	public static bool Intersects<T>(HashSet<T> source, ReadOnlySpan<T> other)
	{
		foreach (var s in other)
		{
			if (!source.Add(s))
				return true;
		}
		return false;
	}

	public static IEnumerable<Set[]> GroupSets(SetCatalog catalog, int square)
	{
		var size = square * square;
		var setChecker = new HashSet<int>(size);
		return catalog
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
			.Select(g => g.ToArray());
	}

	public static Grid GroupGrid(ReadOnlySpan<Set> source)
	{
		var grid = new Grid();

		for (var i = 0; i < source[0].Count; i++)
			grid.AddColumn();

		foreach (var row in source)
			grid.AddRow(row.Select(i => i == 0 ? "" : i.ToString()).ToArray());

		return grid;
	}

	public static Grid Grid(int columns, IEnumerable<IEnumerable<IRenderable>> source, int colPadding = 3, int rowPadding = 1)
	{
		var grid = new Grid();

		for (var i = 0; i < columns; i++)
		{
			grid.AddColumn(new GridColumn()
			{
				Padding = new Padding(0, 0, colPadding, 0),
			});
		}

		foreach (var row in source)
		{
			var a = row is IRenderable[] r ? r : row.ToArray();
			grid.AddRow(a);
			for (var i = 0; i < rowPadding; i++)
				grid.AddEmptyRow();
		}

		return grid;
	}

	public static Grid GroupGrid(ReadOnlySpan<int[]> source)
	{
		var grid = new Grid();

		for (var i = 0; i < source[0].Length; i++)
			grid.AddColumn();

		foreach (var row in source)
			grid.AddRow(row.Select(i => i == 0 ? "" : i.ToString()).ToArray());

		return grid;
	}

	public static Grid GroupGrid<T>(this IGrid<T> source)
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

	public static IEnumerable<ReadOnlyMemory<T[]>> PossibleRemainingGroups<T>(T[][][] groupSets, IGrid<T> source, int square)
		=> groupSets
		.SelectMany(s => s.Permutations())
		.Where(p =>
		{
			var span = p.Span;
			for (var i = 0; i < square; i++)
			{
				var aSub = source.GetRow(i);
				foreach (var e in span[i])
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
				row[x] = GroupGrid(grid.GetSubGrid(x * square, y * square, square, square));
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
