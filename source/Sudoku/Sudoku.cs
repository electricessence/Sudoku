using System.Text;

//https://sudoku.ironmonger.com/home/home.tpl <-- display puzzle difficulty! :)

namespace Sudoku;

public class Sudoku
{
	internal static readonly int MAX_COUNTERS = 11;

	#region Class Decelerations

	public int[,] _nums = new int[9, 9];
	private readonly int[,] _fixes = new int[9, 9];
	private readonly int[,] _errors = new int[9, 9];

	private readonly List<UndoData> _undos = new();

	private readonly HashSet<Pair>[,] _pairs = new HashSet<Pair>[9, 9];

	private readonly List<int>[,] _calc_candidates = new List<int>[9, 9];
	private readonly List<int>[,] _user_candidates = new List<int>[9, 9];
	private readonly List<int>[,] _user_guesses = new List<int>[9, 9];

	private bool _showcandidates = true;

	public bool ShowCandidates
	{
		get => _showcandidates;
		set
		{
			_showcandidates = value;
			if (!value && markPairs)
			{
				markPairs = false;
			}
		}
	}

	public bool RunFindSingles { get; internal set; }

	private int focused_number;
	private bool markPairs;

	// middle of the grid:
	private int hitx = 4;
	private int hity = 4;

	private float realw;
	private float realx;
	private float realy;

	public Action RequestRepaint = () => { };
	public Action<string> DisplayInfo = _ => { };
	public Action<string> DisplayError = _ => { };
	public Action<string[]> DisplayCounts = _ => { };
	private bool _skip_calc_cands;
	private bool _single_found;
	private bool _abort;

	public Sudoku()
	{
		_abort = false;

		for (int i = 0; i < 9; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				_calc_candidates[i, j] = new List<int>();
				_user_candidates[i, j] = new List<int>();
				_user_guesses[i, j] = new List<int>();
			}
		}

