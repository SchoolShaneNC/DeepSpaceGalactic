using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using static System.Net.Mime.MediaTypeNames;

namespace DSG_Library
{
    public class Player : GamePiece
    {

        private int lives;
        private int health;
        private double firerate;
        private double speed;
        private int score;
        private int damage;

        public int Lives { get { return lives; } 
            set 
            { 
                lives = Math.Max(0, Math.Min(6, value));
            } 
        }

        public int Health { get { return health;  } 
            set 
            {
                health = Math.Max(0, Math.Min(200, value));
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

        public int Damage
        {
            get { return damage; }
            set
            {
                if (value > 0)
                    damage = value;
                else
                    damage = 1;
            }
        }

        //public int Lives
        //{
        //    get { return lives; }
        //    set
        //    {
        //        if (value > 0 && value < 6)
        //            lives = value;
        //        else
        //            lives = 3;
        //    }
        //}


        //public int Health
        //{
        //    get { return health; }
        //    set
        //    {
        //        if (value > 0 && value < 200)
        //            health = value;
        //        else
        //            health = 100;
        //    }
        //}



        //out of nowhere it gave ambigius error for image so for time being just fully clarified what Image type
        public Player(Windows.UI.Xaml.Controls.Image img) : base(img)
        {
            Lives = 3;
            Health = 100;
            FireRate = 0.5; //seconds between shots
            Speed = 9;
            Score = 0;
            Damage = 1;
        }
    }
}
