using System;

namespace Sudoku.Core;

public readonly struct GridSegment<T> : IGrid<T>
{
	readonly T[,] _source;
	readonly int _x;
	readonly int _y;

	public int RowCount { get; }

	public int ColCount { get; }

	public GridSegment(T[,] sourceGrid, int x, int y, int width, int height)
	{
		ArgumentNullException.ThrowIfNull(sourceGrid);

		if (x < 0 || x >= sourceGrid.GetLength(0))
			throw new ArgumentOutOfRangeException(nameof(x));
		if (y < 0 || y >= sourceGrid.GetLength(1))
			throw new ArgumentOutOfRangeException(nameof(y));

		if (width < 0 || width > sourceGrid.GetLength(0) - x)
			throw new ArgumentOutOfRangeException(nameof(width));
		if (height < 0 || height > sourceGrid.GetLength(1) - y)
			throw new ArgumentOutOfRangeException(nameof(height));

		_source = sourceGrid;
		_x = x;
		_y = y;
		ColCount = width;
		RowCount = height;
	}

	public T this[int x, int y]
	{
		get
		{
			if (x < 0 || x >= ColCount || y < 0 || y >= RowCount)
				throw new IndexOutOfRangeException();

			return _source[_x + x, _y + y];
		}
		set
		{
			if (x < 0 || x >= ColCount || y < 0 || y >= RowCount)
				throw new IndexOutOfRangeException();

			_source[_x + x, _y + y] = value;
		}
	}

	public IGrid<T> GetSubGrid(int x, int y, int width = -1, int height = -1)
	{
		if (width == -1)
			width = ColCount - x;
		if (height == -1)
			height = RowCount - y;

		return new GridSegment<T>(_source, _x + x, _y + y, width, height);
	}
}