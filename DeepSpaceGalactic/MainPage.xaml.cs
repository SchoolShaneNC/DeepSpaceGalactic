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
        private const int EnemyDirectionDecisionMilliseconds = 2000;
        private const int EnemyCollisionDirectionLockMilliseconds = 3000;
        private Player player;
        private List<Enemy> enemies;
        private readonly HashSet<Windows.System.VirtualKey> heldDirections;
        private readonly DispatcherTimer movementTimer;
        private readonly DispatcherTimer enemyMovementTimer;
        private readonly Dictionary<Enemy, EnemyMovementState> enemyMovementStates;
        private readonly Random random = new Random();

        public MainPage()
        {
            this.InitializeComponent();

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
            Loaded += MainPage_Loaded;
            Unloaded += MainPage_Unloaded;

            heldDirections = new HashSet<Windows.System.VirtualKey>();
            movementTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            movementTimer.Tick += MovementTimer_Tick;

            enemyMovementStates = new Dictionary<Enemy, EnemyMovementState>();
            enemyMovementTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            enemyMovementTimer.Tick += EnemyMovementTimer_Tick;

            // Create the player
            GamePiece playerPiece = GameLogic.CreatePiece(
                "PlayerShip1.png", 80, 400, 500);

            player = new Player(playerPiece.Img);

            MainGrid.Children.Add(player.Img);

            // Create the enemy collection
            enemies = new List<Enemy>();

            // Create enemies
            Enemy smallEnemy = CreateEnemy<SmallEnemy>("EnemyShip1.png", 100, 100);

            Enemy regularEnemy = CreateEnemy<Enemy>("EnemyShip1.png", 300, 100);

            Enemy largeEnemy = CreateEnemy<LargeEnemy>("EnemyShip2.png", 500, 100);

            // Add enemies to the List
            enemies.Add(smallEnemy);
            enemies.Add(regularEnemy);
            enemies.Add(largeEnemy);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            foreach (Enemy enemy in enemies)
            {
                enemyMovementStates.Add(enemy,new EnemyMovementState(now, EnemyDirectionDecisionMilliseconds));
            }
        }

        private Enemy CreateEnemy<T>(string imageName, int left, int top) where T : Enemy
        {
            GamePiece piece = GameLogic.CreatePiece(imageName, left, top);

            Enemy enemy = (Enemy)Activator.CreateInstance(typeof(T), piece.Img);
            enemy.Img.Width = enemy.Size;
            enemy.Img.Height = enemy.Size;

            MainGrid.Children.Add(enemy.Img);

            return enemy;
        }

        private void CoreWindow_KeyDown(object sender, Windows.UI.Core.KeyEventArgs e)
        {
            if (IsDirection(e.VirtualKey) && heldDirections.Add(e.VirtualKey))
            {
                MovePlayer(); // Move once immediately instead of waiting for the first timer tick.
                movementTimer.Start();
            }
        }

        private void CoreWindow_KeyUp(object sender, Windows.UI.Core.KeyEventArgs e)
        {
            if (IsDirection(e.VirtualKey))
            {
                heldDirections.Remove(e.VirtualKey);

                if (heldDirections.Count == 0)
                {
                    movementTimer.Stop();
                }
            }
        }

        private void MovementTimer_Tick(object sender, object e)
        {
            MovePlayer();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            enemyMovementTimer.Start();

        }

        private void EnemyMovementTimer_Tick(object sender, object e)
        {
            if (MainGrid.ActualWidth <= 0)
            {
                return;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;

            foreach (Enemy enemy in enemies)
            {
                EnemyMovementState state = enemyMovementStates[enemy];
                if (now >= state.NextDirectionDecision)
                {
                    if (now < state.DirectionLockedUntil)
                    {
                        state.NextDirectionDecision = state.DirectionLockedUntil.AddMilliseconds(
                            EnemyDirectionDecisionMilliseconds);
                    }
                    else
                    {
                        if (random.Next(4) == 0)
                        {
                            state.Direction *= -1;
                        }

                        state.NextDirectionDecision = now.AddMilliseconds(
                            EnemyDirectionDecisionMilliseconds);
                    }
                }

                MoveEnemyWithinGrid(enemy, state, now);
            }

            ReverseCollidingEnemies(now);
        }

        private void MoveEnemyWithinGrid(Enemy enemy, EnemyMovementState state, DateTimeOffset now)
        {
            double maximumLeft = MainGrid.ActualWidth - enemy.Img.Width;
            double nextLeft = enemy.Position.Left + (enemy.Speed * state.Direction);

            if (nextLeft <= 0)
            {
                enemy.Move(-enemy.Position.Left, 0);
                ReverseEnemy(enemy, now);
            }
            else if (nextLeft >= maximumLeft)
            {
                enemy.Move(maximumLeft - enemy.Position.Left, 0);
                ReverseEnemy(enemy, now);
            }
            else
            {
                enemy.Move(enemy.Speed * state.Direction, 0);
            }
        }

        private void ReverseCollidingEnemies(DateTimeOffset now)
        {
            for (int first = 0; first < enemies.Count - 1; first++)
            {
                for (int second = first + 1; second < enemies.Count; second++)
                {
                    if (GameLogic.IsCollision(enemies[first], enemies[second]))
                    {
                        ReverseEnemy(enemies[first], now);
                        ReverseEnemy(enemies[second], now);
                    }
                }
            }
        }

        private void ReverseEnemy(Enemy enemy, DateTimeOffset now)
        {
            EnemyMovementState state = enemyMovementStates[enemy];
            state.Direction *= -1;
            state.DirectionLockedUntil = now.AddMilliseconds(EnemyCollisionDirectionLockMilliseconds);
            state.NextDirectionDecision = state.DirectionLockedUntil.AddMilliseconds(EnemyDirectionDecisionMilliseconds);
        }

        private void MovePlayer()
        {
            if (MainGrid.ActualWidth <= 0 || MainGrid.ActualHeight <= 0)
            {
                return;
            }

            double horizontal = (heldDirections.Contains(Windows.System.VirtualKey.Right) ? 1 : 0)
                - (heldDirections.Contains(Windows.System.VirtualKey.Left) ? 1 : 0);

            double vertical = (heldDirections.Contains(Windows.System.VirtualKey.Down) ? 1 : 0)
                - (heldDirections.Contains(Windows.System.VirtualKey.Up) ? 1 : 0);

            if (horizontal != 0 && vertical != 0)
            {
                const double diagonalMultiplier = 0.7071067811865476;
                horizontal *= diagonalMultiplier;
                vertical *= diagonalMultiplier;
            }

            double newLeft = player.Position.Left + horizontal * player.Speed;
            double newTop = player.Position.Top + vertical * player.Speed;

            double maximumLeft = MainGrid.ActualWidth - player.Img.Width;
            double minimumTop = MainGrid.ActualHeight * 0.50;
            double maximumTop = MainGrid.ActualHeight - player.Img.Height;

            if (maximumLeft < 0 || maximumTop < minimumTop)
            {
                return;
            }

            newLeft = Math.Max(0, Math.Min(newLeft, maximumLeft));
            newTop = Math.Max(minimumTop, Math.Min(newTop, maximumTop));

            player.Move(newLeft - player.Position.Left, newTop - player.Position.Top);
        }
        private static bool IsDirection(Windows.System.VirtualKey key)
        {
            return key == Windows.System.VirtualKey.Up
                || key == Windows.System.VirtualKey.Down
                || key == Windows.System.VirtualKey.Left
                || key == Windows.System.VirtualKey.Right;
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            movementTimer.Stop();
            enemyMovementTimer.Stop();
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp -= CoreWindow_KeyUp;
            Loaded -= MainPage_Loaded;
        }


    }
}
