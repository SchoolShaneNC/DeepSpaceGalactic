using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using DSG_Library;

namespace DeepSpaceGalactic
{
    public sealed partial class MainPage : Page
    {
        private Player player;
        private List<Enemy> enemies;

        public MainPage()
        {
            this.InitializeComponent();

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            // Create the player
            GamePiece playerPiece = GameLogic.CreatePiece(
                "PlayerShip1.png", 80, 400, 500);

            player = new Player(playerPiece.Img);

            MainGrid.Children.Add(player.Img);

            // Create the enemy collection
            enemies = new List<Enemy>();

            // Create enemies
            Enemy smallEnemy = CreateEnemy<SmallEnemy>(
                "EnemyShip1.png", 50, 100, 100);

            Enemy regularEnemy = CreateEnemy<Enemy>(
                "EnemyShip1.png", 70, 300, 100);

            Enemy largeEnemy = CreateEnemy<LargeEnemy>(
                "EnemyShip2.png", 100, 500, 100);

            // Add enemies to the List
            enemies.Add(smallEnemy);
            enemies.Add(regularEnemy);
            enemies.Add(largeEnemy);
        }

        private Enemy CreateEnemy<T>(
            string imageName,
            int size,
            int left,
            int top) where T : Enemy
        {
            GamePiece piece = GameLogic.CreatePiece(
                imageName, size, left, top);

            Enemy enemy = (Enemy)Activator.CreateInstance(
                typeof(T), piece.Img);

            MainGrid.Children.Add(enemy.Img);

            return enemy;
        }

        private void CoreWindow_KeyDown(
            object sender,
            Windows.UI.Core.KeyEventArgs e)
        {
            player.Move(e.VirtualKey);
        }
    }
}