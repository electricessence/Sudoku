using System;

namespace Sudoku.Core;
public class Grid<T> : IGrid<T>
{
	protected T[,] Source { get; }

	public Grid(int width, int height)
	{
		// Validate parameters.
		if (width <= 0)
			throw new ArgumentOutOfRangeException(nameof(width));
		if (height <= 0)
			throw new ArgumentOutOfRangeException(nameof(height));

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

	public GridSegment<T> GetSubGrid(int x, int y, int width = -1, int height = -1)
	{
		if(width == -1)
			width = ColCount - x;
		if(height == -1)
			height = RowCount - y;

		return new GridSegment<T>(Source, x, y, width, height);
	}
}
