using System.Collections;

namespace CombinationElimination;
public class Superset : IReadOnlyList<List<Set>>
{
	public Superset(int size, int range)
	{
		Count = size;
		Range = range;
		_sets = Enumerable
			.Range(0, size)
			.Select(_ => new List<Set>(range / size))
			.ToArray();
	}

	private readonly IList<List<Set>> _sets;

	public List<Set> this[int index] => _sets[index];

	public int Count { get; }
	public int Range { get; }

	public IEnumerator<List<Set>> GetEnumerator() => _sets.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
