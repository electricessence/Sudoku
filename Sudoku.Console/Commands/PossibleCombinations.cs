using Open.Collections;
using Spectre.Console;
using Spectre.Console.Cli;
using Sudoku.Core;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Sudoku.Console.Commands;
internal class PossibleCombinations : Command<PossibleCombinations.Settings>
{
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
		var values = Enumerable.Range(1, square * square).ToArray();
		foreach (var firstRow in values.PermutationsBuffered())
		{
			var span = firstRow.Span;
			var grid = new SquaredGrid<int>(square);
			for (int x = 0; x < grid.Size; x++)
				grid[x, 0] = span[x];

			var remainingSquare0 = firstRow[square..];
			foreach (var remPerm in remainingSquare0.PermutationsBuffered())
			{
				var remGrid = grid.GetSubSquare(0, 0);
				var remPermSpan = remPerm.Span;

				var i = 0;
				for (var y = 1; y < square; y++)
				{
					for (var x = 0; x < square; x++)
						remGrid[x, y] = remPermSpan[i++];
				}

				var table = new Table()
					//.HideHeaders()
					.HideFooters()
					.ShowRowSeparators();

				for (i = 0; i < grid.ColCount; i++)
					table.AddColumn(span[i].ToString());
				foreach (var row in grid.Rows().Skip(1))
				{
					table.AddRow(row.Select(i => i == 0 ? " " : i.ToString()).ToArray());
				}

				AnsiConsole.Write(table);
			}
		}

		return 0;
	}
}
