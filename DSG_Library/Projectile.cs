using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace DSG_Library
{
    public class Projectile : GamePiece
    {
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }

        public bool IsPlayerProjectile { get; set; }

        public Projectile(Image img) : base(img)
        {
            VelocityX = 0;
            VelocityY = 0;
            IsPlayerProjectile = true;
        }
    }
}
