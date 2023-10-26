using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sudoku;
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
}
