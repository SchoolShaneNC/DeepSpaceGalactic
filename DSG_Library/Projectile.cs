using System;
using Windows.UI.Xaml.Controls;

namespace DSG_Library
{
    public class Projectile : GamePiece
    {
        private double velocityX;
        private double velocityY;
        private int damage;
        private bool isPlayerProjectile;

        public double VelocityX { get { return velocityX; } set { velocityX = value; } }
        public double VelocityY { get { return velocityY; } set { velocityY = value; } }

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

        public bool IsPlayerProjectile { get { return isPlayerProjectile; } set { isPlayerProjectile = value; } }

        public Projectile(Image img) : base(img)
        {
            VelocityX = 0;
            VelocityY = 0;
            Damage = 1;
            IsPlayerProjectile = true;
        }
        //overloaded contructor to keep bool of isplayerprojectile , made it easier storing it in the class
        public Projectile(Image img, double velocityX, double velocityY, int damage, bool isPlayerProjectile) : base(img)
        {
            VelocityX = velocityX;
            VelocityY = velocityY;
            Damage = damage;
            IsPlayerProjectile = isPlayerProjectile;
        }
    }
}