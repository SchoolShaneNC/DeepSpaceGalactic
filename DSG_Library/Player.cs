using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using static System.Net.Mime.MediaTypeNames;

namespace DSG_Library
{
    public class Player : GamePiece, INotifyPropertyChanged
    {

        private int lives;
        private int health;
        private double firerate;
        private double speed;
        private int score;
        private int damage;
        //propertychanged for invoking the objec binding to the xaml
        public event PropertyChangedEventHandler PropertyChanged;

        public int Lives
        {
            get { return lives; }
            set
            {
                if (value < 0 || value > 6)
                    lives = 0;
                else
                    lives = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Lives)));
            }
        }

        public int Health
        {
            get { return health; }
            set
            {
                if (value < 0 || value > 10000)
                    health = 0;
                else
                    health = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Health)));
            }
        }

        public double FireRate
        {
            get { return firerate; }
            set
            {
                if (value > 0.2 && value < 5)
                    firerate = value;
                else
                    firerate = 0.5;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FireRate)));
            }
        }

        public double Speed
        {
            get { return speed; }
            set
            {
                if (value > 0 && value < 20)
                    speed = value;
                else
                    speed = 9;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Speed)));
            }
        }

        public int Score
        {
            get { return score; }
            set
            {
                if (value >= 0)
                    score = value;
                else
                    score = 0;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Score)));
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

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Damage)));
            }
        }

        //out of nowhere it gave ambigius error for image so for time being just fully clarified what Image type
        public Player(Windows.UI.Xaml.Controls.Image img) : base(img)
        {
            Lives = 3;
            Health = 100;  //to test a win making this 9999 and firerate .1 helps
            FireRate = 0.5; //seconds between shots
            Speed = 9;
            Score = 0;
            Damage = 1;
        }

        public Player(Windows.UI.Xaml.Controls.Image img, int lives, int health, double firerate, double speed, int score, int damage) : base(img)
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


