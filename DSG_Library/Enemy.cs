using DSG_Library;
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
        private int health;
        private int pointValue;
        private double speed;
        private int damage;
        private double fireRate;
        private int size;

      
        public int Health
        {
            get { return health; }
            set
            {
                if (value < 0)
                    health = 0;
                else
                    health = value;
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
                    pointValue = 100;
            }
        }

        public double Speed
        {
            get { return speed; }
            set
            {
                if (value > 0)
                    speed = value;
                else
                    speed = 3.6;
            }
        }

        public int Damage
        {
            get { return damage; }
            set
            {
                if (value > 0)
                    damage = value;
                else
                    damage = 35;
            }
        }

        public double FireRate
        {
            get { return fireRate; }
            set
            {
                if (value > 0)
                    fireRate = value;
                else
                    fireRate = 1.5;
            }
        }

        public int Size
        {
            get { return size; }
            set
            {
                if (value > 0)
                    size = value;
                else
                    size = 40;
            }
        }

        public Enemy(Image img) : base(img)
        {
        }
    }
}
