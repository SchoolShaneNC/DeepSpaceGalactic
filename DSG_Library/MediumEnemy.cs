using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace DSG_Library
{
    public class MediumEnemy : Enemy
    {
        public MediumEnemy(Image img) : base(img)
        {
            Health = 2;
            PointValue = 100;
            Speed = 3.6;
            Size = 40;
            Damage = 35;
            FireRate = 1.5;
        }
    }
}
