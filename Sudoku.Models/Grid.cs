using System;

namespace Sudoku.Models;
public class Grid<T> : IGrid<T>
{
	protected T[,] Source { get; }

	public Grid(int width, int height)
	{
		// Validate parameters.
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

		ColCount = width;
		RowCount = height;

		Source = new T[width, height];
	}

	public Grid(int size)
		: this(size, size) { }

	public int ColCount { get; }
	public int RowCount { get; }

	public T this[int x, int y]
	{
		get => Source[x, y];
		set => Source[x, y] = value;
	}

	public GridSegment<T> GetSubGrid(
		int x, int y,
		int width = -1, int height = -1)
	{
		if (width == -1)
			width = ColCount - x;
		if (height == -1)
			height = RowCount - y;

		return new(Source, x, y, width, height);
	}

	IGrid<T> IGrid<T>.GetSubGrid(
		int x, int y,
		int width, int height)
		=> GetSubGrid(x, y, width, height);
}
