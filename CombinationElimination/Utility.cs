namespace CombinationElimination;
public static class Utility
{
	public static IReadOnlyList<IList<int[]>> CreateConjoinedSet(int size)
		=> Enumerable
		.Range(0, size)
		.Select(_ => new List<int[]>())
		.ToArray();
}
