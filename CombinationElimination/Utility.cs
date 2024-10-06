using Open.Disposable;
using Open.Text;

namespace CombinationElimination;
public static class Utility
{
	public static IReadOnlyList<IList<int[]>> CreateConjoinedSet(int size)
		=> Enumerable
		.Range(0, size)
		.Select(_ => new List<int[]>())
		.ToArray();

	public static string GetToStringHash<T>(this IEnumerable<T> set)
	{
		using var lease = StringBuilderPool.Shared.Rent();
		var sb = lease.Item;
		foreach (var c in set)
			sb.Append(c).Append(' ');
		return sb.TrimEnd().ToString();
	}

	public static int[][] ToClockwiseRotated(this ReadOnlySpan<Set> group)
	{
		int size = group.Length;
		int[][] rotated = new int[size][];

		for (int i = 0; i < size; i++)
		{
			rotated[i] = new int[size];
			for (int j = 0; j < size; j++)
			{
				var g = group[size - j - 1];
				rotated[i][j] = g[i];
			}
		}

		return rotated;
	}

	public static int[][] ToClockwiseRotated(this Span<Set> group)
		=> ToClockwiseRotated((ReadOnlySpan<Set>)group);
}
