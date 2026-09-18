using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace DSG_Library
{
    public class Asteroid : GamePiece
    {
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }

        public int Health { get; set; }
        public int PointValue { get; set; }

        public Asteroid(Image img) : base(img)
        {
            VelocityX = 0;
            VelocityY = 0;

            Health = 1;
            PointValue = 50;
        }
    }
}