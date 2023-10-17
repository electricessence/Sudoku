using System;
using System.Collections.Generic;
using System.Linq;

namespace Sudoku;

/// <summary>
/// Ported from http://norvig.com/sudo.py by Richard Birkby, June 2007.
/// See http://norvig.com/sudoku.html
/// Also, https://bugzilla.mozilla.org/attachment.cgi?id=266577 - Javascript1.8 version
/// </summary>
public static class Solver
{
	// Throughout this program we have:
	//   r is a row,    e.g. 'A'
	//   c is a column, e.g. '3'
	//   s is a square, e.g. 'A3'
	//   d is a digit,  e.g. '9'
	//   u is a unit,   e.g. ['A1','B1','C1','D1','E1','F1','G1','H1','I1']
	//   g is a grid,   e.g. 81 non-blank chars, e.g. starting with '.18...7...
	//   values is a dict of possible values, e.g. {'A1':'123489', 'A2':'8', ...}
	const string rows = "ABCDEFGHI";
	const string cols = "123456789";
	const string digits = "123456789";
	static readonly string[] squares = Cross(rows, cols);
	static readonly List<string[]> unitlist;
	static readonly IDictionary<string, IEnumerable<string>> peers;
	static readonly IDictionary<string, IGrouping<string, string[]>> units;

	/*
	 * def cross(A, B):
	 *   return [a+b for a in A for b in B]
	 */
	static string[] Cross(string A, string B) => (from a in A from b in B select "" + a + b).ToArray();

	static Solver()
	{
		/*
		 * unitlist = ([cross(rows, c) for c in cols] +
		 *           [cross(r, cols) for r in rows] +
		 *           [cross(rs, cs) for rs in ('ABC','DEF','GHI') for cs in ('123','456','789')])
		 */
		unitlist = (from c in cols select Cross(rows, c.ToString()))
			.Concat(from r in rows select Cross(r.ToString(), cols))
			.Concat(from rs in new[] { "ABC", "DEF", "GHI" } from cs in new[] { "123", "456", "789" } select Cross(rs, cs)).ToList();

		/*
		 * units = dict((s, [u for u in unitlist if s in u]) 
		 *   for s in squares)
		 */
		units = (from s in squares from u in unitlist where u.Contains(s) group u by s into g select g).ToDictionary(g => g.Key);

		/*
		 * peers = dict((s, set(s2 for u in units[s] for s2 in u if s2 != s))
		 *   for s in squares)
		 */
		peers = (from s in squares from u in units[s] from s2 in u where s2 != s group s2 by s into g select g).ToDictionary(g => g.Key, g => g.Distinct());
	}

	/* [Javascript1.8]
	 * function zip(A, B) {
	 *   let z = []
	 *   let n = Math.min(A.length, B.length)
	 *   for (let i = 0; i < n; i++)
	 *     z.push([A[i], B[i]])
	 *   return z
	 * }
	 */
	static string[][] Zip(string[] A, string[] B)
	{
		var n = Math.Min(A.Length, B.Length);
		string[][] sd = new string[n][];
		for (var i = 0; i < n; i++)
		{
			sd[i] = new string[] { A[i], B[i] };
		}

		return sd;
	}

	/*
	def parse_grid(grid):
		"Given a string of 81 digits (or . or 0 or -), return a dict of {cell:values}"
		grid = [c for c in grid if c in '0.-123456789']
		values = dict((s, digits) for s in squares) ## To start, every square can be any digit
		for s,d in zip(squares, grid):
			if d in digits and not assign(values, s, d):
			return False
		return values
	*/
	/// <summary>Given a string of 81 digits (or . or 0 or -), return a dict of {cell:values}</summary>
	public static IDictionary<string, string>? ParseGrid(string grid)
	{
		//var grid2 = from c in grid where "0.-123456789".Contains(c) select c;
		var values = squares.ToDictionary(s => s, _ => digits); //To start, every square can be any digit

		foreach (var sd in Zip(squares, (from s in grid select s.ToString()).ToArray()))
		{
			var s = sd[0];
			var d = sd[1];

			if (digits.Contains(d) && Assign(values, s, d) == null)
			{
				return null;
			}
		}

		return values;
	}

	/*
	 * def search(values):
	 *   "Using depth-first search and propagation, try all possible values."
	 *   if values is False:
	 *     return False ## Failed earlier
	 *   if all(len(values[s]) == 1 for s in squares): 
	 *     return values ## Solved!
	 *   ## Chose the unfilled square s with the fewest possibilities
	 *   _,s = min((len(values[s]), s) for s in squares if len(values[s]) > 1)
	 *   return some(search(assign(values.copy(), s, d)) 
	 *           for d in values[s])
	 */
	/// <summary>Using depth-first search and propagation, try all possible values.</summary>
	public static IDictionary<string, string>? Search(IDictionary<string, string>? values)
	{
		if (values == null)
		{
			return null; // Failed earlier
		}

		if (All(from s in squares select values[s].Length == 1 ? "" : null))
		{
			return values; // Solved!
		}

		// Chose the unfilled square s with the fewest possibilities
		var s2 = (from s in squares where values[s].Length > 1 orderby values[s].Length ascending select s).First();

		return Some(
			from d in values[s2]
			select Search(Assign(new Dictionary<string, string>(values), s2, d.ToString())));
	}

