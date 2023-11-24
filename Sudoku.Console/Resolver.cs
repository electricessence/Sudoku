using CombinationElimination;
using Open.Collections;
using Open.Disposable;
using Sudoku.Core;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace Sudoku.Console;
public class Resolver
{
	public Resolver(int square)
	{
		Square = square;
		Size = square * square;
		SourceSet = Enumerable.Range(1, Square * Square).ToArray();
		Catalog = new SetCatalog(SourceSet.Subsets(Square).Select(Set.Relinquish));

		var groupSets = Utility.GroupSets(Catalog, square).ToArray();
		var groupSetsLookup = new OrderedDictionary<string, ReadOnlyMemory<Set>>(groupSets.Length);
		foreach (var set in groupSets)
		{
			var hash = set.GetToStringHash();
			groupSetsLookup.Add(hash, set);
		}
		GroupSets = groupSetsLookup;
	}

	public int Square { get; }

	public int Size { get; }

	public ReadOnlyMemory<int> SourceSet { get; }

	public SetCatalog Catalog { get; }

	public IDictionary<string, ReadOnlyMemory<Set>> GroupSets { get; }

	public ReadOnlyMemory<Set> LookupSuperset(Set[] set)
	{
		// Sort the cross set so that we can compare them later.
		Array.Sort(set, (a, b) =>
		{
			for (var i = 0; i < Square; i++)
			{
				var c = a[i].CompareTo(b[i]);
				if (c != 0)
					return c;
			}

			Debug.Fail("Should not be possible to reach this point.");
			return 0;
		});

		return GroupSets[set.GetToStringHash()];
	}

	public ReadOnlyMemory<Set>[] GetAdjacentSets(int[][] set)
		=> GroupSets.Values.Where(s => s.PermutationsBuffered().Any(p =>
		{
			for (var i = 0; i < Square; i++)
			{
				var aSub = set[i];
				foreach (var e in p.Span[i])
				{
					if (aSub.Contains(e))
						return false;
				}
			}

			return true;
		})).ToArray();

	public ReadOnlyMemory<Set>[] GetAdjacentSets(ReadOnlyMemory<int>[] set)
		=> GroupSets.Values.Where(s => s.PermutationsBuffered().Any(p =>
		{
			for (var i = 0; i < Square; i++)
			{
				var aSub = set[i].Span;
				foreach (var e in p.Span[i])
				{
					if (aSub.Contains(e))
						return false;
				}
			}

			return true;
		})).ToArray();

	public ReadOnlyMemory<Set>[] GetAdjacentSets(ReadOnlyMemory<Set> set)
		=> GroupSets.Values.Where(s => s.PermutationsBuffered().Any(p =>
		{
			for (var i = 0; i < Square; i++)
			{
				var aSub = set.Span[i];
				foreach (var e in p.Span[i])
				{
					if (aSub.Contains(e))
						return false;
				}
			}

			return true;
		})).ToArray();

	public ReadOnlyMemory<Set>[] GetAdjacentSets(ReadOnlyMemory<Set>[] sets)
		=> sets.SelectMany(GetAdjacentSets).Distinct().ToArray();

	// Find the cross sets from the possible intersecting sets. "Rotate" the sets possible sets and reduce.
	IEnumerable<ReadOnlyMemory<Set>> SelectCrossSet(ReadOnlyMemory<Set> group)
	{
		var groupSpan = group.Span;
		var permutations = new IEnumerable<ReadOnlyMemory<int>>[Square];
		// Setup anchor row.
		permutations[0] = Enumerable.Repeat(groupSpan[0].AsMemory(), 1);
		for (var i = 1; i < Square; ++i)
		{
			// Setup remaining rows.
			permutations[i] = groupSpan[i].PermutationsBuffered();
		}

		var enumerators = permutations.Select(s =>
		{
			var e = s.GetEnumerator();
			e.MoveNext();
			return e;
		}).ToArray();

		// We use the enumerators as a way to track possibilities and reset the enumerator when it reaches the end.
		// If the first enumerator reaches the end, then we have exhausted all possibilities.
		var set = new ReadOnlyMemory<int>[Square];

		do
		{
			for (var c = 0; c < Square; ++c)
				set[c] = enumerators[c].Current;

			yield return LookupSuperset(set.Select(e => Catalog.Get(e.Span)).ToArray());
		}
		while (MoveNext());

		bool MoveNext()
		{
			int last = Square - 1;
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

	public ReadOnlyMemory<Set>[] GetCrossSets(IEnumerable<ReadOnlyMemory<Set>> sets)
		=> sets
		.SelectMany(SelectCrossSet)
		.Distinct()
		.ToArray();

	public Block GetBlock(BigInteger index)
	{
		Span<int> span = stackalloc int[Size];
		for (var i = 0; i < Size; i++)
			span[i] = i + 1;

		return Block.Create(span.Permutation(index));
	}

	public IEnumerable<Block> GetBlocks()
	{
		using var lease = MemoryPool<int>.Shared.Rent(Size);
		var memory = lease.Memory[..Size];
		var span = memory.Span;
		for (var i = 0; i < Size; i++)
			span[i] = i + 1;

		foreach(var m in memory.Permutations())
			yield return Block.Create(m.Span);
	}

	public SortedDictionary<string, ReadOnlyMemory<Block>> GetBlockFamilies()
		=> GetBlocks()
			.AsParallel()
			.GroupBy(b => b.FamilyID)
			.ToSortedDictionary(
				g => g.Key,
				g => (ReadOnlyMemory<Block>)g.OrderBy(b => b.ToString()).ToArray());

	public IEnumerable<Block> GetValidCrossedBlocks(Block horizontal, Block vertical)
	{
		using var lease = HashSetPool<int>.Rent();
		HashSet<int> values = lease.Item;

		Set[] colSets = vertical.Columns().Select(c => new Set(c)).ToArray();
		foreach (var block in GetBlocks())
		{
			bool match = false;

			// Look for rows that overlap with the anchor set.
			for (var y = 0; y < Square; y++)
			{
				values.Clear();
				values.AddRange(horizontal.GetRow(y));
				if (values.Overlaps(block.GetRow(y)))
				{
					match = true;
					break;
				}
			}
			if (match) continue;

			// Look for columns that overlap with the next diagnal.
			for (var x = 0; x < Square; x++)
			{
				values.Clear();
				values.AddRange(colSets[x]);
				if (values.Overlaps(block.GetColumn(x)))
				{
					match = true;
					break;
				}
			}
			if (match) continue;

			yield return block;
		}
	}
}
