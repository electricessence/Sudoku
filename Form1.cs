using System;
using System.Windows.Forms;


namespace Sudoku
{
    public partial class Form1 : Form
    {
        Sudoku sudoku;

        public Form1()
        {
            InitializeComponent();

            this.Height = Screen.PrimaryScreen.WorkingArea.Height;
            this.Width = this.Height;                
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeGame();
            sudoku.GenerateGame();
        }

        private void InitializeGame()
        {
            sudoku = new Sudoku();

            sudoku.RequestRepaint = () => RepaintPictureBox();
            sudoku.DisplayInfo = (s) => DisplayInfo(s);
            sudoku.DisplayError = (s) => DisplayErrorBox(s);

            sudoku.RunFindSingles = AutomFindSingles.Checked;
        }

        private void DisplayInfo(string info)
        {            
            MessageBox.Show(info);        
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            sudoku.Draw(e.Graphics, 0);
        }

        private void RepaintPictureBox()
        {
            pictureBox1.Invalidate();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            sudoku.KeyPress(e.KeyCode);
        }

        private void pictureBox1_SizeChanged(object sender, EventArgs e)
        {
            RepaintPictureBox();
        }

        private void newToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DialogResult answer = MessageBox.Show(
                    "Clear all fields?", "Sudoku",
                    MessageBoxButtons.YesNo);
            if (answer == DialogResult.No)
                return;

            InitializeGame();
            RepaintPictureBox();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
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
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(sudoku.GetGameString());
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var text = Clipboard.GetText();
            if (!sudoku.SetGameString(text))
                DisplayErrorBox("The clipboard does not contain a valid puzzle.\n" + text);
            else if (sudoku.RunFindSingles)
                sudoku.FindAllSingles();

            RepaintPictureBox();
        }

        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "";

            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;

            try
            {
                sudoku.LoadAllData(openFileDialog1.FileName);
            }
            catch
            {
                DisplayErrorBox("Error: read data from file!");
            }

            RepaintPictureBox();
        }

        private void saveToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if(saveFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                sudoku.SaveAllData(saveFileDialog1.FileName);
            }
            catch
            {
                DisplayErrorBox("Error: save data into file!");
            }
        }

        private static void DisplayErrorBox(string s)
        {
            MessageBox.Show(s, "Error",
              MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        
        private void showPossibleValuesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sudoku.ShowCandidates = !sudoku.ShowCandidates;
            RepaintPictureBox();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            sudoku.GenerateGame();            
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            sudoku.SetLocation(me.Location, me.Button == MouseButtons.Left);
            RepaintPictureBox();
        }

        private void fillOnlyPossibleValuesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sudoku.FillOnlyPossibleValues();
            RepaintPictureBox();
        }

        private void findSingleNumbersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sudoku.FindAllSingles();
        }        

        private void showPairsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sudoku.MarkPairsCandidates();
            RepaintPictureBox();
        }

        private void solvePuzzleToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            sudoku.SolvePuzzle();
        }

        private void lockNumbersToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            DialogResult answer = MessageBox.Show(
                    "Lock numbers?", "Sudoku",
                    MessageBoxButtons.YesNo);
            if (answer == DialogResult.No)
                return;

            sudoku.LockNumbers();
        }

        private void automFindSinglesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sender == AutomFindSingles)
                sudoku.RunFindSingles = AutomFindSingles.Checked;
        }

        private void markAllPairsGuessesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sudoku.MarkAllPairsGuesses();
        }
    }
}
