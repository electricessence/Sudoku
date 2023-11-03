using System.Collections;

namespace CombinationElimination;

public class SetCatalog : IReadOnlyList<Set>
{
	private readonly Set[] _sets;
	private readonly HashSet<Set> _setsLookup;

	public SetCatalog(IEnumerable<Set> sets)
	{
		_sets = sets.ToArray();
		_setsLookup = new HashSet<Set>(_sets.Length);
		foreach (var set in _sets)
		{
			if (_setsLookup.Add(set))
				throw new ArgumentException("Duplicate sets are not allowed.");
		}
	}

	public int Count
		=> _sets.Length;

	public bool Contains(Set set)
		=> _setsLookup.Contains(set);

	public Set this[int index]
		=> _sets[index];

	public ReadOnlySpan<Set> AsSpan()
		=> _sets.AsSpan();

	public IEnumerator<Set> GetEnumerator()
	{
		int len = _sets.Length;
		for (var i = 0; i < len; i++)
			yield return _sets[i];
	}

	IEnumerator IEnumerable.GetEnumerator()
		=> _sets.GetEnumerator();
}