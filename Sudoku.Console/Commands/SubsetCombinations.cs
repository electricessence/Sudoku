using CombinationElimination;
using Open.Collections;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

	public override int Execute(
		[NotNull] CommandContext context,
		[NotNull] Settings settings)
	{
		var square = settings.Value;
		var size = square * square;
		var sourceSet = Enumerable.Range(1, size).ToArray();
		var subsets = sourceSet.Subsets(square).ToArray();
		var setChecker = new HashSet<int>(size);

		var catalog = new SetCatalog(sourceSet.Subsets(square).Select(Set.Relinquish));
		AnsiConsole.WriteLine($"Catalog Size: {catalog.Count}");

		var sb = new StringBuilder(9);
		(Set[] set, string hash) SupersetHash(Set[] e)
		{
			sb.Clear();
			foreach (var c in e.SelectMany(e => e))
				sb.Append(c);
			return (set: e, hash: sb.ToString());
		}

		var groupSetLookup = Utility.GroupSets(catalog, square)
			.Select(SupersetHash)
			.ToDictionary(item => item.hash, item => item.set);

		var groupSets = groupSetLookup
			.OrderBy(e=>e.Key)
			.Select(e => e.Value)
			.ToArray();

		AnsiConsole.WriteLine($"Total Group Sets: {groupSets.Length}");

		// This is the designated set that defines what the other set should compare against
		// since real combinations are essentially interchangeable.
		var anchorSet = groupSets[0].Select(e => e.ToArray()).ToArray();
		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine("Anchor Set:");
		AnsiConsole.Write(Utility.GroupGrid(anchorSet));
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

		AnsiConsole.WriteLine($"Total Possible Adjacent Group Sets: {possibleIntersectingSets.Length}");

		// Find the cross sets from the possible intersecting sets. "Rotate" the sets possible sets and reduce.
		IEnumerable<Set[]> SelectCrossSet(Set[] group)
		{
			var permutations = new IEnumerable<ReadOnlyMemory<int>>[square];
			// Setup anchor row.
			permutations[0] = Enumerable.Repeat(group[0].AsMemory(), 1);
			for (var i = 1; i < square; ++i)
			{
				// Setup remaining rows.
				permutations[i] = group[i].PermutationsBuffered();
			}

			var enumerators = permutations.Select(s =>
			{
				var e = s.GetEnumerator();
				e.MoveNext();
				return e;
			}).ToArray();

			// We use the enumerators as a way to track possibilities and reset the enumerator when it reaches the end.
			// If the first enumerator reaches the end, then we have exhausted all possibilities.
			var set = new ReadOnlyMemory<int>[square];

			do
			{
				for (var c = 0; c < square; ++c)
					set[c] = enumerators[c].Current;

				var crossSet = set.Select(e => catalog.Get(e.Span)).ToArray();

				// Sort the cross set so that we can compare them later.
				Array.Sort(crossSet, (a, b) =>
				{
					for (var i = 0; i < square; i++)
					{
						var c = a[i].CompareTo(b[i]);
						if (c != 0)
							return c;
					}

					Debug.Fail("Should not be possible to reach this point.");
					return 0;
				});

				yield return groupSetLookup[SupersetHash(crossSet).hash];
			}
			while (MoveNext());

			bool MoveNext()
			{
				int last = square - 1;
				if (Rotate(last))
					return true;

				for (int c = last - 1; c >= 0; --c)
				{
					if (Rotate(c))
						return true;
				}

				return false;
			}

			bool Rotate(int index)
			{
				var e = enumerators[index];
				if (e.MoveNext())
					return true;
				e.Dispose();
				enumerators[index] = e = permutations[index].GetEnumerator();
				e.MoveNext();
				return false;
			}
		}

		var hashSetCheck = new HashSet<string>();
		var crossSets = possibleIntersectingSets
			.SelectMany(SelectCrossSet)
			.Distinct()
			.ToArray();

		AnsiConsole.WriteLine($"Total Possible Cross Group Sets: {crossSets.Length}");

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
