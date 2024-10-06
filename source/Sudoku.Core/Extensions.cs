using System.Runtime.CompilerServices;

namespace Sudoku.Core;
public static class Extensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static char ToNumChar(this int value)
		=> (char)(value + '0');

	public static StringBuilder AppendChars(this StringBuilder sb, IEnumerable<char> line)
	{
		foreach (var c in line)
			sb.Append(c);

		return sb;
	}

	public static void Add(this IList<char> list, int value)
		=> list.Add(value.ToNumChar());

	public static string ToStringFromChars(this IEnumerable<char> chars)
	{
		var sb = new StringBuilder();
		foreach (var c in chars)
			sb.Append(c);
		return sb.ToString();
	}

	public static StringBuilder AppendRepresentation<T>(this StringBuilder sb, IEnumerable<T> values)
		where T : struct, IFormattable
	{
		ArgumentNullException.ThrowIfNull(sb);
		ArgumentNullException.ThrowIfNull(values);

		sb.Append('[');
		foreach (var value in values)
		{
			sb.Append(value.ToString(null, null));
			sb.Append(',');
		}
		sb.Remove(sb.Length - 1, 1);
		sb.Append(']');
		return sb;
	}
}
