using System.Collections;
using System.Text;

namespace CombinationElimination;

/// <summary>
/// Represents an immutable set of integers, sorted in ascending order.
/// </summary>
public class Set : IEquatable<Set>, IComparable<Set>, IReadOnlyList<int>, IReadOnlySet<int>
{
	/// <summary>
	/// Initializes a new instance of the Set class with the specified elements.
	/// </summary>
	/// <param name="elements">The elements to include in the set.</param>
	public Set(IEnumerable<int> elements)
		: this(elements.ToArray()) { }

	/// <inheritdoc cref="Set(IEnumerable{int})"/>"
	public Set(ReadOnlySpan<int> elements)
		: this(elements.ToArray()) { }

	private Set(int[] elements)
	{
		Array.Sort(elements);
		_elements = elements;
		_hashCode = GenerateHashCode(_elements);
		_stringRepresentation = ToStringRepresentation(_elements);
	}

	private readonly int[] _elements;
	private readonly int _hashCode;
	private readonly string _stringRepresentation;

	/// <inheritdoc />
	public int Count => _elements.Length;

	/// <inheritdoc />
	public int this[int index] => _elements[index];

	/// <summary>
	/// Returns a read-only span representing the Set elements.
	/// </summary>
	/// <returns>A read-only span of integers.</returns>
	public ReadOnlySpan<int> AsSpan() => _elements.AsSpan();

	/// <inheritdoc />
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

	private static string ToStringRepresentation(ReadOnlySpan<int> elements)
	{
		var sb = new StringBuilder(elements.Length * 4 + 2);
		sb.Append('{');
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
					sb.Append(',').Append(' ');
					sb.Append(elements[i]);
				}
				break;
		}
		sb.Append('}');
		return sb.ToString();
	}

	/// <inheritdoc />
	public override string ToString() => _stringRepresentation;

	/// <inheritdoc />
	public IEnumerator<int> GetEnumerator()
	{
		int len = _elements.Length;
		for (var i = 0; i < len; i++)
			yield return _elements[i];
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator() => _elements.GetEnumerator();

	/// <inheritdoc />
	public bool Equals(Set? other) => other is not null && _stringRepresentation == other._stringRepresentation;

	/// <inheritdoc />
	public override bool Equals(object? obj) => Equals(obj as Set);

	/// <inheritdoc />
	public bool Contains(int item)
		// Use binary search to determine if the item exists.
		=> _elements.AsSpan().BinarySearch(item) >= 0;

	/// <inheritdoc />
	public bool IsProperSubsetOf(IEnumerable<int> other)
		=> _elements.Length < other.Count() && IsSubsetOf(other);

	/// <inheritdoc />
	public bool IsProperSupersetOf(IEnumerable<int> other)
		=> _elements.Length > other.Count() && IsSupersetOf(other);

	/// <inheritdoc />
	public bool IsSubsetOf(IEnumerable<int> other)
		=> !_elements.Except(other).Any();

	/// <inheritdoc />
	public bool IsSupersetOf(IEnumerable<int> other)
		=> !other.Except(_elements).Any();

	/// <inheritdoc />
	public bool Overlaps(IEnumerable<int> other)
		=> _elements.Any(other.Contains);

	/// <inheritdoc />
	public bool SetEquals(IEnumerable<int> other)
	{
		if (other is ICollection<int> c && c.Count != _elements.Length)
			return false;

		foreach (int item in other)
		{
			if (!Contains(item))
				return false;
		}

		return true;
	}

	/// <inheritdoc />
	public int CompareTo(Set? other)
	{
		if (other is null)
			return 1;

		if (this == other)
			return 0;

		int len = _elements.Length;
		int lenDif = len - other._elements.Length;
		if (lenDif != 0)
			return lenDif;

		var span = _elements.AsSpan();
		var spanOther = other._elements.AsSpan();
		for (int i = 0; i < len; i++)
		{
			int dif = span[i] - spanOther[i];
			if (dif != 0)
				return dif;
		}

		return 0;
	}

	/// <summary>
	/// Determines if two instances of Set are equal.
	/// </summary>
	public static bool operator ==(Set? left, Set? right)
		=> left?.Equals(right) ?? right is null;

	/// <summary>
	/// Determines if two instances of Set are not equal.
	/// </summary>
	public static bool operator !=(Set? left, Set? right)
		=> !(left == right);

	public static implicit operator Set(ReadOnlySpan<int> set)
		=> new(set);

	public static implicit operator Set(int[] set)
		=> new(set);
}
