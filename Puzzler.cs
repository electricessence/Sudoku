using System;
using System.Collections.Generic;

namespace Sudoku
{

    //https://github.com/igortrofymov/SudokuGenerator

    public class Puzzler
    {
        private int[,] table = new int[9,9];    

        int numberOfSwaps;
        
        public Puzzler(int numberOfSwaps)
        {
            this.numberOfSwaps = numberOfSwaps;            
        }
        static Random rnd = new Random((int)(DateTime.Now.Ticks));

        internal string MakePuzzle(string input)
        {
            FillBoard(input);
            Swap();
            Hide();
            return MakeLine();
        }

        static int RandomGenerator(Random rnd, out int first, out int second)
        {
            first = rnd.Next(0, 3);
            second = rnd.Next(0, 3);
            while (second == first)
                second = rnd.Next(0, 3);
            return rnd.Next(0, 3);
        }

        public void Swap()
        {
            for (int i = 0; i <= numberOfSwaps; i++)
            {
                int swapCase = rnd.Next(1, 5);
                switch (swapCase)
                {
                    case 1:
                        SwitchRowSmall();
                        break;
                    case 2:
                        SwitchRowBig();
                        break;
                    case 3:
                        SwitchRowCol();
                        SwitchRowBig();
                        SwitchRowCol();
                        break;
                    case 4:
                        SwitchRowCol();
                        SwitchRowSmall();
                        SwitchRowCol();
                        break;
                    case 5:
                        SwitchRowCol();
                        break;
                }
            }
        }

        void SwitchRowCol()
        {
            int[,] newArr = new int[9, 9];
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    newArr[i, j] = table[j, i];
                }
            }
            table = newArr;
        }

        void SwitchRowSmall()
        {
            int firstRowToSwap;
            int secondRowToSwap;
            int rowBig = RandomGenerator(rnd, out firstRowToSwap, out secondRowToSwap);
            int[] newArr = new int[9];
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    newArr[i] = table[rowBig * 3 + firstRowToSwap, i];
                    table[rowBig * 3 + firstRowToSwap, i] = table[rowBig * 3 + secondRowToSwap, i];
                    table[rowBig * 3 + secondRowToSwap, i] = newArr[i];
                }
            }
        }

        void SwitchRowBig()
        {
            int firstRowToSwap;
            int secondRowToSwap;
            RandomGenerator(rnd, out firstRowToSwap, out secondRowToSwap);
            int[] newArr = new int[9];
            for (int k = 0; k < 3; k++)
            {
                for (int i = 0; i < 9; i++)
                {
                    newArr[i] = table[firstRowToSwap * 3 + k, i];
                    table[firstRowToSwap * 3 + k, i] = table[secondRowToSwap * 3 + k, i];
                    table[secondRowToSwap * 3 + k, i] = newArr[i];
                }
            }
        }

        public void Hide()
        {
            for (int i = 0; i < 9; i++)
            {
                int hidedInRow = rnd.Next(4, 7);
                List<int> toHide = new List<int>();
                for (int h = 0; h <= hidedInRow; h++)
                {
                    int rand = rnd.Next(0, 9);
                    while (toHide.Contains(rand))
                        rand = rnd.Next(0, 9);
                    toHide.Add(rand);
                }
                foreach (var item in toHide)
                {
                    table[i, item] = 0;
                }
            }
        }

        public void FillBoard(string game)
        {
            if (game.Length != 81)
                throw new Exception($"Input game string should have length = 81, but was {game.Length}!");

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
                    table[i, j] = 0;                                    
                else                
                    table[i, j] = Convert.ToInt32(c);                                    
            }
        }

        private string MakeLine()
        {
            var res = "";
            for (int i = 0; i < 9; i++)             
                for (int j = 0; j < 9; j++)
                {
                    var value = table[i, j];
                    res += value == 0 ? "." : value.ToString();
                }             

            return res;
        }
    }
}