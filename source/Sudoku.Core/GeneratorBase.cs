using System;
using System.Collections.Generic;

namespace Sudoku.Core;
public abstract class GeneratorBase : IBoardGenerator
{
	protected Func<int, int, int> NextInt { get; }

	protected GeneratorBase(Func<int, int, int> nextIntFunction)
		=> NextInt = nextIntFunction;

	protected GeneratorBase(Random rnd)
		: this(rnd.Next) { }

	protected GeneratorBase(int seed)
		: this(new Random(seed)) { }

	protected GeneratorBase()
		: this(new Random()) { }

	public abstract IEnumerable<char> Generate();
}
