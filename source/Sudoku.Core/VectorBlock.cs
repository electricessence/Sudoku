using System;
using System.Text;
using System.Threading;
using Throw;

namespace Sudoku.Core;
public class VectorBlock<T> : IGrid<T>
{
	public VectorBlock(ReadOnlyMemory<T> source, int width, int height)
	{
		// Validate parameters.
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

		ColCount = width;
		RowCount = height;

		var len = width * height;
		source.Length.Throw().IfLessThan(len);
		Values = source.Length == len ? source : source[..len];
	}

	public VectorBlock(ReadOnlyMemory<T> source, int size)
		: this(source, size, size) { }

	public VectorBlock(ReadOnlyMemory<T> source)
		: this(source, GetSquare(source.Span)) { }

	protected static int GetSquare(ReadOnlySpan<T> source)
	{
		var sqrt = Math.Sqrt(source.Length);
		if (sqrt % 1 != 0)
			throw new ArgumentException("The size of the source must be a square number in order to automatically determine its dimensions.", nameof(source));
		return (int)sqrt;
	}

	public T this[int x, int y]
	{
		get
		{
			x.Throw().IfNegative().IfGreaterThanOrEqualTo(ColCount);
			y.Throw().IfNegative().IfGreaterThanOrEqualTo(RowCount);

			return Values.Span[x + y * RowCount];
		}
	}

	public ReadOnlyMemory<T> Values { get; }

	public int ColCount { get; }
	public int RowCount { get; }

	public IGrid<T> GetSubGrid(int x, int y, int width = -1, int height = -1)
	{
		x.Throw().IfNegative().IfGreaterThanOrEqualTo(ColCount);
		y.Throw().IfNegative().IfGreaterThanOrEqualTo(RowCount);
		var newMaxWidth = ColCount - x;
		var newMaxHeight = RowCount - y;
		width.Throw().IfEquals(0).IfLessThan(-1).IfGreaterThan(newMaxWidth);
		height.Throw().IfEquals(0).IfLessThan(-1).IfGreaterThan(newMaxHeight);

		if (width == -1)
			width = newMaxWidth;
		if (height == -1)
			height = newMaxHeight;

		return new SubGrid(this, x, y, width, height);
	}

	string? _toString;
	public override string ToString()
		=> LazyInitializer.EnsureInitialized(ref _toString, () =>
		{
			var sb = new StringBuilder();
			for (var y = 0; y < RowCount; y++)
			{
				if (y != 0) sb.AppendLine();
				sb.Append(this[0, y]);
				for (var x = 1; x < ColCount; x++)
				{
					sb.Append(' ');
					sb.Append(this[x, y]);
				}
			}
			return sb.ToString();
		});

	readonly record struct SubGrid(VectorBlock<T> Source, int X, int Y, int Width, int Height) : IGrid<T>
	{
		public T this[int x, int y]
		{
			get
			{
				x.Throw().IfNegative().IfGreaterThanOrEqualTo(ColCount);
				y.Throw().IfNegative().IfGreaterThanOrEqualTo(RowCount);

				return Source[X + x, Y + y];
			}
		}

		public readonly VectorBlock<T> Source = Source;
		public int ColCount { get; } = Width;
		public int RowCount { get; } = Height;

		private readonly int X = X;
		private readonly int Y = Y;

		public IGrid<T> GetSubGrid(int x, int y, int width = -1, int height = -1)
		{
			x.Throw().IfNegative().IfGreaterThanOrEqualTo(ColCount);
			y.Throw().IfNegative().IfGreaterThanOrEqualTo(RowCount);
			var newMaxWidth = ColCount - x;
			var newMaxHeight = RowCount - y;
			width.Throw().IfEquals(0).IfLessThan(-1).IfGreaterThan(newMaxWidth);
			height.Throw().IfEquals(0).IfLessThan(-1).IfGreaterThan(newMaxHeight);

			if (width == -1)
				width = newMaxWidth;
			if (height == -1)
				height = newMaxHeight;

			return new SubGrid(Source, X + x, Y + y, width, height);
		}
	}
}
