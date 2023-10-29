using Open.Disposable;
using Spectre.Console;
using System.Buffers;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Sudoku.Console;

public readonly struct SquareMatrix<T> : IReadOnlyList<T>, IComparable<SquareMatrix<T>>
	where T : IComparable<T>
{
	public SquareMatrix(ImmutableArray<T> vector, int size)
	{
		var len = size * size;
		if (vector.Length != len) throw new ArgumentException($"The size ({size}) does not match the vector length ({vector.Length}).");
		Vector = vector;
		Size = size;
	}

	public SquareMatrix(ImmutableArray<T> vector) : this(vector, GetSquareRoot(vector.Length))
	{

	}

	public SquareMatrix<TResult> Transform<TResult>(Func<T, TResult> transform)
		where TResult : IComparable<TResult>
		=> new(Vector.Select(transform).ToImmutableArray(), Size);

	public static SquareMatrix<T> Create(IReadOnlyCollection<T> vector, int size)
	{
		if (vector is null) throw new ArgumentNullException(nameof(vector));
		return new(vector is ImmutableArray<T> a ? a : vector.ToImmutableArray(), size);
	}

	public static SquareMatrix<T> Create(IReadOnlyList<T> vector, int size)
	{
		if (vector is null) throw new ArgumentNullException(nameof(vector));
		return new(vector is ImmutableArray<T> a ? a : vector.ToImmutableArray(), size);
	}

	public static SquareMatrix<T> Create(IReadOnlyList<T> vector)
	{
		if (vector is null) throw new ArgumentNullException(nameof(vector));
		var size = GetSquareRoot(vector.Count);
		return new(vector is ImmutableArray<T> a ? a : vector.ToImmutableArray(), size);
	}

	static SquareMatrix<T> CreateCore(T[,] matrix, int size)
	{
		var len = size * size;
		var vector = ImmutableArray.CreateBuilder<T>(len);
		vector.Count = len;
		for (var y = 0; y < size; ++y)
		{
			for (var x = 0; x < size; ++x)
			{
				vector[y * size + x] = matrix[x, y];
			}
		}
		return new SquareMatrix<T>(vector.MoveToImmutable(), size);
	}

	public static SquareMatrix<T> Create(T[,] matrix, int size)
	{
		if (matrix is null) throw new ArgumentNullException(nameof(matrix));
		if (matrix.GetLength(0) != size)
			throw new ArgumentException("Matrix width does not match the size.");
		if (matrix.GetLength(1) != size)
			throw new ArgumentException("Matrix height does not match the size.");

		return CreateCore(matrix, size);
	}

	public static SquareMatrix<T> Create(T[,] matrix)
	{
		if (matrix is null) throw new ArgumentNullException(nameof(matrix));
		var size = matrix.GetLength(0);
		if (size != matrix.GetLength(1)) throw new ArgumentException("Matrix is not square.");

		return CreateCore(matrix, size);
	}

	public static SquareMatrix<T> Create(IEnumerable<IEnumerable<T>> rows, int size, bool ignoreOversize = false)
	{
		var len = size * size;
		var vector = ImmutableArray.CreateBuilder<T>(len);
		vector.Count = len;

		var y = 0;
		foreach (var row in rows)
		{
			if (y == size)
			{
				if (!ignoreOversize) throw new ArgumentException($"Row count is greater than the size ({size}).");
				break;
			}
			var x = 0;
			foreach (var cell in row)
			{
				if (x == size)
				{
					if (!ignoreOversize) throw new ArgumentException($"Cell count is greater than the size ({size}).");
					break;
				}
				vector[y * size + x] = cell;
				++x;
			}
			++y;
		}
		return new SquareMatrix<T>(vector.MoveToImmutable(), size);
	}


	public static SquareMatrix<T> Create(T[] vector, int size)
	{
		if (vector is null) throw new ArgumentNullException(nameof(vector));
		return new(vector.ToImmutableArray(), size);
	}

	static int GetSquareRoot(int length)
	{
		var sqrt = Math.Sqrt(length);
		if (Math.Floor(sqrt) != sqrt) throw new ArgumentException("Vector is not square.");
		return (int)sqrt;
	}

	public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Vector).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public int CompareToVector(IReadOnlyList<T> other)
	{
		var len = Vector.Length;
		if (len != other.Count) throw new ArgumentException("Comparison is incompatible. Lengths are different.");
		for (var i = 0; i < len; ++i)
		{
			var a = Vector[i];
			var b = other[i];
			if (a == null) return b == null ? 0 : -b.CompareTo(a);
			var c = a.CompareTo(b);
			if (c != 0) return c;
		}
		return 0;
	}

	public int CompareTo(SquareMatrix<T> other) => CompareToVector(other.Vector);

	public ImmutableArray<T> Vector { get; }

	public int Size { get; }

	public readonly int Length
		=> Vector.Length;

	int IReadOnlyCollection<T>.Count => Length;

	public T this[int index]
		=> Vector[index];

	public T this[int x, int y]
		=> Vector[y * Size + x];

	public static implicit operator ImmutableArray<T>(SquareMatrix<T> square) => square.Vector;

	public static implicit operator SquareMatrix<T>(ImmutableArray<T> vector) => new(vector);
}

public static class SquareMatrixExtensions
{
	static IEnumerable<T> RowCore<T>(ImmutableArray<T> vector, int size, int index)
	{
		for (var x = 0; x < size; ++x)
			yield return vector[index * size + x];
	}

	static IEnumerable<T> ColumnCore<T>(ImmutableArray<T> vector, int size, int index)
	{
		for (var y = 0; y < size; ++y)
			yield return vector[y * size + index];
	}

