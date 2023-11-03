using Open.Collections;
using Open.RandomizationExtensions;
using Spectre.Console;
using Spectre.Console.Cli;
using Sudoku.Core;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Sudoku.Console.Commands;
internal class GenerateBoard : Command<GenerateBoard.Settings>
{
	private readonly Random rnd = new();

	//private int[][] GetRandomizedGroup(int[][][] groupSets)
	//	=> groupSets[rnd.Next(groupSets.Length)]
	//	.Select(s => s.Shuffle().ToArray())
	//	.Shuffle()
	//	.ToArray();

	private void GetRandomizedGroup(int[][][] groupSets, GridSegment<int> grid)
	{
		var group = groupSets[rnd.Next(groupSets.Length)];
		foreach (var (row, y) in group.Shuffle().Select((e, i) => (e, i)))
		{
			foreach (var (value, x) in row.Shuffle().Select((e, i) => (e, i)))
			{
				grid[x, y] = value;
			}
		}
	}

	private void ApplyGroup(int[][] group, GridSegment<int> grid)
	{
		foreach (var (row, y) in group.Select((e, i) => (e, i)))
		{
			foreach (var (value, x) in row.Select((e, i) => (e, i)))
			{
				grid[x, y] = value;
			}
		}
	}

	public sealed class Settings
		: CommandSettings
	{
		[Description("The number to square in order to generate a board.")]
		[CommandArgument(0, "[value]")]
		public byte Value { get; init; } = 3;
	}

	public override int Execute(
		[NotNull] CommandContext context,
		[NotNull] Settings settings)
	{
		var square = settings.Value;
		var size = square * square;
		var groupSets = Utility.GroupSets(square).ToArray();
		var board = new Grid<int>(size);

		// Upper left corner.
		var anchorSet = board.GetSubGrid(0, 0, square, square);
		GetRandomizedGroup(groupSets, anchorSet);
		var rightCorner = board.GetSubGrid(square * square - square, 0, square, square);
		var rightCornerValues = Utility.PossibleRemainingGroups(groupSets, anchorSet, square).ToArray().RandomSelectOne(rnd).ScrambleValues(rnd);
		ApplyGroup(rightCornerValues, rightCorner);

		board.RenderGrid(square);

		return 0;
	}
}