	/*
	 * def assign(values, s, d):
	 *   "Eliminate all the other values (except d) from values[s] and propagate."
	 *   if all(eliminate(values, s, d2) for d2 in values[s] if d2 != d):
	 *     return values
	 *   else:
	 *     return False
	 */
	/// <summary>Eliminate all the other values (except d) from values[s] and propagate.</summary>
	static IDictionary<string, string>? Assign(IDictionary<string, string> values, string s, string d)
	{
		if (All(
				from d2 in values[s]
				where d2.ToString() != d
				select Eliminate(values, s, d2.ToString())))
		{
			return values;
		}

		return null;
	}

	// Eliminate d from values[s]; propagate when values or places <= 2.
	/* def eliminate(values, s, d):
	 *   "Eliminate d from values[s]; propagate when values or places <= 2."
	 *   if d not in values[s]:
	 *       return values ## Already eliminated
	 *   values[s] = values[s].replace(d,'')
	 *   if len(values[s]) == 0:
	 *       return False ## Contradiction: removed last value
	 *   elif len(values[s]) == 1:
	 *       ## If there is only one value (d2) left in square, remove it from peers
	 *       d2, = values[s]
	 *       if not all(eliminate(values, s2, d2) for s2 in peers[s]):
	 *           return False
	 *   ## Now check the places where d appears in the units of s
	 *   for u in units[s]:
	 *       dplaces = [s for s in u if d in values[s]]
	 *       if len(dplaces) == 0:
	 *           return False
	 *       elif len(dplaces) == 1:
	 *           # d can only be in one place in unit; assign it there
	 *           if not assign(values, dplaces[0], d):
	 *               return False
	 *   return values
	 */
	/// <summary>Eliminate d from values[s]; propagate when values or places &lt;= 2.</summary>
	static IDictionary<string, string>? Eliminate(IDictionary<string, string> values, string s, string d)
	{
		if (!values[s].Contains(d))
		{
			return values;
		}

		values[s] = values[s].Replace(d, "");
		if (values[s].Length == 0)
		{
			return null; //Contradiction: removed last value
		}
		else if (values[s].Length == 1)
		{
			//If there is only one value (d2) left in square, remove it from peers
			var d2 = values[s];
			if (!All(from s2 in peers[s] select Eliminate(values, s2, d2)))
			{
				return null;
			}
		}

		//Now check the places where d appears in the units of s
		foreach (var u in units[s])
		{
			var dplaces = from s2 in u where values[s2].Contains(d) select s2;
			switch (dplaces.Count())
			{
				case 0:
					return null;
				case 1:
					// d can only be in one place in unit; assign it there
					if (Assign(values, dplaces.First(), d) is null)
					{
						return null;
					}

					break;
			}
		}
		return values;
	}

	/*
	 * def all(seq):
	 *   for e in seq:
	 *     if not e: return False
	 *   return True
	 */
	static bool All<T>(IEnumerable<T> seq)
	{
		foreach (var e in seq)
		{
			if (e == null)
			{
				return false;
			}
		}
		return true;
	}

	/*
	 * def some(seq):
	 *   for e in seq:
	 *     if e: return e
	 *  return False
	 */
	static T Some<T>(IEnumerable<T> seq)
	{
		foreach (var e in seq)
		{
			if (e != null)
			{
				return e;
			}
		}
		return default!;
	}

	/*
	 * def center(s, width):
	 *   n = width - len(s)
	 *   if n <= 0: return s
	 *   half = n/2
	 *   if n%2 and width%2:
	 *     half = half+1
	 *   return ' '*half +  s + ' '*(n-half)
	 */
	static string Center(this string s, int width)
	{
		var n = width - s.Length;
		if (n <= 0)
		{
			return s;
		}

		var half = n / 2;

		if (n % 2 > 0 && width % 2 > 0)
		{
			half++;
		}

		return new string(' ', half) + s + new String(' ', n - half);
	}

	/*
	 * def printboard(values):
	 *   "Used for debugging."
	 *   width = 1+max(len(values[s]) for s in squares)
	 *   line = '\n' + '+'.join(['-'*(width*3)]*3)
	 *   for r in rows:
	 *     print ''.join(values[r+c].center(width)+(c in '36' and '|' or '')
	 *            for c in cols) + (r in 'CF' and line or '')
	 *   print
	 *   return values
	 */
	/// <summary>Used for debugging.</summary>
	public static IDictionary<string, string> PrintBoard(IDictionary<string, string> values)
	{
		if (values is null)
		{
			throw new ArgumentNullException(nameof(values));
		}

		var width = 1 + (from s in squares select values[s].Length).Max();
		var line = "\n" + String.Join("+", Enumerable.Repeat(new String('-', width * 3), 3).ToArray());

		foreach (var r in rows)
		{
			Console.WriteLine(string.Concat(
				(from c in cols
				 select values["" + r + c].Center(width) + ("36".Contains(c) ? "|" : "")).ToArray())
					+ ("CF".Contains(r) ? line : ""));
		}

		Console.WriteLine();
		return values;
	}
}