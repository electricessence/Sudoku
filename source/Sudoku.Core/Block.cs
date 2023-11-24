using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Sudoku.Core;
public class Block : VectorBlock<int>
{
	public Block(ReadOnlyMemory<int> source)
		: base(source) => Validate(source.Span);

	private Block(ReadOnlySpan<int> source)
		: base(Validate(source).ToArray().AsMemory()) {}

	public static Block Create(ReadOnlySpan<int> source) => new(source);

	public static Block Create<T>(ReadOnlySpan<T> source)
		where T : IEnumerable<int>
	{
		var list = new List<int>();
		foreach (var item in source)
			list.AddRange(item);
		return new(list.ToArray().AsMemory());
	}

	protected static ReadOnlySpan<int> Validate(ReadOnlySpan<int> source)
	{
		var end = source.Length + 1;
		for (var i = 1; i < end; i++)
		{
			if (!source.Contains(i))
				throw new ArgumentException("The source must contain all values from 1 to the length of the source.");
		}

		return source;
	}


	string? _familyID;
	public string FamilyID
		=> LazyInitializer.EnsureInitialized(ref _familyID, () =>
		{
			var len = Values.Length;
			using var lease = MemoryPool<(IOrderedEnumerable<int> row, IOrderedEnumerable<int> col)>.Shared.Rent(len);
			var values = lease.Memory.Span[..len];

			for (var y = 0; y < RowCount; y++)
			{
				for (var x = 0; x < RowCount; x++)
				{
					var i = this[x, y] - 1;
					values[i] = (this.GetRow(y).Order(), this.GetColumn(x).Order());
				}
			}

			var sb = new StringBuilder();
			for (var i = 0; i < len; i++)
			{
				var (row, col) = values[i];
				sb.AppendRepresentation(row);
				sb.AppendRepresentation(col);
			}

			return sb.ToString();
		});
}
