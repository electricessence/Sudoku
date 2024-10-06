// Ignore Spelling: Sudoku rnd

namespace Sudoku.Core;

public interface IBoardGenerator
{
	IEnumerable<char> Generate();
}