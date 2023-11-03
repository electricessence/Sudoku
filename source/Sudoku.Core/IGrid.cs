using System;
using System.Collections.Generic;
using System.Text;

namespace Sudoku.Core;
public interface IGrid<T>
{
	/// <summary>
	/// Gets or sets the value at the specified row and column indexes.
	/// </summary>
	T this[int x, int y] { get; set; }

	/// <summary>
	/// Gets the number of columns in the grid.
	/// </summary>
	int ColCount { get; }

	/// <summary>
	/// Gets the number of rows in the grid.
	/// </summary>
	int RowCount { get; }

	GridSegment<T> GetSubGrid(int x, int y, int width = -1, int height = -1);
}


public static class GridExtensions
{
	/// <summary>
	/// Retrieves a row as an IEnumerable of T.
	/// </summary>
	/// <param name="grid">The grid.</param>
	/// <param name="rowIndex">The row index.</param>
	/// <returns>An IEnumerable of T representing the row.</returns>
	public static IEnumerable<T> GetRow<T>(this IGrid<T> grid, int rowIndex)
	{
		for (int x = 0; x < grid.ColCount; x++)
			yield return grid[x, rowIndex];
	}

	/// <summary>
	/// Retrieves a column as an IEnumerable of T.
	/// </summary>
	/// <param name="grid">The grid.</param>
	/// <param name="columnIndex">The column index.</param>
	/// <returns>An IEnumerable of T representing the column.</returns>
	public static IEnumerable<T> GetColumn<T>(this IGrid<T> grid, int columnIndex)
	{
		for (int y = 0; y < grid.RowCount; y++)
			yield return grid[columnIndex, y];
	}

	public static IEnumerable<IEnumerable<T>> Rows<T>(this IGrid<T> grid)
	{
		for (int y = 0; y < grid.RowCount; y++)
			yield return grid.GetRow(y);
	}

	public static IEnumerable<IEnumerable<T>> Columns<T>(this IGrid<T> grid)
	{
		for (int x = 0; x < grid.ColCount; x++)
			yield return grid.GetColumn(x);
	}
}
