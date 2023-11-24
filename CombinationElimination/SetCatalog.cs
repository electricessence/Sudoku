using System.Collections;

namespace CombinationElimination;

public class SetCatalog : IReadOnlyList<Set>
{
	private readonly Set[] _sets;
	private readonly ISet<Set> _setsLookup;

	public SetCatalog(IEnumerable<Set> sets, bool ignoreDuplicates = false)
		: this(sets.ToArray(), ignoreDuplicates) { }

	private SetCatalog(Set[] sets, bool ignoreDuplicates = false)
	{
		Array.Sort(sets);
		_sets = sets;

		if (ignoreDuplicates)
		{
			_setsLookup = new HashSet<Set>(sets);
			return;
		}

		_setsLookup = new HashSet<Set>(_sets.Length);
		foreach (var set in _sets)
		{
			if (!_setsLookup.Add(set))
				throw new ArgumentException("Duplicate sets are not allowed.");
		}
	}

	private SetCatalog(ISet<Set> sets)
	{
		_setsLookup = sets;
		_sets = [.. sets.Order()];
	}

	public static SetCatalog Relinquish(ISet<Set> sets)
		=> new(sets);

	public static SetCatalog Relinquish(Set[] sets)
	=> new(sets);

	public SetCatalog[] InitSuperset(int length)
		=> Enumerable.Repeat(this, length).ToArray();

	public int Count
		=> _sets.Length;

	public bool Contains(Set set)
		=> _setsLookup.Contains(set);

	public Set this[int index]
		=> _sets[index];

	public Set? Find(Set set)
	{
		var span = _sets.AsSpan();
		int index = span.BinarySearch(set);
		return index < 0 ? null : span[index];
	}

	public Set Get(Set set)
	{
		ArgumentNullException.ThrowIfNull(set);
		return Find(set) ?? throw new ArgumentException($"Set not found: {set}");
	}

	public ReadOnlySpan<Set> AsSpan()
		=> _sets.AsSpan();

	IEnumerator IEnumerable.GetEnumerator()
		=> _sets.GetEnumerator();

	public IEnumerator<Set> GetEnumerator()
		=> ((IEnumerable<Set>)_sets).GetEnumerator();

	/// <summary>
	/// Creates a new <see cref="SetCatalog"/> that does not contain the provide <paramref name="set"/>.
	/// </summary>
	public SetCatalog Without(IEnumerable<int> set)
		=> new(_sets.Where(s => !s.Overlaps(set)));
}