		InitPairs();
	}

	public void InitPairs()
	{
		for (int j = 0; j < 9; j++)
		{
			for (int i = 0; i < 9; i++)
			{
				var list = new HashSet<Pair>();

				for (int xy = 0; xy < 9; xy++)
				{
					list.Add(new Pair(j, xy, 0));
					list.Add(new Pair(xy, i, 1));
				}

				// BOX: j=row=y, i=col=x
				var y = j / 3;
				var x = i / 3;
				for (int m = 0; m < 3; m++)
				{
					for (int l = 0; l < 3; l++)
					{
						list.Add(new Pair(3 * y + m, 3 * x + l, 2));
					}
				}

				// remove pair for this particular cell
				for (int k = 0; k < 3; k++)
				{
					list.Remove(new Pair(j, i, k));
				}

				_pairs[j, i] = list;
			}
		}
	}

	internal void UndoEnterNumber()
	{
		_abort = false;
		RestoreState();
		CalculateCandidates();
		RequestRepaint();
	}

	#endregion

	private void ClearAllLists()
	{
		_abort = false;

		_undos.Clear();

		for (int i = 0; i < 9; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				_calc_candidates[j, i].Clear();
				_user_candidates[j, i].Clear();
				_user_guesses[j, i].Clear();
			}
		}
	}

	public void SetLocation(Point Location, bool isLeftButton)
	{
		int new_hitx = (int)Math.Floor((Location.X - realx) / realw * 9.0);

		int new_hity = (int)Math.Floor((Location.Y - realy) / realw * 9.0);

		var field_is_selected = !(new_hitx < 0 || new_hitx > 8
			|| new_hity < 0 || new_hity > 8);

		if (!field_is_selected)
			return;

		hitx = new_hitx;
		hity = new_hity;
		var value = _nums[hity, hitx];

		if (isLeftButton)
		{
			Enter1PossValueIntoEmptyField(value);
		}
		else
		{
			// Right mause button

			if (Control.ModifierKeys == Keys.Control)
			{
				HandleUserGuesses(focused_number);
				return;
			}

			if (Control.ModifierKeys == Keys.Alt)
			{
				HandleUserCandidates(focused_number);
				if (RunFindSingles)
				{
					FindAllSingles();
				}

				return;
			}

			MarkFieldsBy(value);

			EnterFocusedValueIntoEmptyField();
		}

		if (RunFindSingles)
		{
			FindAllSingles();
		}
	}

	private void Enter1PossValueIntoEmptyField(int value)
	{
		//enter only 1 remaining possible value into empty field
		if (value == 0) // current value is empty field
		{
			// only 1 candidate left                    
			if (_calc_candidates[hity, hitx].Count == 1)
			{
				EnterValueIntoField(_calc_candidates[hity, hitx][0]);
			}
		}
	}

	internal void MarkPairsCandidates()
	{
		focused_number = 0;
		markPairs = !markPairs;
		if (markPairs && !ShowCandidates)
		{
			ShowCandidates = true;
		}
	}

	private void FinderForSingle(int k, int j, int i)
	{
		var find = new List<int>();

		foreach (var p in _pairs[j, i])
		{
			if (p.Kind == k)
			{
				find.AddRange(_calc_candidates[p.J, p.I]);
			}
		}

		var diff = _calc_candidates[j, i]
			.Except(find.Distinct()).ToList();

		if (diff.Count == 1) // one number remain
		{
			_calc_candidates[j, i] = new List<int>(diff);
			_single_found = true;
		}
	}

	public void FindAllSingles()
	{
		if (_abort)
			return;

		if (ComputeErors() > 0)
			return;

		do
		{
			FindSingleNumbers();
		}
		while (_single_found);

		RequestRepaint();
	}

	internal void FindSingleNumbers()
	{
		_single_found = false;
		_abort = false;

		for (int j = 0; j < 9; j++)
		{
			for (int i = 0; i < 9; i++)
			{
				for (int k = 0; k < 3; k++)
				{
					FinderForSingle(k, j, i);
				}
			}
		}

		FillOnlyPossibleValues();
	}

	public void MarkAllPairsGuesses()
	{
		for (int j = 0; j < 3; j++)
		{
			for (int i = 0; i < 3; i++)
			{
				MarkPairGuessesInBox(j, i);
			}
		}

		RequestRepaint();
	}

	public void MarkPairGuessesInBox(int j, int i)
	{
		// create coordinates for box:
		var box = new List<Pair>();

		var y = 3 * j;
		var x = 3 * i;

		// first cell coordinate in box
		box.Add(new Pair(y, x));

		// add rest cell coordinates in box 
		foreach (var p in _pairs[y, x]
			.Where(m => m.Kind == 2))
		{
			box.Add(new Pair(p.J, p.I));
		}

		var dict = new Dictionary<int, int>();
		foreach (var p in box)
		{
			foreach (var c in _calc_candidates[p.J, p.I])
			{
				if (dict.TryGetValue(c, out var v))
				{
					dict[c] = v + 1;
				}
				else
				{
					dict.Add(c, 1);
				}
			}
		}

		// all candidates appear 2 times in box
		var values = dict.Where(k => k.Value == 2)
			.Select(v => v.Key);

		foreach (var p in box)
		{
			var cc = _calc_candidates[p.J, p.I];
			foreach (var v in values)
			{
				if (cc.Contains(v))
				{
					HandleUserGuesses(v, p.J, p.I);
				}
			}
		}
	}

	private void HandleUserGuesses(int value, int j, int i)
	{
		var gues = _user_guesses[j, i];

		// guesses must be part of possible (calculated) candidates
		if (!_calc_candidates[j, i].Contains(value))
			return;

		if (!gues.Contains(value))
		{
			SaveState(new UndoData(j, i, value, What.user_guess, false));
			gues.Add(value);
		}

		gues.Sort();
	}

	public void MarkFieldsBy(int value)
	{
		if (value > 0)
		{
			focused_number = value;
			markPairs = false;
		}
	}

	private void EnterFocusedValueIntoEmptyField()
	{
		if (focused_number > 0
			&& _fixes[hity, hitx] == 0
			&& _calc_candidates[hity, hitx].Contains(focused_number))
		{
			EnterValueIntoField(focused_number);
		}
	}

	private void Enter1PossValueIntoEmptyField(int j, int i)
	{
		var clc = _calc_candidates[j, i];

		if (clc.Count == 1)
		{
			var x = hitx;
			var y = hity;

			hitx = i;
			hity = j;

			// 0 - because field is empty
			Enter1PossValueIntoEmptyField(0);

			hitx = x;
			hity = y;
		}
	}

	internal void FillOnlyPossibleValues()
	{
		_skip_calc_cands = true;

		// when one field contain 1 red candidate
		// fill this empty field with this red candidate
		for (int i = 0; i < 9; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				Enter1PossValueIntoEmptyField(j, i);
			}
		}

		_skip_calc_cands = false;

		CalculateCandidates();
	}

	internal void LockNumbers()
	{
		_undos.Clear();
		HidePossibleFields();
		SetAllFixesByValues();
		RequestRepaint();
	}

	public void HidePossibleFields()
	{
		focused_number = 0;
		markPairs = false;
	}

	public void DeleteCurrentField()
	{
		_abort = false;

		if (_fixes[hity, hitx] == 1)
			return;

		SaveState(new UndoData(hity, hitx, _nums[hity, hitx]));

		_nums[hity, hitx] = 0;
		_errors[hity, hitx] = 0;

		CalculateCandidates();
	}

	public void KeyPress(Keys key)
	{
		DoSpecialKeys(key);

		TryToEnterNumberIntoField(key);

		ComputeErors();

		RequestRepaint();
	}

	private void DoSpecialKeys(Keys key)
	{
		switch (key)
		{
			case Keys.Enter:
				var value = _nums[hity, hitx];
				if (value == 0)
				{
					Enter1PossValueIntoEmptyField(value);
				}
				else
				{
					MarkFieldsBy(value);
					EnterFocusedValueIntoEmptyField();
				}

				break;

			case Keys.Escape:
				HidePossibleFields();
				break;

			case Keys.Delete:
			case Keys.D0:
			case Keys.NumPad0:
				DeleteCurrentField();
				break;

			case Keys.Back:
			case Keys.Z when Control.ModifierKeys == Keys.Control:
				UndoEnterNumber();
				break;

			case Keys.Up:
				MoveSelected(1);
				break;

			case Keys.Down:
				MoveSelected(2);
				break;

			case Keys.Right:
				MoveSelected(3);
				break;

			case Keys.Left:
				MoveSelected(4);
				break;
		}
	}

	private void TryToEnterNumberIntoField(Keys key)
	{
		var isOk = false;
		int value = -1;

		if (key is >= Keys.D1 and <= Keys.D9)
		{
			value = key - Keys.D0;
			isOk = true;
		}

		if (key is >= Keys.NumPad1 and <= Keys.NumPad9)
		{
			value = key - Keys.NumPad0;
			isOk = true;
		}

		if (!isOk)
			return;

		switch (Control.ModifierKeys)
		{
			case Keys.Control:
				HandleUserGuesses(value);
				break;
			case Keys.Alt:
				// user remove possible candidates 
				HandleUserCandidates(value);
				break;
			default:
				EnterValueIntoField(value);
				break;
		}

		if (RunFindSingles)
		{
			FindAllSingles();
		}
	}

	private void HandleUserCandidates(int value)
	{
		var usrcands = _user_candidates[hity, hitx];
		if (usrcands.Contains(value))
		{
			SaveState(new UndoData(hity, hitx, value, What.user_cand, true));
			usrcands.Remove(value);
		}
		else
		{
			SaveState(new UndoData(hity, hitx, value, What.user_cand, false));
			usrcands.Add(value);
		}

		RecalculateCandidates(hity, hitx, _nums[hity, hitx]);
	}

	private void HandleUserGuesses(int value)
	{
		var gues = _user_guesses[hity, hitx];

		// guesses must be part of possible (calculated) candidates
		if (!_calc_candidates[hity, hitx].Contains(value))
			return;

		if (gues.Contains(value))
		{
			SaveState(new UndoData(hity, hitx, value, What.user_guess, true));
			gues.Remove(value);
		}
		else
		{
			SaveState(new UndoData(hity, hitx, value, What.user_guess, false));
			gues.Add(value);
		}

		gues.Sort();
	}

	private void EnterValueIntoField(int value)
	{
		if (_fixes[hity, hitx] == 1)
			return;

		// before any changes, save current state
		SaveState(new UndoData(hity, hitx, _nums[hity, hitx]));

		_nums[hity, hitx] = value;

		CalculateCandidates();

		CheckPuzzleIsOk();
	}

	public void CheckPuzzleIsOk()
	{
		if (_abort)
			return;

		if (CalculateSolution() is null)
		{
			_single_found = false;
			ComputeErors();
			RequestRepaint();
			_abort = true;
			DisplayError("PUZZLE DOES NOT HAVE SOLUTION!");
			return;
		}

		if (IsSolved())
		{
			DisplayInfo("PUZZLE IS SOLVED!!!");
			focused_number = 0;
			markPairs = false;
		}
	}

	private void RestoreState()
	{
		if (_undos.Count == 0)
			return;

		var u = _undos[^1];
		this.hitx = u.x;
		this.hity = u.y;

		switch (u.what)
		{
			case What.number:
				_nums[u.y, u.x] = u.val;
				break;

			case What.user_guess:
				if (u.add)
				{
					_user_guesses[u.y, u.x].Add(u.val);
				}
				else
				{
					_user_guesses[u.y, u.x].Remove(u.val);
				}

				break;

			case What.user_cand:
				if (u.add)
				{
					_user_candidates[u.y, u.x].Add(u.val);
				}
				else
				{
					_user_candidates[u.y, u.x].Remove(u.val);
				}

				break;

			default:
				break;
		}

		_undos.RemoveAt(_undos.Count - 1);
	}

	private void SaveState(UndoData undoData) => _undos.Add(undoData);

	private void CalculateCandidates()
	{
		if (_skip_calc_cands)
			return;

		for (int i = 0; i < 9; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				RecalculateCandidates(j, i, _nums[j, i]);
			}
		}
	}

	private void RecalculateCandidates(int j, int i, int value)
	{
		if (value != 0)
		{
			_calc_candidates[j, i].Clear();
		}
		else
		{
			_calc_candidates[j, i] = Enumerable.Range(1, 9).ToList();

			foreach (var p in _pairs[j, i])
			{
				_calc_candidates[j, i].Remove(_nums[p.J, p.I]);
			}
		}

		RemoveUserCandsFromCalcCands(j, i);
	}

	private void RemoveUserCandsFromCalcCands(int j, int i)
	{
		// remove from calc candidates user candidates
		foreach (var item in _user_candidates[j, i])
		{
			_calc_candidates[j, i].Remove(item);
		}
	}

	#region LoadGameFileOrString

	internal void SaveAllData(string fileName)
	{
		// save nums, fixes, user_calc and guesses

		var str = new StringBuilder();
		str.AppendChars(GetGameString()).AppendLine();

		var fixstr = new List<char>();
		var calc = new List<char>();
		var gues = new List<char>();
		for (int i = 0; i < 9; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				fixstr.Add(_fixes[i, j]);

				foreach (var c in _user_candidates[i, j])
				{
					calc.Add(c);
				}

				calc.Add(',');

				foreach (var c in _user_guesses[i, j])
				{
					gues.Add(c);
				}

				gues.Add('|');
			}

			calc.AddRange(Environment.NewLine);
			gues.AddRange(Environment.NewLine);
		}

		str.AppendChars(fixstr).AppendLine();
		str.AppendChars(calc).AppendLine();
		str.AppendChars(gues).AppendLine();

		foreach (var un in _undos)
		{
			str.AppendLine(un.ToString());
		}

		File.WriteAllText(fileName, str.ToString());
	}

	internal void LoadAllData(string fileName)
	{
		// load nums, fixes, user_calc and guesses

		var text = File.ReadAllText(fileName);

		var lines = text.Split(
			new string[] { Environment.NewLine },
			StringSplitOptions.RemoveEmptyEntries);

		//this method clear all lists + undos list
		SetGameString(lines[0]);

		if (lines.Length == 1)
			return;

		for (int n = 0; n < 81; n++)
		{
			int i = n / 9;
			int j = n % 9;
			_fixes[i, j] = lines[1][n] - '0';
		}

		for (int i = 0; i < 9; i++)
		{
			var calcstr = lines[i + 2].Split(',');
			var guesstr = lines[i + 11].Split('|');

			for (int j = 0; j < 9; j++)
			{
				foreach (char c in calcstr[j])
				{
					_user_candidates[i, j].Add(c - '0');
				}

				foreach (char c in guesstr[j])
				{
					_user_guesses[i, j].Add(c - '0');
				}

				RemoveUserCandsFromCalcCands(i, j);
			}
		}

		for (int i = 20; i < lines.Length; i++)
		{
			_undos.Add(new UndoData(lines[i]));
		}
	}

	public IEnumerable<char> GetGameString()
	{
		for (int i = 0; i < 9; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				var c = (char)(_nums[i, j] + '0');
				if (c == '0') c = '.';
				yield return c;
			}
		}
	}

	public bool SetGameString(ReadOnlySpan<char> game)
	{
		ClearAllLists();

		HidePossibleFields();
		FillGameBoardFromLine(game);

		ComputeErors();
		CalculateCandidates();

		return true;
	}

	public void FillGameBoardFromLine(ReadOnlySpan<char> game)
	{
		if (game.Length != 81)
		{
			MessageBox.Show($"Input game string should have length = 81, but was {game.Length}!");
			return;
		}

		for (int n = 0; n < 81; n++)
		{
			char c = game[n];
			if (c is (< '1' or > '9') and not '.')
			{
				throw new Exception("Input game string should have values 0-9 and . only!");
			}
		}

		for (int n = 0; n < 81; n++)
		{
			var c = game[n];
			int i = n / 9;
			int j = n % 9;
			_nums[i, j] = c == '.' ? 0 : (c - '0');
		}

		SetAllFixesByValues();
	}

	static readonly ReadOnlyMemory<Color> NumberColors = new Color[] {
		Color.White,		// 0
		Color.DarkRed,		// 1
		Color.Orange,		// 2
		Color.Yellow,		// 3
		Color.LightGreen,	// 4
		Color.DarkGreen,	// 5
		Color.LightBlue,	// 6
		Color.DarkBlue,		// 7
		Color.Violet,		// 8
		Color.DarkViolet	// 9
	};

	public void Draw(Graphics G, float angle)
	{
		Color Background = Color.DarkKhaki;
		Brush BG1 = new Pen(Color.FloralWhite).Brush;
		Brush BG2 = new Pen(Color.LightGoldenrodYellow).Brush;
		Brush BG;
		Brush Selected = new SolidBrush(Color.FromArgb(64, Color.RoyalBlue));
		Brush FontColor1 = Brushes.Black;
		Brush FontColor2 = Brushes.RoyalBlue;
		//Brush FontColor3 = Brushes.Crimson;
		Brush FontColor = FontColor1;
		Pen Error = new(Color.Red, 3);
		Pen HintCircle = new(Color.Blue, 3);
		Brush HintBrush = new Pen(Color.LightPink).Brush;
		Brush PairBrush = new Pen(Color.LightSkyBlue).Brush;
		SolidBrush SmallFont_BlackBrush = new(Color.FromArgb(200, Color.SlateGray));
		SolidBrush SmallFont_1Calc_RedBrush = new(Color.FromArgb(200, Color.Red));
		SolidBrush SmallFont_Guess_BlueBrush = new(Color.FromArgb(200, Color.RoyalBlue));
		G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
		G.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
		G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

		G.Clear(Background);
		Pen Border1 = new(Color.Black, 3);
		Pen Border2 = new(Color.Black, 1);
		float w = G.VisibleClipBounds.Width;
		float h = G.VisibleClipBounds.Height;
		float min = Math.Min(w, h);
		float centre_x = w / 2;
		float centre_y = h / 2;
		float startx = centre_x - min / 2;
		float starty = centre_y - min / 2;

		const float m = 0; // how much top of su. grid is bellow window top
		realx = startx + m;
		realy = starty + m;
		realw = min - 2 * m;

		if (realw <= 0)
			return;

		var realw9 = realw / 9;
		float error_circle_m = realw9 * 0.95f;
		Font F = new("Arial", realw / 18);
		//Font Fsmall = new Font("Arial", realw / 72, FontStyle.Bold);
		Font FThird = new("Arial", realw / 38, FontStyle.Bold);

		float SizeThirdDiffX = (realw9 / 3 - G.MeasureString("8", FThird).Width) / 2;

		G.TranslateTransform(centre_x, centre_y);
		G.RotateTransform(angle);
		G.TranslateTransform(-centre_x, -centre_y);
		G.FillRectangle(Brushes.White, realx, realy, realw, realw);
		G.DrawRectangle(Border1, realx, realy, realw, realw);

		var third_x = new float[3];
		var third_y = new float[3];
		for (float i = 0; i < 3; i++)
		{
			for (float j = 0; j < 3; j++)
			{
				var ri3 = realx + realw * i / 3;
				var rj3 = realy + realw * j / 3;
				G.DrawRectangle(Border1, ri3, rj3, realw / 3, realw / 3);
				BG = (i + j) % 2 == 0 ? BG1 : BG2;
				G.FillRectangle(BG, ri3, rj3, realw / 3, realw / 3);

				for (float i2 = 0; i2 < 3; i2++)
				{
					for (float j2 = 0; j2 < 3; j2++)
					{
						//if (i * 3 + i2 == hitx && j * 3 + j2 == hity) 
						//    G.FillRectangle(Selected, ci, cj, realw9, realw9);

						float ci = realx + realw * (i / 3 + i2 / 9);
						float cj = realy + realw * (j / 3 + j2 / 9);
						G.DrawRectangle(Border2, ci, cj, realw9, realw9);

						int index_i = Convert.ToInt32(i * 3 + i2);
						int index_j = Convert.ToInt32(j * 3 + j2);

						if (focused_number > 0)
						{
							if (!_calc_candidates[index_j, index_i].Contains(focused_number))
							{
								G.FillRectangle(HintBrush, ci, cj, realw9, realw9);
							}
						}
						else
						{
							if (markPairs)
							{
								if (_calc_candidates[index_j, index_i].Count != 2)
								{
									G.FillRectangle(PairBrush, ci, cj, realw9, realw9);
								}
							}
						}

						int numValue = _nums[index_j, index_i];
						string num = numValue == 0 ? "" : numValue.ToString();

						if(numValue != 0)
						{
							var numColor = NumberColors.Span[numValue];
							var brush = new SolidBrush(numColor);
							G.FillEllipse(brush, ci, cj, realw9, realw9);
						}

						if (_fixes[index_j, index_i] == 1)
						{
							FontColor = FontColor1;
						}

						if (_fixes[index_j, index_i] == 0)
						{
							FontColor = FontColor2;
						}

						SizeF size_num = G.MeasureString(num, F);

						float num_x = ci + (realw9 - size_num.Width) / 2;
						float num_y = cj + (realw9 - size_num.Height) / 2;

						for (int sc = 0; sc < 3; sc++)
						{
							third_x[sc] = SizeThirdDiffX + ci + sc * (realw9 / 3);
							third_y[sc] = cj + sc * (realw9 / 3);
						}

						// put candidate numbers to match with numerical keyboard:
						(third_y[2], third_y[0]) = (third_y[0], third_y[2]);

						// Rectangles: x for number, y for hints and z for error circle

						RectangleF x = new(num_x, num_y, size_num.Width, size_num.Height);
						//RectangleF y = new RectangleF(ci, cj, realw9, realw9);
						RectangleF z = new(ci + error_circle_m, cj + error_circle_m,
							realw9 - 2 * error_circle_m, realw9 - 2 * error_circle_m);

						float num_centre_x = num_x + size_num.Width / 2;
						float num_centre_y = num_y + size_num.Height / 2;
						G.TranslateTransform(num_centre_x, num_centre_y);
						G.RotateTransform(-angle);
						G.TranslateTransform(-num_centre_x, -num_centre_y);
						G.DrawString(num, F, FontColor, x);
						G.TranslateTransform(num_centre_x, num_centre_y);
						G.RotateTransform(+angle);
						G.TranslateTransform(-num_centre_x, -num_centre_y);

						for (int yo = 0; yo < 3; yo++)
						{
							for (int xo = 0; xo < 3; xo++)
							{
								var value = 3 * yo + xo + 1; // 1-9
								SolidBrush smalfontbrush = SmallFont_BlackBrush;

								var clc = _calc_candidates[index_j, index_i];

								var clchaveval = clc.Contains(value);
								bool shownumber = ShowCandidates && clchaveval;

								if (clchaveval &&
									_user_guesses[index_j, index_i].Contains(value))
								{
									shownumber = true;
									smalfontbrush = SmallFont_Guess_BlueBrush;
								}

								if (clc.Count == 1)
								{
									smalfontbrush = SmallFont_1Calc_RedBrush;
								}

								if (shownumber)
								{
									RectangleF o = new(third_x[xo],
										third_y[yo], realw9 / 3, realw9 / 3);
									G.DrawString(value.ToString(),
											FThird, smalfontbrush, o);
								}
							}
						}

						// draw selected field
						if (index_i == hitx && index_j == hity)
						{
							G.FillRectangle(Selected, ci, cj, realw9, realw9);
						}

						if (focused_number > 0 && _nums[index_j, index_i] == focused_number)
						{
							G.DrawEllipse(HintCircle, z);
						}

						if (_errors[index_j, index_i] == 1)
						{
							G.DrawEllipse(Error, z);
						}
					}
				}
			}
		}
	}

	public bool LoadFile(string file)
	{
		string? content;
		using (StreamReader R = new(file))
			content = R.ReadLine();

		return content?.Length == 81
			&& SetGameString(content);
	}

	public bool SaveFile(string file)
	{
		using StreamWriter W = new(file);
		W.WriteLine(GetGameString());

		return true;
	}

	#endregion LoadGameFileOrString

	#region errors
	public int ComputeErors()
	{
		ClearAllErrors();

		for (int j = 0; j < 9; j++)
		{
			for (int i = 0; i < 9; i++)
			{
				FindErrors(j, i);
			}
		}

		var sum = 0;
		for (int j = 0; j < 9; j++)
		{
			for (int i = 0; i < 9; i++)
			{
				sum += _errors[j, i]; // e[j,i]=0 <- no error
			}
		}

		return sum;
	}

	private void FindErrors(int j, int i)
	{
		var num = _nums[j, i];
		if (num == 0)
			return;

		foreach (var p in _pairs[j, i])
		{
			if (num == _nums[p.J, p.I])
			{
				_errors[j, i] = 1;
			}
		}
	}

	private void ClearAllErrors()
	{
		for (int i = 0; i < 9; i++)
		{
			for (int k = 0; k < 9; k++)
			{
				_errors[i, k] = 0;
			}
		}
	}

	#endregion errors

	//----------------------------------------------------

	#region puzzle_generator

	private IDictionary<string, string>? CalculateSolution()
		=> Solver.Search(Solver.ParseGrid(GetGameString()));

	public void SolvePuzzle()
	{
		if (ComputeErors() != 0)
		{
			DisplayError("Fix errors in table!");
			return;
		}

		var solution = CalculateSolution();
		if (solution is null)
		{
			DisplayError("This sudoku does not have solution :(");
			return;
		}
		else
		{
			DialogResult answer = MessageBox.Show(
				"Puzzle have solution!\nApply solution?",
				  "Sudoku", MessageBoxButtons.YesNo);
			if (answer == DialogResult.No)
				return;
		}

		var res = "";
		foreach (var item in solution)
		{
			res += item.Value;
		}

		FillGameBoardFromLine(res);

		ClearAllLists();

		RequestRepaint();
	}

	public bool IsSolved()
	{
		if (!IsFull())
			return false;

		return ComputeErors() == 0;
	}

	private bool IsFull()
	{
		for (int i = 0; i < 9; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				if (_nums[j, i] == 0)
					return false;
			}
		}

		return true;
	}

	public void GenerateGame()
	{
		var puzz = new Puzzler(65)
			.MakePuzzle(GenerateBoard())
			.ToArray();

		SetGameString(puzz);
		LockNumbers();

		if (RunFindSingles)
		{
			FindAllSingles();
		}
	}

	private static char[] GenerateBoard()
	{
		var fill = Generator2.GenNew().ToArray();
		if (fill.Length == 0)
			fill = Generator.GenNew().ToArray();
		return fill;
	}

	private void SetAllFixesByValues()
	{
		for (int i = 0; i < 9; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				_fixes[i, j] = _nums[i, j] == 0 ? 0 : 1;
			}
		}
	}

	internal void MoveSelected(int move)
	{
		if (move == 2 && hity + 1 < 9)
		{
			hity++;
		}

		if (move == 1 && hity - 1 >= 0)
		{
			hity--;
		}

		if (move == 3 && hitx + 1 < 9)
		{
			hitx++;
		}

		if (move == 4 && hitx - 1 >= 0)
		{
			hitx--;
		}

		RequestRepaint();
	}

	#endregion
}