using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace DSG_Library
{
    public class Player : GamePiece
    {
        public int Lives { get; set; }

        public int Health { get; set; }

        public double FireRate { get; set; }

        public double Speed { get; set; }
        public int Score { get; set; }

        public Player(Image img) : base(img)
        {
            Lives = 3;
            Health = 100;
            FireRate = 0.5; //seconds between shots
            Speed = 4;
            Score = 0;
        }
    }
}
