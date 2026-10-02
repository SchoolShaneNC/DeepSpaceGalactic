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

        private int lives;
        private int health;
        private double firerate;
        private double speed;
        private int score;

        public int Lives { get { return lives; } 
            set 
            { 
                if (value > 0 && value < 6)
                    lives = value;
                else
                    lives = 3; 
            } 
        }

        public int Health { get { return health;  } 
            set 
            {
                if (value > 0 && value < 200)
                    health = value;
                else
                    health = 100;
            } 
        }

        public double FireRate { get { return firerate; } 
            set 
            { 
                if (value > 0.2 && value < 5)
                    firerate = value;
                else
                    firerate = 0.5;
            } 
        }

        public double Speed { get { return speed; } 
            set 
            {
                if (value > 0 && value < 20)
                    speed = value;
                else
                    speed = 9;
            } 
        }
        public int Score { get { return score; }
            set
            {
                if (value >= 0)
                    score = value;
                else
                    score = 0;
            }
        }

        public Player(Image img) : base(img)
        {
            Lives = 3;
            Health = 100;
            FireRate = 0.5; //seconds between shots
            Speed = 9;
            Score = 0;
        }
    }
}
