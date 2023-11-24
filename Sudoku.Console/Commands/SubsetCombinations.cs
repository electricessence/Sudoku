using CombinationElimination;
using Open.Collections;
using Open.Disposable;
using Spectre.Console;
using Spectre.Console.Cli;
using Sudoku.Core;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Sudoku.Console.Commands;
public class SubsetCombinations : AsyncCommand<SubsetCombinations.Settings>
{
	public sealed class Settings
		: CommandSettings
	{
		[Description("The value to use to idenitify a square set.")]
		[CommandArgument(0, "[value]")]
		public byte Value { get; init; } = 3;
	}

	public override async Task<int> ExecuteAsync(
		[NotNull] CommandContext context,
		[NotNull] Settings settings)
	{
		var resolver = new Resolver(settings.Value);
		var blockFamilies = resolver.GetBlockFamilies();
		AnsiConsole.WriteLine($"{resolver.Square}x{resolver.Square} familes: {blockFamilies.Count}");
		AnsiConsole.WriteLine($"Square Set Catalog Size: {resolver.Catalog.Count}");

		Debug.Assert(resolver.GroupSets.Count == resolver.GetCrossSets(resolver.GroupSets.Values).Length);
		AnsiConsole.WriteLine($"Total Group Sets: {resolver.GroupSets.Count}");

		// This is the designated set that defines what the other set should compare against
		// since real combinations are essentially interchangeable.
		var anchorSet = resolver.GroupSets.Values.First();
		var anchorSetR = anchorSet.Span.ToClockwiseRotated();
		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine("Anchor Block:");
		var anchorGrid = Utility.GroupGrid(anchorSet.Span);
		AnsiConsole.Write(anchorGrid);
		/***********
		 * 1  2  3 *
		 * 4  5  6 *
		 * 7  8  9 *
		 ***********/

		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine("Anchor Block Rotated:");
		AnsiConsole.Write(Utility.GroupGrid(anchorSetR));
		AnsiConsole.WriteLine();
		/***********
		 * 7  4  1 *
		 * 8  5  2 *
		 * 9  6  3 *
		 ***********/

		// Given the anchor set, we can narrow down any remaining sets that could exist along side it.
		var possibleAdjacentSets = resolver.GetAdjacentSets(anchorSet);
		var possibleAdjacentSetsR = resolver.GetAdjacentSets(anchorSetR);
		Debug.Assert(possibleAdjacentSets.Length == possibleAdjacentSetsR.Length);
		AnsiConsole.WriteLine($"Total Possible Adjacent Group Sets: {possibleAdjacentSets.Length}");
		/*********************
		 * 1  2  3 * -  -  - *
		 * 4  5  6 * -  -  - *
		 * 7  8  9 * -  -  - *
		 *********************
		 * |  |  | *
		 * |  |  | *
		 * |  |  | *
		 ***********/

		var crossSets = resolver.GetCrossSets(possibleAdjacentSets);
		var crossSetsR = resolver.GetCrossSets(possibleAdjacentSetsR);
		Debug.Assert(crossSets.Length == crossSetsR.Length);
		AnsiConsole.WriteLine($"Total Possible Cross Group Sets: {crossSets.Length}");
		/*********************
		 * 1  2  3 * -  -  - *
		 * 4  5  6 * -  -  - *
		 * 7  8  9 * -  -  - *
		 *********************
		 * |  |  | * X  X  X *
		 * |  |  | * X  X  X *
		 * |  |  | * X  X  X *
		 *********************/

		var crossSetsAdjacent = resolver.GetAdjacentSets(crossSets);
		var crossSetsAdjacentR = resolver.GetAdjacentSets(crossSetsR);
		Debug.Assert(crossSetsAdjacent.Length == crossSetsAdjacentR.Length);
		AnsiConsole.WriteLine($"Total Possible Cross Group Adjacent Sets: {crossSetsAdjacent.Length}");

		var nextDiagnal = blockFamilies.First().Value.Span[0];
		var nextDiagnalGrid = nextDiagnal.GroupGrid();
		var anchorBlock = Block.Create(anchorSet.Span);
		// Return the results of each set that crosses the diagnal in parallel.

		var x1y0T = Task.Run(() => resolver.GetValidCrossedBlocks(anchorBlock, nextDiagnal).First());
		var x0y1 = await Task.Run(() => resolver.GetValidCrossedBlocks(nextDiagnal, anchorBlock).First());
		var x1y0 = await x1y0T;
		var x0y1Grid = x0y1.GroupGrid();
		var x1y0Grid = x1y0.GroupGrid();
		//AnsiConsole.Write(Utility.Grid(2, [
		//	[anchorGrid, x1y0Grid],
		//	[x0y1Grid, nextDiagnalGrid],
		//]));

		var localSet = new List<int>(resolver.Size);
		var x2y0set = new Set[resolver.Square];
		{
			for (var i = 0; i < resolver.Square; i++)
			{
				localSet.Clear();
				localSet.AddRange(anchorBlock.GetRow(i));
				localSet.AddRange(x1y0.GetRow(i));

				x2y0set[i] = new Set(Enumerable.Range(1, resolver.Size).Except(localSet));
			}
		}
		var x2y0 = Block.Create((ReadOnlySpan<Set>)x2y0set.AsSpan());
		var x2y0Grid = x2y0.GroupGrid();

		var x0y2setR = new Set[resolver.Square];
		{
			for (var i = 0; i < resolver.Square; i++)
			{
				localSet.Clear();
				localSet.AddRange(anchorBlock.GetColumn(i));
				localSet.AddRange(x0y1.GetColumn(i));

				x0y2setR[i] = new Set(Enumerable.Range(1, resolver.Size).Except(localSet));
			}
		}

		x0y2setR.AsSpan().Reverse();
		var x0y2set = x0y2setR.AsSpan().ToClockwiseRotated();
		var x0y2 = Block.Create((ReadOnlySpan<int[]>)x0y2set.AsSpan());
		var x0y2Grid = x0y2.GroupGrid();

		AnsiConsole.Write(Utility.Grid(3, [
			[anchorGrid, x1y0Grid, x2y0Grid],
			[x0y1Grid, nextDiagnalGrid],
			[x0y2Grid],
		]));

		// Validate all posible group configurations.

		//AnsiConsole.WriteLine();
		//AnsiConsole.WriteLine("Total Possilbe Board Variations:");
		//var squareMinusOne = square - 1;
		//var estimatedTotalBoardVariations = 1;
		//for (var j = 0; j < 2 * squareMinusOne; j++)
		//{
		//	AnsiConsole.Write(possibleIntersectingSets.Length);
		//	AnsiConsole.Write(" × ");
		//	estimatedTotalBoardVariations *= possibleIntersectingSets.Length;
		//}
		//for (var j = 0; j < squareMinusOne * squareMinusOne; j++)
		//{
		//	AnsiConsole.Write(crossConfigurations.Length);
		//	AnsiConsole.Write(" × ");
		//	estimatedTotalBoardVariations *= crossConfigurations.Length;
		//}
		//AnsiConsole.WriteLine(1);
		//AnsiConsole.Write(" = ");
		//AnsiConsole.WriteLine(estimatedTotalBoardVariations);

		//var possibleCrossConfigurations = groupSets.Where(s => s.PermutationsBuffered().Any(p =>
		//{
		//	foreach (var iSet in possibleIntersectingSets)
		//	{
		//		for (var i = 0; i < square; i++)
		//		{
		//			var pSub = p.Span[i];
		//			for (var x = 0; x < square; ++x)
		//			{
		//				var v = iSet[x][i];
		//				if (pSub.Contains(v))
		//					return false;
		//			}
		//		}
		//	}

		//	return true;
		//})).ToArray();
		//AnsiConsole.WriteLine($"Total Possible Opposing Sets: {possibleCrossConfigurations.Length}");

		return 0;
	}
}
