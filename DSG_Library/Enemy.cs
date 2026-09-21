using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace DSG_Library
{
    public class Enemy : GamePiece
    {
        public int Health { get; set; }
        public int PointValue { get; set; }
        public double Speed { get; set; }
        public int Damage { get; set; }
        public double FireRate { get; set; }

        public int Size { get; set; }

        public Enemy(Image img) : base(img)
        {
            Health = 2;
            PointValue = 100;
            Speed = 3.6;
            Size = 40;
            Damage = 35;
            FireRate = 1.5; // shots per idk yet  prolly seconds between shots
        }
    }
}
