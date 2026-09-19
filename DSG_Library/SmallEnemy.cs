using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace DSG_Library
{
    public class SmallEnemy : Enemy
    {
        public SmallEnemy(Image img) : base(img)
        {
            Health = 1;
            PointValue = 150;
            Speed = 5.3;
            Damage = 20;
            FireRate = 1.0; //seconds between shots
        }
    }
}
