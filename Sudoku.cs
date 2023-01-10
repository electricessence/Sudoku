using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

//https://sudoku.ironmonger.com/home/home.tpl <-- display puzzle difficulty! :)

namespace Sudoku
{
    public class Sudoku
    {
        internal static readonly int MAX_COUNTERS = 11;

        #region Class Decelerations

        public int[,] nums = new int[9, 9];                    
        private int[,] fixes = new int[9, 9];
        private int[,] errors = new int[9, 9];

        private List<UndoData> undos = new List<UndoData>();

        private HashSet<Pair>[,] pairs = new HashSet<Pair>[9, 9];

        private List<int>[,] calc_candidates = new List<int>[9, 9];
		private List<int>[,] user_candidates = new List<int>[9, 9];
        private List<int>[,] user_guesses = new List<int>[9, 9];


        private bool showcandidates = true;

        public bool ShowCandidates
        {
            get { return showcandidates; }
            set { 
                showcandidates = value;
                if (!value && markPairs)
                    markPairs = false;
            }
        }

        public bool RunFindSingles { get; internal set; }

        private int focused_number = 0;
        private bool markPairs = false;

        // middle of the grid:
        private int hitx = 4;
        private int hity = 4;

        private float realw;
        private float realx;
        private float realy;        

        public Action RequestRepaint = () => { };
        public Action<string> DisplayInfo = (s) => { };
        public Action<string> DisplayError = (s) => { };
        public Action<string[]> DisplayCounts = (s) => { };
        private bool skip_calc_cands = false;
        private bool single_found = false;
        private bool abort = false;

        public Sudoku()
        {
            abort = false;

            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                {
                    calc_candidates[i, j] = new List<int>();
                    user_candidates[i, j] = new List<int>();
                    user_guesses[i, j] = new List<int>();
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
                        for (int l = 0; l < 3; l++)
                            list.Add(new Pair(3 * y + m, 3 * x + l, 2));

                    // remove pair for this particular cell
                    for (int k = 0; k < 3; k++)
                        list.Remove(new Pair(j, i, k));

                    pairs[j, i] = list;
                }
            }
        }



        internal void UndoEnterNumber()
        {
            abort = false;
            RestoreState();
            CalculateCandidates();            
            RequestRepaint();
        }

        #endregion                

