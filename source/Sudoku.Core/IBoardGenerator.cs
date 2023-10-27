// Ignore Spelling: Sudoku rnd

using System.Collections.Generic;

namespace Sudoku;

public interface IBoardGenerator
{
	IEnumerable<char> Generate();
}