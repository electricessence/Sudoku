using System;

namespace Sudoku.Models;

public class SquaredGrid<T> : Grid<T>
{
	/// <summary>
	/// Represents the size of the grid.
	/// </summary>
	public int Size { get; }

	/// <summary>
	/// Represents the square root of the size of the grid.
	/// </summary>
	public int Square { get; }

	/// <summary>
	/// Constructs a new squared grid with a specified square root size.
	/// </summary>
	/// <param name="square">The square root of the size of the grid.</param>
	public SquaredGrid(byte square)
		: base(square * square, square * square)
	{
		if (square < 2)
			throw new ArgumentException("Square must be at least 2.");

		Square = square;
		Size = square * square;
	}

	public GridSegment<T> GetSubSquare(int x, int y)
		=> GetSubGrid(x * Square, y * Square, Square, Square);
}