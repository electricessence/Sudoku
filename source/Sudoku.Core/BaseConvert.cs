using Open.Disposable;
using System;
using System.Numerics;
using System.Text;

namespace Sudoku.Core;
public static class BaseConvert
{
	/// <summary>
	/// Convert from any base (2 to 36) to BigInteger.
	/// </summary>
	public static BigInteger ToBigInteger(this string number, byte fromBase = 10)
		=> fromBase == 10
		? BigInteger.Parse(number)
		: ConvertToBigInteger(number.AsSpan(), fromBase);

	/// <summary>
	/// Convert from any base (2 to 36) to BigInteger.
	/// </summary>
	public static BigInteger ConvertToBigInteger(ReadOnlySpan<char> number, byte fromBase)
	{
		if (fromBase is < 2 or > 36)
			throw new ArgumentOutOfRangeException(nameof(fromBase), fromBase, "Must be between 2 and 36");

		if (number.Length == 0)
			return BigInteger.Zero;

		BigInteger result = 0;
		BigInteger multiplier = 1;

		int first = number[0] == '-' ? 1 : 0; // Adjust for negative sign
		bool isNegative = first == 1;

		int last = number.Length - 1;
		if (last < first)
			return BigInteger.Zero;

		for (int i = last; i >= first; i--) // Start from the end, skip '-' if present
		{
			int digit = number[i] - (number[i] <= '9' ? '0' : ('A' - 10));
			if (digit >= fromBase || digit < 0)
				throw new ArgumentException("Invalid character in the number for the specified base", nameof(number));

			result += digit * multiplier;
			multiplier *= fromBase;
		}

		return isNegative ? BigInteger.Negate(result) : result;
	}

	/// <summary>
	/// Convert from BigInteger to any base (2 to 36).
	/// </summary>
	public static string ToString(this BigInteger number, byte toBase)
	{
		if (toBase is < 2 or > 36)
			throw new ArgumentOutOfRangeException(nameof(toBase), toBase, "Must be between 2 and 36");

		if (number == 0) return "0";

		using var lease = StringBuilderPool.Rent();
		StringBuilder result = lease.Item;

		bool isNegative = number < 0;
		number = BigInteger.Abs(number);

		while (number > 0)
		{
			number = BigInteger.DivRem(number, toBase, out BigInteger remainder);
			char digit = (remainder < 10) ? (char)('0' + remainder) : (char)('A' + remainder - 10);
			result.Insert(0, digit);
		}

		if (isNegative)
			result.Insert(0, '-');

		return result.ToString();
	}
}
