﻿using System;

namespace Sudoku.Models;

public readonly struct GridSegment<T> : IGrid<T>
{
	readonly T[,] _source;
	readonly int _x;
	readonly int _y;

	public int RowCount { get; }

	public int ColCount { get; }

	public GridSegment(
		T[,] sourceGrid,
		int x, int y,
		int width, int height)
	{
		Validate(sourceGrid, x, y, width, height);

		_source = sourceGrid;
		_x = x;
		_y = y;
		ColCount = width;
		RowCount = height;
	}

	public GridSegment(
		T[,] sourceGrid,
		Rectangle<int> rect)
		: this(sourceGrid, rect.X, rect.Y, rect.Width, rect.Height) { }

	public static void Validate(
		T[,] sourceGrid,
		int x, int y,
		int width, int height)
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
	}

	public static void Validate(
		T[,] sourceGrid,
		Rectangle<int> rect)
		=> Validate(sourceGrid,
			rect.X, rect.Y,
			rect.Width, rect.Height);

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

	public GridSegment<T> GetSubGrid(
		int x, int y,
		int width = -1, int height = -1)
	{
		if (width == -1)
			width = ColCount - x;
		if (height == -1)
			height = RowCount - y;

		return new(_source, _x + x, _y + y, width, height);
	}

	IGrid<T> IGrid<T>.GetSubGrid(
		int x, int y,
		int width, int height)
		=> GetSubGrid(x, y, width, height);
}