        private void ClearAllLists()
        {
            abort = false;

            undos.Clear();

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    calc_candidates[j, i].Clear();
                    user_candidates[j, i].Clear();
                    user_guesses[j, i].Clear();
                }
            }
        }

        public void SetLocation(Point Location, bool isLeftButton)
        {
            int new_hitx = (int)Math.Floor((Location.X - realx) / realw * 9.0);

            int new_hity = (int)Math.Floor((Location.Y - realy) / realw * 9.0);

            var field_is_selected = !((new_hitx < 0 || new_hitx > 8
                || new_hity < 0 || new_hity > 8));

            if (!field_is_selected)
                return;

            hitx = new_hitx;
            hity = new_hity;
            var value = nums[hity, hitx];

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
                        FindAllSingles();
                    return;
                }

                MarkFieldsBy(value);

                EnterFocusedValueIntoEmptyField();
            }
            
            if (RunFindSingles)
                FindAllSingles();
        }

        private void Enter1PossValueIntoEmptyField(int value)
        {
            //enter only 1 remaining possible value into empty field
            if (value == 0) // current value is empty field
            {
                // only 1 candidate left                    
                if (calc_candidates[hity, hitx].Count == 1)                
                    enter_value_into_field(calc_candidates[hity, hitx][0]);                
            }
        }

        internal void MarkPairsCandidates()
        {
            focused_number = 0;
            markPairs = !markPairs;
            if (markPairs && !ShowCandidates)
                ShowCandidates = true;
        }        

        private void FinderForSingle(int k, int j, int i)
        { 
            var find = new List<int>();

            foreach (var p in pairs[j, i])
                if (p.kind == k)
                    find.AddRange(calc_candidates[p.j, p.i]);

            var diff = calc_candidates[j, i]
                .Except(find.Distinct()).ToList();

            if (diff.Count == 1) // one number remain
            {
                calc_candidates[j, i] = new List<int>(diff);
                single_found = true;
            }
        }

        public void FindAllSingles()
        {
            if (abort) return;

            if (ComputeErors() > 0)
                return;

            do  
                FindSingleNumbers();            
            while (single_found);                
            
            RequestRepaint();
        }

        internal void FindSingleNumbers()
        {
            single_found = false;
            abort = false;

            for (int j = 0; j < 9; j++)
                for (int i = 0; i < 9; i++)
                    for (int k = 0; k < 3; k++)
                       FinderForSingle(k, j, i);                  
            

            FillOnlyPossibleValues();
        }

        public void MarkAllPairsGuesses()
        {
            for (int j = 0; j < 3; j++)
                for (int i = 0; i < 3; i++)
                    MarkPairGuessesInBox(j, i);

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
            foreach (var p in (pairs[y, x])
                .Where(m => m.kind == 2))
                box.Add(new Pair(p.j, p.i));

            var dict = new Dictionary<int, int>();
            foreach (var p in box)
            {
                foreach (var c in calc_candidates[p.j, p.i])
                {
                    if (dict.ContainsKey(c))
                        dict[c]++;
                    else
                        dict.Add(c, 1);
                }
            }

            // all candidates appear 2 times in box
            var values = dict.Where(k => k.Value == 2)
                .Select(v => v.Key);            

            foreach (var p in box)
            {
                var cc = calc_candidates[p.j, p.i];
                foreach (var v in values)
                    if (cc.Contains(v))
                        HandleUserGuesses(v, p.j, p.i);
            }
        }

        private void HandleUserGuesses(int value, int j, int i)
        {
            var gues = user_guesses[j, i];

            // guesses must be part of possible (calculated) candidates
            if (!calc_candidates[j,i].Contains(value))
                return;

            if (!gues.Contains(value))
            {
                SaveState(new UndoData(j,i, value, What.user_guess, false));
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
            if ((focused_number > 0)
                && (fixes[hity, hitx] == 0)
                && (calc_candidates[hity, hitx].Contains(focused_number)))
            {
                enter_value_into_field(focused_number);
            }
        }

        private void Enter1PossValueIntoEmptyField(int j, int i)
        {
            var clc = calc_candidates[j, i];

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
            skip_calc_cands = true;

            // when one field contain 1 red candidate
            // fill this empty field with this red candidate
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)                
                    Enter1PossValueIntoEmptyField(j, i);                

            skip_calc_cands = false;

            CalculateCandidates();
        }

        internal void LockNumbers()
        {
            undos.Clear();
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
            abort = false;

            if (fixes[hity, hitx] == 1)
                return;

            SaveState(new UndoData(hity, hitx, nums[hity, hitx]));

            nums[hity, hitx] = 0;
            errors[hity, hitx] = 0;

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
            if (key == Keys.Enter)
            {
                var value = nums[hity, hitx];
                if (value == 0)
                    Enter1PossValueIntoEmptyField(value);
                else
                {
                    MarkFieldsBy(value);
                    EnterFocusedValueIntoEmptyField();
                }
            }

            if (key == Keys.Escape) HidePossibleFields();

            if (key == Keys.Delete
                || key == Keys.D0
                || key == Keys.NumPad0)
            {
                DeleteCurrentField();
            }

            if (key == Keys.Back ||
                (Control.ModifierKeys == Keys.Control && key == Keys.Z))
            {
                UndoEnterNumber();
            }

            if (key == Keys.Up) MoveSelected(1);
            if (key == Keys.Down) MoveSelected(2);
            if (key == Keys.Right) MoveSelected(3);
            if (key == Keys.Left) MoveSelected(4);
        }

        private void TryToEnterNumberIntoField(Keys key)
        {
            var isOk = false;
            int value = -1;

            if (key >= Keys.D1 && key <= Keys.D9)
            {
                value = key - Keys.D0;
                isOk = true;
            }

            if (key >= Keys.NumPad1 && key <= Keys.NumPad9)
            {
                value = key - Keys.NumPad0;
                isOk = true;
            }

            if (isOk)
            {
                if (Control.ModifierKeys == Keys.Control)
                {
                    HandleUserGuesses(value);                    
                }
                else if (Control.ModifierKeys == Keys.Alt)
                {
                    // user remove possible candidates 
                    HandleUserCandidates(value);                    
                } else 
                    enter_value_into_field(value);

                if (RunFindSingles)
                    FindAllSingles();
            }
        }
        
        private void HandleUserCandidates(int value)
        {
            var usrcands = user_candidates[hity, hitx];
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

            recalculate_candidates(hity, hitx, nums[hity, hitx]);

        }

        private void HandleUserGuesses(int value)
        {
            var gues = user_guesses[hity, hitx];

            // guesses must be part of possible (calculated) candidates
            if (!calc_candidates[hity, hitx].Contains(value))
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

        private void enter_value_into_field(int value)
        {
            if (fixes[hity, hitx] == 1)
                return;

            // before any changes, save current state
            SaveState(new UndoData(hity, hitx, nums[hity, hitx]));

            nums[hity, hitx] = value;

            CalculateCandidates();
                
            CheckPuzzleIsOk();
        }

        public void CheckPuzzleIsOk()
        {
            if (abort) return;

            if (CalculateSolution() == null)
            {
                single_found = false;
                ComputeErors();
                RequestRepaint();
                abort = true;
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
            if (undos.Count == 0)
                return;

            var u = undos[undos.Count - 1];
            this.hitx = u.x;
            this.hity = u.y;

            switch (u.what)
            {
                case What.number: nums[u.y, u.x] = u.val; break;

                case What.user_guess:
                    if (u.add)
                        user_guesses[u.y, u.x].Add(u.val);
                    else
                        user_guesses[u.y, u.x].Remove(u.val);
                    break;

                case What.user_cand:
                    if (u.add)
                        user_candidates[u.y, u.x].Add(u.val);
                    else
                        user_candidates[u.y, u.x].Remove(u.val);
                    break;

                default: break;
            }

            undos.RemoveAt(undos.Count - 1);
        }

        private void SaveState(UndoData undoData)
        {
            undos.Add(undoData);
        }

        private void CalculateCandidates()
        {
            if (skip_calc_cands)
                return;

            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                   recalculate_candidates(j, i, nums[j, i]);                                  
        }

        private void recalculate_candidates(int j, int i, int value)
        {
            if (value != 0)
            {
                calc_candidates[j, i].Clear();
            }
            else
            {
                calc_candidates[j, i] = Enumerable.Range(1, 9).ToList();

                foreach (var p in pairs[j, i])
                    calc_candidates[j, i].Remove(nums[p.j, p.i]);
            }

            RemoveUserCandsFromCalcCands(j, i);
        }

        private void RemoveUserCandsFromCalcCands(int j, int i)
        {
            // remove from calc candidates user candidates
            foreach (var item in user_candidates[j, i])
                calc_candidates[j, i].Remove(item);
        }

        #region LoadGameFileOrString

        internal void SaveAllData(string fileName)
        {
            // save nums, fixes, user_calc and guesses

            var str = new StringBuilder();
            str.AppendLine(GetGameString());

            var fixstr = "";
            var calc = "";
            var gues = "";
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    fixstr += fixes[i, j].ToString();

                    foreach (var c in user_candidates[i, j])
                        calc += c.ToString();
                    calc += ',';

                    foreach (var c in user_guesses[i, j])
                        gues += c.ToString();
                    gues += '|';
                }

                calc += Environment.NewLine;
                gues += Environment.NewLine;
            }

            str.AppendLine(fixstr);
            str.AppendLine(calc);
            str.AppendLine(gues);

            foreach (var un in undos)
                str.AppendLine(un.ToString());

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

            if (lines.Count() == 1)
                return;

            for (int n = 0; n < 81; n++)
            {
                string c = lines[1].Substring(n, 1);
                int i = n / 9;
                int j = n % 9;
                fixes[i, j] = Convert.ToInt32(c);
            }

            for (int i = 0; i < 9; i++)
            {
                var calcstr = lines[i + 2].Split(',');
                var guesstr = lines[i + 11].Split('|');

                for (int j = 0; j < 9; j++)
                {
                    foreach (char c in calcstr[j])
                        user_candidates[i, j].Add(
                            Convert.ToInt32(c.ToString()));

                    foreach (char c in guesstr[j])
                        user_guesses[i, j].Add(
                            Convert.ToInt32(c.ToString()));

                    RemoveUserCandsFromCalcCands(i, j);
                }
            }

            for (int i = 20; i < lines.Count(); i++)
                undos.Add(new UndoData(lines[i]));
        }

        public string GetGameString()
        {
            string res = "";

            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    res += nums[i, j].ToString();

            res = res.Replace("0", ".");

            return res;
        }

        public bool SetGameString(string game)
        {            
            ClearAllLists();

            HidePossibleFields();
            FillGameBoardFromLine(game);

            ComputeErors();
            CalculateCandidates();                          

            return true;
        }

        public void FillGameBoardFromLine(string game)
        {
            if (game.Length != 81)
            {
                MessageBox.Show($"Input game string should have length = 81, but was {game.Length}!");
                return;
            }

            for (int n = 0; n < 81; n++)
            {
                char c = game[n];
                if ((c < '1' || c > '9') && c != '.')
                    throw new Exception($"Input game string should have values 0-9 and . only!");
            }

            for (int n = 0; n < 81; n++)
            {
                string c = game.Substring(n, 1);
                int i = n / 9;
                int j = n % 9;
                if (c == ".")                
                    nums[i, j] = 0;
                else                
                    nums[i, j] = Convert.ToInt32(c);
            }

            SetAllFixesByValues();
        }

        public void Draw(Graphics G, float angle)
        {
            Color Background = Color.DarkKhaki;
            Brush BG1 = new Pen(Color.FloralWhite).Brush;
            Brush BG2 = new Pen(Color.LightGoldenrodYellow).Brush;
            Brush BG;
            Brush Selected = new SolidBrush(Color.FromArgb(64, Color.RoyalBlue));
            Brush FontColor1 = Brushes.Black;
            Brush FontColor2 = Brushes.RoyalBlue;
            Brush FontColor3 = Brushes.Crimson;
            Brush FontColor = FontColor1;
            Pen Error = new Pen(Color.Red, 3);
            Pen HintCircle = new Pen(Color.Blue, 3);
            Brush HintBrush = new Pen(Color.LightPink).Brush;
            Brush PairBrush = new Pen(Color.LightSkyBlue).Brush;
            SolidBrush SmallFont_BlackBrush = new SolidBrush(Color.FromArgb(200, Color.SlateGray));
            SolidBrush SmallFont_1Calc_RedBrush = new SolidBrush(Color.FromArgb(200, Color.Red));
            SolidBrush SmallFont_Guess_BlueBrush = new SolidBrush(Color.FromArgb(200, Color.RoyalBlue));
            G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            G.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            G.Clear(Background);
            Pen Border1 = new Pen(Color.Black, 3);
            Pen Border2 = new Pen(Color.Black, 1);
            float w = G.VisibleClipBounds.Width;
            float h = G.VisibleClipBounds.Height;
            float min = Math.Min(w, h);
            float centre_x = w / 2;
            float centre_y = h / 2;
            float startx = centre_x - min / 2;
            float starty = centre_y - min / 2;

            float m = 0; // how much top of su. grid is bellow window top
            realx = startx + m;
            realy = starty + m;
            realw = min - 2 * m;

            if (realw <= 0) return;

            var realw9 = realw / 9;
            float error_circle_m = realw9 * 0.95f;
            Font F = new Font("Arial", realw / 18);
            //Font Fsmall = new Font("Arial", realw / 72, FontStyle.Bold);
            Font FThird = new Font("Arial", realw / 38, FontStyle.Bold);

            float SizeThirdDiffX = ((realw9 / 3) - G.MeasureString("8", FThird).Width) / 2;

            G.TranslateTransform(centre_x, centre_y);
            G.RotateTransform(angle);
            G.TranslateTransform(-centre_x, -centre_y);
            G.FillRectangle(Brushes.White, realx, realy, realw, realw);
            G.DrawRectangle(Border1, realx, realy, realw, realw);

            float temp = 0;
            var third_x = new float[3];
            var third_y = new float[3];

            float ci = 0;
            float cj = 0;

            for (float i = 0; i < 3; i++)
            {
                for (float j = 0; j < 3; j++)
                {
                    var ri3 = realx + realw * i / 3;
                    var rj3 = realy + realw * j / 3;
                    G.DrawRectangle(Border1, ri3, rj3, realw / 3, realw / 3);
                    if ((i + j) % 2 == 0) BG = BG1; else BG = BG2;
                    G.FillRectangle(BG, ri3, rj3, realw / 3, realw / 3);

                    for (float i2 = 0; i2 < 3; i2++)
                    {
                        for (float j2 = 0; j2 < 3; j2++)
                        {
                            //if (i * 3 + i2 == hitx && j * 3 + j2 == hity) 
                            //    G.FillRectangle(Selected, ci, cj, realw9, realw9);

                            ci = realx + realw * (i / 3 + i2 / 9);
                            cj = realy + realw * (j / 3 + j2 / 9);

                            G.DrawRectangle(Border2, ci, cj, realw9, realw9);

                            int index_i = Convert.ToInt32(i * 3 + i2);
                            int index_j = Convert.ToInt32(j * 3 + j2);

                            if (focused_number > 0)
                            {
                                if (!calc_candidates[index_j, index_i].Contains(focused_number))
                                    G.FillRectangle(HintBrush, ci, cj, realw9, realw9);
                            }
                            else
                            {
                                if (markPairs)
                                {                                    
                                    if (calc_candidates[index_j, index_i].Count() != 2)
                                        G.FillRectangle(PairBrush, ci, cj, realw9, realw9);
                                }
                            }

                            string num = nums[index_j, index_i] == 0 ? "" : nums[index_j, index_i].ToString();                            

                            if (fixes[index_j, index_i] == 1) FontColor = FontColor1;
                            if (fixes[index_j, index_i] == 0) FontColor = FontColor2;

                            SizeF size_num = G.MeasureString(num, F);

                            float num_x = ci + (realw9 - size_num.Width) / 2;
                            float num_y = cj + (realw9 - size_num.Height) / 2;
                            
                            for (int sc = 0; sc < 3; sc++)
                            {
                                third_x[sc] = SizeThirdDiffX + ci + sc*(realw9 / 3);
                                third_y[sc] = cj + sc*(realw9 / 3);
                            }

                            // put candidate numbers to match with numerical keyboard:
                            temp = third_y[0];
                            third_y[0] = third_y[2];
                            third_y[2] = temp;

                            // Rectangles: x for number, y for hints and z for error circle

                            RectangleF x = new RectangleF(num_x, num_y, size_num.Width, size_num.Height);
                            //RectangleF y = new RectangleF(ci, cj, realw9, realw9);
                            RectangleF z = new RectangleF(ci + error_circle_m, cj + error_circle_m, 
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


                            for (int yo=0; yo<3; yo++)
                            {
                                for(int xo=0; xo<3; xo++)
                                {
                                    var shownumber = false;
                                    var value = 3 * yo + xo + 1; // 1-9
                                    SolidBrush smalfontbrush = SmallFont_BlackBrush;
                                                                        
                                    var clc = calc_candidates[index_j, index_i];

                                    var clchaveval = clc.Contains(value);                                    
                                    shownumber = ShowCandidates && clchaveval;

                                    if (clchaveval &&  
                                        user_guesses[index_j, index_i].Contains(value))
                                    {
                                        shownumber = true;
                                        smalfontbrush = SmallFont_Guess_BlueBrush;
                                    }

                                    if (clc.Count == 1)
                                        smalfontbrush = SmallFont_1Calc_RedBrush;

                                    if (shownumber)
                                    {
                                        RectangleF o = new RectangleF(third_x[xo], 
                                            third_y[yo], realw9 / 3, realw9 / 3);
                                        G.DrawString(value.ToString(),
                                                FThird, smalfontbrush, o);
                                    }                                    
                                }
                            }   
                                    
                            // draw selected field
                            if (index_i == hitx && index_j == hity)
                                G.FillRectangle(Selected, ci, cj, realw9, realw9);
                            
                            if (focused_number > 0 && nums[index_j, index_i] == focused_number)
                                G.DrawEllipse(HintCircle, z);

                            if (errors[index_j, index_i] == 1) G.DrawEllipse(Error, z);
                        }
                    }
                }
            }
        }


        public bool LoadFile(string file)
        {
            StreamReader R = new StreamReader(file);
            string content = R.ReadLine();
            R.Close();
            if (content.Length != 81) return false;
            return SetGameString(content);
        }

        public bool SaveFile(string file)
        {
            StreamWriter W = new StreamWriter(file);
            W.WriteLine(GetGameString());
            W.Close();
            return true;
        }

        #endregion LoadGameFileOrString


        #region errors
        public int ComputeErors()
        {
            ClearAllErrors();
                        
            for (int j = 0; j < 9; j++)
                for (int i = 0; i < 9; i++)                    
                    FindErrors(j, i);
            
            var sum = 0;
            for (int j = 0; j < 9; j++)
                for (int i = 0; i < 9; i++)
                    sum += errors[j, i]; // e[j,i]=0 <- no error

            return sum;
        }

        private void FindErrors(int j, int i)
        {
            var num = nums[j, i];
            if (num == 0) return;

            foreach (var p in pairs[j, i])
                if (num == nums[p.j, p.i])
                    errors[j,i] = 1;
        }

        private void ClearAllErrors()
        {
            for (int i = 0; i < 9; i++)
                for (int k = 0; k < 9; k++)
                    errors[i, k] = 0;
        }

        #endregion errors

        //----------------------------------------------------

        #region puzzle_generator

        private Dictionary<string, string> CalculateSolution()
        {
            return Solver.search(Solver.parse_grid(GetGameString()));
        }

        public void SolvePuzzle()
        {
            if (ComputeErors() != 0)
            {
                DisplayError("Fix errors in table!");
                return;
            }

            var solution = CalculateSolution();
            if (solution == null)
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
                res += item.Value;            
            FillGameBoardFromLine(res);

            ClearAllLists();

            RequestRepaint();
        }

        public bool IsSolved()
        {
            if (!IsFull()) return false;
            return (ComputeErors() == 0);
        }

        private bool IsFull()
        {
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    if (nums[j, i] == 0) return false;

            return true;
        }

        public void GenerateGame()
        {
            var fill = new Generator2().GenerateSudokuBoard();
            if (string.IsNullOrEmpty(fill))
                fill = Generator.Run();

            var puzz = new Puzzler(65).MakePuzzle(fill);
            
            SetGameString(puzz);
            LockNumbers();

            if (RunFindSingles)
                FindAllSingles();
        }

        private void SetAllFixesByValues()
        {
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    fixes[i, j] = nums[i, j] == 0 ? 0 : 1;
        }

        internal void MoveSelected(int move)
        {
            if (move == 2 && hity + 1 < 9) hity++;
            if (move == 1 && hity - 1 >= 0) hity--;
            if (move == 3 && hitx + 1 < 9) hitx++;
            if (move == 4 && hitx - 1 >= 0) hitx--;            
            RequestRepaint();
        }

        #endregion
    }
}