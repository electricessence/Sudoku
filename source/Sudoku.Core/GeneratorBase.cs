namespace Sudoku.Core;
public abstract class GeneratorBase(Func<int, int, int> nextIntFunction) : IBoardGenerator
{
	protected Func<int, int, int> NextInt { get; } = nextIntFunction;

	protected GeneratorBase(Random rnd)
		: this(rnd.Next) { }

	protected GeneratorBase(int seed)
		: this(new Random(seed)) { }

	protected GeneratorBase()
		: this(new Random()) { }

	public abstract IEnumerable<char> Generate();
}
