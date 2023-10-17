// Ignore Spelling: Sudoku

using System;
using System.Windows.Forms;

namespace Sudoku
{
	public partial class Form1 : Form
	{
		Sudoku _sudoku;

		public Form1()
		{
			InitializeComponent();

			this.Height = Screen.PrimaryScreen.WorkingArea.Height;
			this.Width = this.Height;
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			InitializeGame();
			_sudoku.GenerateGame();
		}

		private void InitializeGame() => _sudoku = new Sudoku
		{
			RequestRepaint = () => RepaintPictureBox(),
			DisplayInfo = (s) => DisplayInfo(s),
			DisplayError = (s) => DisplayErrorBox(s),

			RunFindSingles = AutomFindSingles.Checked
		};

		private void DisplayInfo(string info) => MessageBox.Show(info);

		private void PictureBox1_Paint(object sender, PaintEventArgs e) => _sudoku.Draw(e.Graphics, 0);

		private void RepaintPictureBox() => pictureBox1.Invalidate();

		private void Form1_KeyDown(object sender, KeyEventArgs e) => _sudoku.KeyPress(e.KeyCode);

		private void PictureBox1_SizeChanged(object sender, EventArgs e) => RepaintPictureBox();

		private void NewToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			DialogResult answer = MessageBox.Show(
					"Clear all fields?", "Sudoku",
					MessageBoxButtons.YesNo);
			if (answer == DialogResult.No)
			{
				return;
			}

			InitializeGame();
			RepaintPictureBox();
		}

		private void AboutToolStripMenuItem_Click(object sender, EventArgs e) => MessageBox.Show(
				"Copyleft © 2021 by Bondjasan\n " +
				"\nCtrl+C / Ctrl+V - copy / paste sudoku numbers.\n" +
				"\nSelect number with mouse right click.\n" +
				"\nWhen number is selected, app will display" +
				"\nfree cels for that number.\n" +
				"\nAdd / delete candidate: " +
				"\n- Select number, Holl ALT + right mouse button" +
				"\n- Hold ALT + number \n" +
				"\nMark / demark candidate:" +
				"\n- Select number, Hold CTRL + right mouse button" +
				"\n- Hold CTRL + number\n" +
				"\nBackspace for undo operation!",
				"About Sudoku 1.0", MessageBoxButtons.OK);

		private void CopyToolStripMenuItem_Click(object sender, EventArgs e) => Clipboard.SetText(_sudoku.GetGameString());

		private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var text = Clipboard.GetText();
			if (!_sudoku.SetGameString(text))
			{
				DisplayErrorBox("The clipboard does not contain a valid puzzle.\n" + text);
			}
			else if (_sudoku.RunFindSingles)
			{
				_sudoku.FindAllSingles();
			}

			RepaintPictureBox();
		}

		private void OpenToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			openFileDialog1.FileName = "";

			if (openFileDialog1.ShowDialog() != DialogResult.OK)
			{
				return;
			}

			try
			{
				_sudoku.LoadAllData(openFileDialog1.FileName);
			}
			catch
			{
				DisplayErrorBox("Error: read data from file!");
			}

			RepaintPictureBox();
		}

		private void SaveToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			if (saveFileDialog1.ShowDialog() != DialogResult.OK)
			{
				return;
			}

			try
			{
				_sudoku.SaveAllData(saveFileDialog1.FileName);
			}
			catch
			{
				DisplayErrorBox("Error: save data into file!");
			}
		}

		private static void DisplayErrorBox(string s) => MessageBox.Show(s, "Error",
			  MessageBoxButtons.OK, MessageBoxIcon.Error);

		private void ExitToolStripMenuItem_Click(object sender, EventArgs e) => Close();

		private void ShowPossibleValuesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			_sudoku.ShowCandidates = !_sudoku.ShowCandidates;
			RepaintPictureBox();
		}

		private void ToolStripMenuItem2_Click(object sender, EventArgs e) => _sudoku.GenerateGame();

		private void PictureBox1_Click(object sender, EventArgs e)
		{
			MouseEventArgs me = (MouseEventArgs)e;
			_sudoku.SetLocation(me.Location, me.Button == MouseButtons.Left);
			RepaintPictureBox();
		}

		private void FillOnlyPossibleValuesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			_sudoku.FillOnlyPossibleValues();
			RepaintPictureBox();
		}

		private void FindSingleNumbersToolStripMenuItem_Click(object sender, EventArgs e) => _sudoku.FindAllSingles();

		private void ShowPairsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			_sudoku.MarkPairsCandidates();
			RepaintPictureBox();
		}

		private void SolvePuzzleToolStripMenuItem_Click_1(object sender, EventArgs e) => _sudoku.SolvePuzzle();

		private void LockNumbersToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			DialogResult answer = MessageBox.Show(
					"Lock numbers?", "Sudoku",
					MessageBoxButtons.YesNo);
			if (answer == DialogResult.No)
			{
				return;
			}

			_sudoku.LockNumbers();
		}

		private void AutomFindSinglesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (sender == AutomFindSingles)
			{
				_sudoku.RunFindSingles = AutomFindSingles.Checked;
			}
		}

		private void MarkAllPairsGuessesToolStripMenuItem_Click(object sender, EventArgs e) => _sudoku.MarkAllPairsGuesses();
	}
}