	public static IEnumerable<T> Row<T>(this SquareMatrix<T> square, int index)
		where T : IComparable<T>
	{
		if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, "Must be at least zero.");
		var size = square.Size;
		if (index < size) return RowCore(square.Vector, size, index);
		throw new ArgumentOutOfRangeException(nameof(index), index, "Must be less than the size.");

	}

	public static IEnumerable<T> Column<T>(this SquareMatrix<T> square, int index)
		where T : IComparable<T>
	{
		if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, "Must be at least zero.");
		var size = square.Size;
		if (index < size) return ColumnCore(square.Vector, size, index);
		throw new ArgumentOutOfRangeException(nameof(index), index, "Must be less than the size.");
	}

	public static IEnumerable<IEnumerable<T>> Rows<T>(this SquareMatrix<T> square)
		where T : IComparable<T>
	{
		var size = square.Size;
		for (var y = 0; y < size; ++y)
			yield return square.Row(y);
	}

	public static IEnumerable<IEnumerable<T>> Columns<T>(this SquareMatrix<T> square)
		where T : IComparable<T>
	{
		var size = square.Size;
		for (var x = 0; x < size; ++x)
			yield return square.Column(x);
	}

	public static string ToMatrixString(this SquareMatrix<int> square)
		=> string.Join('\n', square.Rows().Select(r => string.Join(' ', r)));

	public static IEnumerable<T> GetMirrorX<T>(this SquareMatrix<T> square)
		where T : IComparable<T>
	{
		var size = square.Size;
		for (var y = 0; y < size; ++y)
		{
			for (var x = size - 1; x >= 0; --x)
			{
				yield return square[x, y];
			}
		}
	}

	public static IEnumerable<T> GetRotatedCW<T>(this SquareMatrix<T> square)
		where T : IComparable<T>
	{
		var size = square.Size;
		for (var x = 0; x < size; ++x)
		{
			for (var y = size - 1; y >= 0; --y)
			{
				yield return square[x, y];
			}
		}
	}

	public static IEnumerable<string> ToDisplayRowStrings<T>(this SquareMatrix<T> square)
		where T : IComparable<T>
	{
		var pool = ListPool<string[]>.Shared;
		var table = pool.Take();
		var size = square.Size;
		var colWidth = new int[size];
		for (var y = size - 1; y >= 0; --y)
		{
			var row = new string[size];
			for (var x = 0; x < size; ++x)
			{
				var v = square[x, y]?.ToString();
				Debug.Assert(v != null);
				colWidth[x] = Math.Max(colWidth[x], v!.Length);
				row[x] = v.ToString();
			}

			table.Add(row);
		}

		for (var c = 0; c < size; ++c)
		{
			var width = colWidth[c];
			for (var r = 0; r < size; ++r)
			{
				var value = table[r][c];
				var diff = width - value.Length;
				table[r][c] = new string(' ', diff) + value;
			}
		}

		foreach (var row in table)
			yield return string.Join(' ', row);

		pool.Give(table);
	}

	public static IEnumerable<string> ToDisplayRowStringsWithTotals(this SquareMatrix<int> square)
	{
		var pool = ListPool<string[]>.Shared;
		var table = pool.Take();
		var size = square.Size;
		var colSum = new int[size];
		for (var y = size - 1; y >= 0; --y)
		{
			var rowSum = 0;
			var row = new string[size + 2];
			for (var x = 0; x < size; ++x)
			{
				var v = square[x, y];
				rowSum += v;
				colSum[x] += v;
				row[x] = v.ToString();
			}
			row[^2] = "=";
			row[^1] = rowSum.ToString();
			table.Add(row);
		}
		var divider = new string[size];
		var colSumRow = colSum.Select(s => s.ToString()).ToArray();

		for (var c = 0; c < size; ++c)
		{
			var width = colSumRow[c].Length;
			divider[c] = new string('-', width);
			for (var r = 0; r < size; ++r)
			{
				var value = table[r][c];
				var diff = width - value.Length;
				table[r][c] = new string(' ', diff) + value;
			}
		}

		foreach (var row in table)
			yield return string.Join(' ', row);

		pool.Give(table);
		yield return string.Join('-', divider);

		yield return string.Join(' ', colSumRow);
	}

	public static void OutputToConsole<T>(this SquareMatrix<T> square)
		where T : IComparable<T>
	{
		foreach (var row in square.ToDisplayRowStrings())
		{
			AnsiConsole.WriteLine(row);
		}
	}

	public static void OutputToConsoleWithTotals(this SquareMatrix<int> square)
	{
		foreach (var row in square.ToDisplayRowStringsWithTotals())
		{
			AnsiConsole.WriteLine(row);
		}
	}

	public static bool MagicSquareQuality(this SquareMatrix<int> square, int sum = 0)
	{
		var pool = ArrayPool<int>.Shared;
		var size = square.Size;
		var columns = pool.Rent(size);
		var right = 0;
		var left = 0;
		try
		{
			for (var y = 0; y < size; ++y)
			{
				var rowSum = 0;
				for (var x = 0; x < size; ++x)
				{
					var cell = square[x, y];
					rowSum += cell;
					if (y == 0) columns[x] = cell;
					else columns[x] += cell;
					if (x == y) right += cell;
					if (size - x - 1 == y) left += cell;
				}

				if (sum == 0) sum = rowSum;
				else if (rowSum != sum) return false;
			}

			for (var i = 0; i < size; i++)
				if (columns[i] != sum) return false;

			if (left != sum || right != sum) return false;

			return true;
		}
		finally
		{
			pool.Return(columns);
		}

	}
}