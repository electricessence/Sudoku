// Ignore Spelling: Sudoku rnd

using System;
using System.Linq;

// https://github.com/nilamsavani/SudokuGenerator
// https://github.com/nilamsavani/SudokuGenerator/blob/master/SudokuGenerator/SudokuGenerator/Logic/Sudoku.cs

namespace Sudoku;

public class Generator2
{
	//Retrieve array of 81 integers
	public string GenerateSudokuBoard()
	{
		//Initialize variables
		int[,] sudokuGrid = new int[9, 9];
		Random rndNumber = new();
		//int[] result = new int[81];

		//Fill cells diagonally for each 3*3 grid
		FillAllDiagonalCells(ref sudokuGrid, ref rndNumber);

		//Fill remaining cells for each 3*3 grid
		FillOtherCells(0, 3, ref sudokuGrid);

		//check validity of sudoku
		bool isValid = ValidateSudokuBoard(ref sudokuGrid);

		if (isValid)
		{
			//Convert 2-D array of 9*9 to 1-D of 81 integers
			var str = "";
			for (int i = 0; i < 9; i++)
			{
				for (int j = 0; j < 9; j++)
				{
					str += sudokuGrid[i, j] == 0 ? "." : sudokuGrid[i, j].ToString();
				}
			}

			return str;
		}

		return "";
	}

	//Fill all diagonal cells
	public void FillAllDiagonalCells(ref int[,] sudokuGrid, ref Random rndNumber)
	{
		for (int i = 0; i < 9; i += 3)
		{
			FillBox(i, i, ref sudokuGrid, ref rndNumber);
		}
	}

	//Fill each cell
	public void FillBox(int row, int col, ref int[,] sudokuGrid, ref Random rndNumber)
	{
		int num;
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				do
				{
					num = rndNumber.Next(1, 10);
				} while (!CheckNotUsedInGrid(row, col, num, ref sudokuGrid));

				sudokuGrid[row + i, col + j] = num;
			}
		}
	}

	//Check if grid contains the number
	public bool CheckNotUsedInGrid(int rowStart, int colStart, int num, ref int[,] sudokuGrid)
	{
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				if (sudokuGrid[rowStart + i, colStart + j] == num)
				{
					return false;
				}
			}
		}
		return true;
	}

	//Check if it is safe to put the number in a cell
	public bool CheckIfSafeToPlace(int i, int j, int num, ref int[,] sudokuGrid) => CheckNotUsedInRow(i, num, ref sudokuGrid) &&
			CheckNotUsedInColumn(j, num, ref sudokuGrid) &&
			CheckNotUsedInGrid(i - i % 3, j - j % 3, num, ref sudokuGrid);

	//Check if number is already there in a row
	public bool CheckNotUsedInRow(int i, int num, ref int[,] sudokuGrid)
	{
		for (int j = 0; j < 9; j++)
		{
			if (sudokuGrid[i, j] == num)
			{
				return false;
			}
		}

		return true;
	}

	//Check if number is already there in a column
	public bool CheckNotUsedInColumn(int j, int num, ref int[,] sudokuGrid)
	{
		for (int i = 0; i < 9; i++)
		{
			if (sudokuGrid[i, j] == num)
			{
				return false;
			}
		}

		return true;
	}

	//Fill all the remaining cells excepts diagonal
	public bool FillOtherCells(int i, int j, ref int[,] sudokuGrid)
	{
		if (j >= 9 && i < 9 - 1)
		{
			i++;
			j = 0;
		}
		if (i >= 9 && j >= 9)
		{
			return true;
		}

		if (i < 3)
		{
			if (j < 3)
			{
				j = 3;
			}
		}
		else if (i < 6)
		{
			if (j == (int)(i / 3) * 3)
			{
				j += 3;
			}
		}
		else
		{
			if (j == 6)
			{
				i++;
				j = 0;
				if (i >= 9)
				{
					return true;
				}
			}
		}

		for (int num = 1; num <= 9; num++)
		{
			if (CheckIfSafeToPlace(i, j, num, ref sudokuGrid))
			{
				sudokuGrid[i, j] = num;
				if (FillOtherCells(i, j + 1, ref sudokuGrid))
				{
					return true;
				}

				sudokuGrid[i, j] = 0;
			}
		}
		return false;
	}

	//Check if sudoku is valid or not
	public bool ValidateSudokuBoard(ref int[,] sudokuGrid)
	{
		for (int i = 0; i < 9; i++)
		{
			bool[] row = new bool[10];
			bool[] col = new bool[10];
			int[] validNumbers = new int[9] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

			for (int j = 0; j < 9; j++)
			{
				//Check if number other than 1 to 9 is present
				if (!validNumbers.Contains(sudokuGrid[i, j]))
				{
					return false;
				}

				//Check if number already exist in a row
				if (row[sudokuGrid[i, j]] && sudokuGrid[i, j] > 0)
				{
					return false;
				}
				row[sudokuGrid[i, j]] = true;

				//Check if number already exist in a column
				if (col[sudokuGrid[j, i]] && sudokuGrid[j, i] > 0)
				{
					return false;
				}
				col[sudokuGrid[j, i]] = true;

				//Check if number already exist in 3*3 grid
				if ((i + 3) % 3 == 0 && (j + 3) % 3 == 0)
				{
					bool[] sqr = new bool[10];
					for (int m = i; m < i + 3; m++)
					{
						for (int n = j; n < j + 3; n++)
						{
							if (sqr[sudokuGrid[m, n]] && sudokuGrid[m, n] > 0)
							{
								return false;
							}
							sqr[sudokuGrid[m, n]] = true;
						}
					}
				}
			}
		}
		return true;
	}
}