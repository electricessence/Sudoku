using System.Collections;
using System.Text;

namespace CombinationElimination;
public class Set : IEquatable<Set>, IReadOnlyList<int>, IReadOnlySet<int>
{
	public Set(IEnumerable<int> elements)
	{
		_elements = elements.OrderBy(x => x).ToArray();
		_hashCode = GenerateHashCode(_elements);
		_stringRepresentation = ToStringRepresentation(_elements);
	}

	private readonly int[] _elements;
	private readonly int _hashCode;

	private readonly string _stringRepresentation;

	public int Count => _elements.Length;

	public bool IsReadOnly => true;

	public int this[int index] => _elements[index];

	public ReadOnlySpan<int> AsSpan() => _elements.AsSpan();

	public override int GetHashCode() => _hashCode;

	private static int GenerateHashCode(int[] elements)
	{
		unchecked // Overflow is fine, just wrap
		{
			int hash = 19;
			foreach (var element in elements)
				hash = hash * 31 + element; // 31 is an arbitrary prime number that can be used for hashing

			return hash;
		}
	}

	public static string ToStringRepresentation(ReadOnlySpan<int> elements)
	{
		var sb = new StringBuilder();
		sb.Append('[');
		switch (elements.Length)
		{
			case 0:
				break;

			case 1:
				sb.Append(elements[0]);
				break;

			default:
				sb.Append(elements[0]);
				for (var i = 1; i < elements.Length; i++)
				{
					sb.Append(',');
					sb.Append(elements[i]);
				}
				break;
		}
		sb.Append(']');
		return sb.ToString();
	}

	public override string ToString()
		=> _stringRepresentation;

	public IEnumerator<int> GetEnumerator()
	{
		int len = _elements.Length;
		for (var i = 0; i < len; i++)
			yield return _elements[i];
	}
	IEnumerator IEnumerable.GetEnumerator()
		=> _elements.GetEnumerator();

	public bool Equals(Set? other)
		=> other is not null && _stringRepresentation == other._stringRepresentation;

	public override bool Equals(object? obj)
		=> Equals(obj as Set);

	public bool Contains(int item)
		 // use binary search to determine if the item exists.
		 => _elements.AsSpan().BinarySearch(item) >= 0;

	public bool IsProperSubsetOf(IEnumerable<int> other)
		=> _elements.Length < other.Count() && IsSubsetOf(other);

	public bool IsProperSupersetOf(IEnumerable<int> other)
		=> _elements.Length > other.Count() && IsSupersetOf(other);

	public bool IsSubsetOf(IEnumerable<int> other)
		=> !_elements.Except(other).Any();

	public bool IsSupersetOf(IEnumerable<int> other)
		=> !other.Except(_elements).Any();

	public bool Overlaps(IEnumerable<int> other)
		=> _elements.Any(other.Contains);

	public bool SetEquals(IEnumerable<int> other)
	{
		if(other is ICollection<int> c && c.Count != _elements.Length)
			return false;

		foreach(int item in other)
		{
			if(!Contains(item))
				return false;
		}

		return true;
	}

	public static bool operator ==(Set? left, Set? right)
		=> left?.Equals(right) ?? right is null;

	public static bool operator !=(Set? left, Set? right)
		=> !(left == right);
}