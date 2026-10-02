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
        private double velocityX;
        private double velocityY;

        private int health;
        private int pointValue;

        public double VelocityX
        {
            get { return velocityX; }
            set
            {
                velocityX = value;
            }
        }

        public double VelocityY
        {
            get { return velocityY; }
            set
            {
                velocityY = value;
            }
        }

        public int Health
        {
            get { return health; }
            set
            {
                if (value > 0)
                    health = value;
                else
                    health = 1;
            }
        }

        public int PointValue
        {
            get { return pointValue; }
            set
            {
                if (value >= 0)
                    pointValue = value;
                else
                    pointValue = 0;
            }
        }

        public Asteroid(Image img) : base(img)
        {
            VelocityX = 0;
            VelocityY = 0;

            Health = 1;
            PointValue = 10;
        }
    }
}
