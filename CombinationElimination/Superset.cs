using System.Collections;

namespace CombinationElimination;
public class Superset : IReadOnlyList<IList<int[]>>
{
	public Superset(int size, int range)
	{
		Count = size;
		Range = range;
		_sets = Enumerable
			.Range(0, size)
			.Select(_ => new List<int[]>(range / size))
			.ToArray();
	}

	private readonly IList<IList<int[]>> _sets;

	public IList<int[]> this[int index] => _sets[index];

	public int Count { get; }
	public int Range { get; }

	public IEnumerator<IList<int[]>> GetEnumerator() => _sets.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
