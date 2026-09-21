using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace DSG_Library
{
    public class LargeEnemy : Enemy
    {
        public LargeEnemy(Image img) : base(img)
        {
            Health = 5;
            PointValue = 300;
            Speed = 2.3;
            Size = 150;
            Damage = 50;
            FireRate = 3.0; //seconds between shots
        }
    }
}
