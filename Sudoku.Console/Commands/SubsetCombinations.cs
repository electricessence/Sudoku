using Open.Collections;
using Spectre.Console;
using Spectre.Console.Cli;
using Sudoku.Core;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sudoku.Console.Commands;
public class SubsetCombinations : Command<SubsetCombinations.Settings>
{
	public sealed class Settings
		: CommandSettings
	{
		[Description("The value to use to idenitify a square set.")]
		[CommandArgument(0, "[value]")]
		public byte Value { get; init; } = 3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static IEnumerable<HashSet<T>> SubsetsToHashsets<T>(IReadOnlyList<T> source, int size)
	{
		foreach (var subset in source.SubsetsBuffered(size))
		{
			var hashset = new HashSet<T>(size);
			{
				var span = subset.Span;
				for (var i = 0; i < size; ++i)
					hashset.Add(span[i]);
			}
			yield return hashset;
		}
	}

	static bool Intersects<T>(HashSet<T> source, ReadOnlyMemory<T> other)
	{
		foreach (var s in other.Span)
		{
			if (!source.Add(s))
				return true;
		}
		return false;
	}

	public override int Execute(
		[NotNull] CommandContext context,
		[NotNull] Settings settings)
	{
		var square = settings.Value;
		var size = square * square;
		var sourceSet = Enumerable.Range(1, size).ToArray();
		var subsets = sourceSet.Subsets(square).ToArray();
		var setChecker = new HashSet<int>(size);
		var groupSets = subsets.SubsetsBuffered(square).Where(group =>
		{
			setChecker.Clear();
			foreach (var subset in group.Span)
			{
				if (Intersects(setChecker, subset))
					return false;
			}

			return true;
		}).Select(g => g.ToArray()).ToArray();

		var totalGroupSets = groupSets.Length;
		AnsiConsole.WriteLine($"Total Group Sets: {totalGroupSets}");

		// This is the designated set that defines what the other set should compare against
		// since real combinations are essentially interchangeable.
		var anchorSet = groupSets[0];
		var grid = new Grid();
		for (var i = 0; i < square; i++)
			grid.AddColumn();
		foreach (var row in anchorSet)
			grid.AddRow(row.Select(e => e.ToString()).ToArray());
		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine("Anchor Set:");
		AnsiConsole.Write(grid);
		AnsiConsole.WriteLine();
		 
		// Given the anchor set, we can narrow down any remaining sets that could exist along side it.
		var possibleIntersectingSets = groupSets.Where(s => s.PermutationsBuffered().Any(p =>
		{

			for (var i = 0; i < square; i++)
			{
				var aSub = anchorSet[i];
				foreach (var e in p.Span[i])
				{
					if (aSub.Contains(e))
						return false;
				}
			}

			return true;
		})).ToArray();
		AnsiConsole.WriteLine($"Total Possible Intersecting Sets: {possibleIntersectingSets.Length}");

		// Find the cross sets from the possible intersecting sets. "Rotate" the sets possible sets and reduce.
		IEnumerable<int[][]> SelectCrossSet(int[][] pis)
		{
			foreach (var perm in pis.Select(c => c.Permutations().ToArray()))
			{
				// Iterate all the possible permutations to produce each cross set.
				var crossSet = new int[square][];
				for (var y = 0; y < square; y++)
				{
					var row = new int[square];
					crossSet[y] = row;
					for (var x = 0; x < square; x++)
					{
						row[x] = perm[x][y];
					}
					Array.Sort(row);
				}

				Array.Sort(crossSet, (a, b) =>
				{
					for (var i = 0; i < square; i++)
					{
						var c = a[i].CompareTo(b[i]);
						if (c != 0)
							return c;
					}

					Debug.Assert(false, "Should not be possible to reach this point.");
					return 0;
				});

				yield return crossSet;
			}
		}

		var sb = new StringBuilder(9);
		var hashSetCheck = new HashSet<string>();
		var crossConfigurations = possibleIntersectingSets
			.SelectMany(SelectCrossSet)
			.Select(e =>
			{
				sb.Clear();
				foreach (var c in e.SelectMany(e => e))
					sb.Append(c);
				return (set: e, hash: sb.ToString());
			})
			.Where(e => hashSetCheck.Add(e.hash))
			.Select(e => e.set)
			.ToArray();
		AnsiConsole.WriteLine($"Total Possible Cross Configurations: {crossConfigurations.Length}");
		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine("Total Possilbe Board Variations:");
		var squareMinusOne = square - 1;
		var estimatedTotalBoardVariations = 1;
		for (var j = 0; j < 2 * squareMinusOne; j++)
		{
			AnsiConsole.Write(possibleIntersectingSets.Length);
			AnsiConsole.Write(" × ");
			estimatedTotalBoardVariations *= possibleIntersectingSets.Length;
		}
		for (var j = 0; j < squareMinusOne * squareMinusOne; j++)
		{
			AnsiConsole.Write(crossConfigurations.Length);
			AnsiConsole.Write(" × ");
			estimatedTotalBoardVariations *= crossConfigurations.Length;
		}
		AnsiConsole.WriteLine(1);
		AnsiConsole.Write(" = ");
		AnsiConsole.WriteLine(estimatedTotalBoardVariations);

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
