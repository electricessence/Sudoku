using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using Throw;

namespace Sudoku.Models;

public class ReadOnlyVectorBlock<T> : IGrid<T>
{
	public ReadOnlyVectorBlock(ReadOnlyMemory<T> source, int width, int height)
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

	public ReadOnlyVectorBlock(ReadOnlyMemory<T> source, int size)
		: this(source, size, size) { }

	public ReadOnlyVectorBlock(ReadOnlyMemory<T> source)
		: this(source, GetSquare(source.Span)) { }

	protected static int GetSquare(ReadOnlySpan<T> source)
	{
		var sqrt = Math.Sqrt(source.Length);
		if (sqrt % 1 != 0)
			throw new ArgumentException("The size of the source must be a square number in order to automatically determine its dimensions.", nameof(source));

		return (int)sqrt;
	}

	protected int GetIndex(int x, int y)
	{
		x.Throw().IfNegative().IfGreaterThanOrEqualTo(ColCount);
		y.Throw().IfNegative().IfGreaterThanOrEqualTo(RowCount);
		return x + y * RowCount;
	}

	public T this[int x, int y]
		=> Values.Span[GetIndex(x, y)];

	public ReadOnlyMemory<T> Values { get; }

	public int ColCount { get; }
	public int RowCount { get; }

	public ReadOnlySubGrid GetSubGrid(int x, int y, int width = -1, int height = -1)
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

		return new(this, x, y, width, height);
	}

	IGrid<T> IGrid<T>.GetSubGrid(int x, int y, int width, int height)
		=> GetSubGrid(x, y, width, height);

	protected string? _toString;
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

	public record ReadOnlySubGrid(ReadOnlyVectorBlock<T> Source, int X, int Y, int Width, int Height) : IGrid<T>
	{
		protected void Validate(int x, int y)
		{
			x.Throw().IfNegative().IfGreaterThanOrEqualTo(ColCount);
			y.Throw().IfNegative().IfGreaterThanOrEqualTo(RowCount);
		}

		public T this[int x, int y]
		{
			get
			{
				Validate(x, y);
				return Source[_x + x, _y + y];
			}
		}

		public ReadOnlyVectorBlock<T> Source { get; } = Source;
		public int ColCount { get; } = Width;
		public int RowCount { get; } = Height;

		protected readonly int _x = X;
		protected readonly int _y = Y;

		public Rectangle<int> GetSubRect(
			int x, int y,
			int width = -1, int height = -1)
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

			return new(_x + x, _y + y, width, height);
		}

		public ReadOnlySubGrid GetSubGrid(
			int x, int y,
			int width = -1, int height = -1)
		{
			var rect = GetSubRect(x, y, width, height);
			return new(Source, rect.X, rect.X, rect.Width, rect.Height);
		}

		IGrid<T> IGrid<T>.GetSubGrid(int x, int y, int width, int height)
			=> GetSubGrid(x, y, width, height);
	}
}

public sealed class VectorBlock<T>(
	Memory<T> source,
	int width, int height)
	: ReadOnlyVectorBlock<T>(source, width, height)
{
	public VectorBlock(Memory<T> source, int size)
		: this(source, size, size) { }

	public VectorBlock(Memory<T> source)
		: this(source, GetSquare(source.Span)) { }

	public new T this[int x, int y]
	{
		get => base[x, y];
		set => source.Span[GetIndex(x, y)] = value;
	}

	[SuppressMessage("Style",
		"IDE1006:Naming Styles",
		Justification = "Consistency and to avoid collision.")]
	public record SubGrid(
		VectorBlock<T> source,
		int x, int y,
		int width, int height)
		: ReadOnlySubGrid(source, x, y, width, height)
	{
		public new T this[int x, int y]
		{
			get => base[x, y];
			set
			{
				Validate(x, y);
				source[_x + x, _y + y] = value;
			}
		}

		public new SubGrid GetSubGrid(
			int x, int y,
			int width = -1, int height = -1)
		{
			var rect = GetSubRect(x, y, width, height);
			return new(source, rect.X, rect.X, rect.Width, rect.Height);
		}
	}
}
