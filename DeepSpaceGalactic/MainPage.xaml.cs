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
        private const int EnemyDirectionDecisionMilliseconds = 1700;
        private const int EnemyCollisionDirectionLockMilliseconds = 2700;
        private const double MinimumAsteroidSpawnSeconds = 0.7;
        private const double MaximumAsteroidSpawnSeconds = 2.0;
        private const int MinimumAsteroidsPerSpawn = 1;
        private const int MaximumAsteroidsPerSpawn = 4;
        private const int MinimumAsteroidSize = 35;
        private const int MaximumAsteroidSize = 80;
        private Player player;
        private List<Enemy> enemies;
        private List<Projectile> projectiles;
        private List<Asteroid> asteroids;
        private readonly HashSet<Windows.System.VirtualKey> heldDirections;
        private readonly DispatcherTimer movementTimer;
        private readonly DispatcherTimer enemyMovementTimer;
        private readonly Dictionary<Enemy, EnemyMovementState> enemyMovementStates;
        private readonly Dictionary<Enemy, EnemyCombatState> enemyCombatStates;
        private readonly Random random = new Random();
        private DateTimeOffset nextPlayerShotTime;
        private DateTimeOffset nextAsteroidSpawnTime;

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
            enemyCombatStates = new Dictionary<Enemy, EnemyCombatState>();
            enemyMovementTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            enemyMovementTimer.Tick += EnemyMovementTimer_Tick;

            // Create the player
            GamePiece playerPiece = GameLogic.CreatePiece("SpaceShips/PlayerShip1.png", 80, 400, 500);
            //sets shot timer to lowest value to start
            nextPlayerShotTime = DateTimeOffset.MinValue;

            player = new Player(playerPiece.Img);

            MainGrid.Children.Add(player.Img);

            // Create the enemy collection
            enemies = new List<Enemy>();
            projectiles = new List<Projectile>();
            asteroids = new List<Asteroid>();

            // Create enemies
            Enemy smallEnemy = CreateEnemy<SmallEnemy>("SpaceShips/EnemyShip1.png", 100, 100);

            Enemy regularEnemy = CreateEnemy<MediumEnemy>("SpaceShips/EnemyShip1.png", 300, 100);

            Enemy largeEnemy = CreateEnemy<LargeEnemy>("SpaceShips/EnemyShip2.png", 500, 100);

            // Add enemies to the List
            enemies.Add(smallEnemy);
            enemies.Add(regularEnemy);
            enemies.Add(largeEnemy);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            foreach (Enemy enemy in enemies)
            {
                enemyMovementStates.Add(enemy,new EnemyMovementState(now, EnemyDirectionDecisionMilliseconds));
                enemyCombatStates.Add(enemy, new EnemyCombatState());
            
            }

           // txtTest.Text = $"{player.Health}, {player.Speed}, {player.Damage}, {player.FireRate}";
        }
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ScheduleNextAsteroidSpawn(DateTimeOffset.UtcNow);
            enemyMovementTimer.Start();

        }
        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            movementTimer.Stop();
            enemyMovementTimer.Stop();
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp -= CoreWindow_KeyUp;
            Loaded -= MainPage_Loaded;
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

        #region Keybaord input
        private void CoreWindow_KeyDown(object sender, Windows.UI.Core.KeyEventArgs e)
        {
            if (e.VirtualKey == Windows.System.VirtualKey.Space)
            {
                CreatePlayerProjectile();

                return;
            }

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

        private static bool IsDirection(Windows.System.VirtualKey key)
        {
            return key == Windows.System.VirtualKey.Up
                || key == Windows.System.VirtualKey.Down
                || key == Windows.System.VirtualKey.Left
                || key == Windows.System.VirtualKey.Right;
        }


        #endregion

        #region Timer ticks
        private void MovementTimer_Tick(object sender, object e)
        {
            MovePlayer();
        }
        private void EnemyMovementTimer_Tick(object sender, object e)
        {
            if (MainGrid.ActualWidth <= 0 || MainGrid.ActualHeight <= 0)
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
                        state.NextDirectionDecision = state.DirectionLockedUntil.AddMilliseconds(EnemyDirectionDecisionMilliseconds);
                    }
                    else
                    {
                        if (random.Next(4) == 0)
                        {
                            state.Direction *= -1;
                        }
                        state.NextDirectionDecision = now.AddMilliseconds(EnemyDirectionDecisionMilliseconds);
                    }
                }

                MoveEnemyWithinGrid(enemy, state, now);

                EnemyCombatState combatState = enemyCombatStates[enemy];
                if (now >= combatState.NextShotTime)
                {
                    CreateEnemyProjectile(enemy);
                    combatState.NextShotTime = now.AddSeconds(enemy.FireRate);
                }
            }

            ReverseCollidingEnemies(now);
            MoveProjectiles();
            SpawnAndMoveAsteroids(now);
            ProcessCollisions();
        }
        #endregion

        #region Collision and After collision damage/removal
        private void ProcessCollisions()
        {
            HashSet<Projectile> destroyedProjectiles = new HashSet<Projectile>();
            HashSet<Asteroid> destroyedAsteroids = new HashSet<Asteroid>();
            HashSet<Enemy> destroyedEnemies = new HashSet<Enemy>();

            foreach (Projectile projectile in projectiles)
            {
                if (projectile.IsPlayerProjectile)
                {
                    foreach (Enemy enemy in enemies)
                    {
                        if (destroyedEnemies.Contains(enemy) || !GameLogic.IsCollision(projectile, enemy))
                        {
                            continue;
                        }

                        enemy.Health -= player.Damage;
                        destroyedProjectiles.Add(projectile);

                        if (enemy.Health <= 0)
                        {
                            player.Score += enemy.PointValue;
                            destroyedEnemies.Add(enemy);
                        }

                        break;
                    }

                    if (destroyedProjectiles.Contains(projectile))
                    {
                        continue;
                    }
                }
                else if (GameLogic.IsCollision(projectile, player))
                {
                    ApplyDamageToPlayer(projectile.Damage);
                    destroyedProjectiles.Add(projectile);
                    continue;
                }

                foreach (Asteroid asteroid in asteroids)
                {
                    if (destroyedAsteroids.Contains(asteroid) || !GameLogic.IsCollision(projectile, asteroid))
                    {
                        continue;
                    }

                    destroyedProjectiles.Add(projectile);
                    destroyedAsteroids.Add(asteroid);
                    break;
                }
            }

            foreach (Asteroid asteroid in asteroids)
            {
                if (!destroyedAsteroids.Contains(asteroid) && GameLogic.IsCollision(player, asteroid))
                {
                    ApplyDamageToPlayer(asteroid.Damage);
                    destroyedAsteroids.Add(asteroid);
                }
            }

            RemoveDestroyedProjectiles(destroyedProjectiles);
            RemoveDestroyedAsteroids(destroyedAsteroids);
            RemoveDestroyedEnemies(destroyedEnemies);
        }

        private void ApplyDamageToPlayer(int damage)
        {
            int healthBeforeHit = player.Health;
            player.Health -= damage;

            if (healthBeforeHit > 0 && player.Health == 0)
            {
                player.Lives -= 1;
            }
            txtTest.Text = $"Health: {player.Health.ToString()} Lives: {player.Lives.ToString()}";
        }
        private void RemoveDestroyedAsteroids(IEnumerable<Asteroid> destroyedAsteroids)
        {
            foreach (Asteroid asteroid in destroyedAsteroids)
            {
                asteroids.Remove(asteroid);
                MainGrid.Children.Remove(asteroid.Img);
            }
        }

        private void RemoveDestroyedEnemies(IEnumerable<Enemy> destroyedEnemies)
        {
            foreach (Enemy enemy in destroyedEnemies)
            {
                enemies.Remove(enemy);
                enemyMovementStates.Remove(enemy);
                enemyCombatStates.Remove(enemy);
                MainGrid.Children.Remove(enemy.Img);
            }
        }
        #endregion

        #region Astriods 

        private void SpawnAndMoveAsteroids(DateTimeOffset now)
        {
            if (now >= nextAsteroidSpawnTime)
            {
                int asteroidCount = GetAsteroidsPerSpawn();
                for (int count = 0; count < asteroidCount; count++)
                {
                    CreateAsteroid();
                }

                ScheduleNextAsteroidSpawn(now);
            }

            for (int index = asteroids.Count - 1; index >= 0; index--)
            {
                Asteroid asteroid = asteroids[index];
                asteroid.Move(asteroid.VelocityX, asteroid.VelocityY);

                if (asteroid.Position.Top + asteroid.Img.Height < 0
                    || asteroid.Position.Left + asteroid.Img.Width < 0
                    || asteroid.Position.Left > MainGrid.ActualWidth
                    || asteroid.Position.Top > MainGrid.ActualHeight)
                {
                    asteroids.RemoveAt(index);
                    MainGrid.Children.Remove(asteroid.Img);
                }
            }
        }

        private void CreateAsteroid()
        {
            int size = random.Next(MinimumAsteroidSize, MaximumAsteroidSize + 1);
            int left;
            int top;
            int spawnEdge = random.Next(3);
            double velocityX;

            if (spawnEdge == 0)
            {
                left = random.Next(0, Math.Max(1, (int)MainGrid.ActualWidth - size + 1));
                top = -size;
                velocityX = GetAsteroidHorizontalVelocity();
            }
            else if (spawnEdge == 1)
            {
                left = -size;
                top = random.Next(0, Math.Max(1, (int)(MainGrid.ActualHeight / 2.5)));
                velocityX = random.Next(1, 4);
            }
            else
            {
                left = (int)MainGrid.ActualWidth;
                top = random.Next(0, Math.Max(1, (int)(MainGrid.ActualHeight / 2.5)));
                velocityX = -random.Next(1, 4);
            }

            string imageName = random.Next(2) == 0 ? "Astriods/Astriod1.png" : "Astriods/Astriod2.png";
            GamePiece piece = GameLogic.CreatePiece(imageName, size, left, top);
            Asteroid asteroid = new Asteroid(piece.Img)
            {
                VelocityX = velocityX,
                VelocityY = random.Next(2, 6)
            };

            asteroids.Add(asteroid);
            MainGrid.Children.Add(asteroid.Img);
        }

        private int GetAsteroidsPerSpawn()
        {
            // 1 and 2 are common; 3 is less common; 4 is an occasional burst.
            int weightedChoice = random.Next(10);
            int count = weightedChoice < 4 ? 1
                : weightedChoice < 7 ? 2
                : weightedChoice < 9 ? 3
                : 4;

            return Math.Max(MinimumAsteroidsPerSpawn, Math.Min(count, MaximumAsteroidsPerSpawn));
        }

        private double GetAsteroidHorizontalVelocity()
        {
            int horizontalSpeed = random.Next(0, 6);
            return random.Next(2) == 0 ? -horizontalSpeed : horizontalSpeed;
        }

        private void ScheduleNextAsteroidSpawn(DateTimeOffset now)
        {
            double delay = MinimumAsteroidSpawnSeconds + random.NextDouble() * (MaximumAsteroidSpawnSeconds - MinimumAsteroidSpawnSeconds);
            nextAsteroidSpawnTime = now.AddSeconds(delay);
        }

        #endregion

        #region Projectiles
        private void CreatePlayerProjectile()
        {
            //checks if player is allowed to shoot. 
            if (DateTimeOffset.UtcNow < nextPlayerShotTime)
            {
                return;
            }

            //creates a projectile at the center of the player and moves it just above the player
            const int projectileSize = 15;
            //finds the middle of the player image
            int left = (int)(player.Position.Left + (player.Img.Width - projectileSize) / 2);
            //finds the top of the player image and moves the projectile just above it
            int top = (int)(player.Position.Top - projectileSize);
            GamePiece piece = GameLogic.CreatePiece("LazerBeams/BlueLazerBeam.png", projectileSize, left, top);

            Projectile projectile = new Projectile(piece.Img, 0, -18, player.Damage, true);

            projectiles.Add(projectile);
            MainGrid.Children.Add(projectile.Img);

            //after player shoots, sets the next time the player is allowed to shoot based on the player's fire rate
            nextPlayerShotTime = DateTimeOffset.UtcNow.AddSeconds(player.FireRate);

        }

        private void CreateEnemyProjectile(Enemy enemy)
        {
            const int projectileSize = 15;

            int left =(int)(enemy.Position.Left + (enemy.Img.Width - projectileSize) / 2);
            int top = (int)(enemy.Position.Top + enemy.Img.Height);

            GamePiece piece = GameLogic.CreatePiece("LazerBeams/RedLazerBeam.png", projectileSize, left, top);

            Projectile projectile = new Projectile(piece.Img, 0, 18, enemy.Damage, false);

            projectiles.Add(projectile);
            MainGrid.Children.Add(projectile.Img);
        }

        private void MoveProjectiles()
        {
            //loops through projectile list backwards so that we can remove projectiles from the list without affecting the loop
            //moves each projectile based on its velocity and removes it from the list if it has moved off the top of the screen
            for (int index = projectiles.Count - 1; index >= 0; index--)
            {
                Projectile projectile = projectiles[index];
                projectile.Move(projectile.VelocityX, projectile.VelocityY);

                //checks if a projectile is out of the playing area then removes it, checks top then bottom then the sides
                if (projectile.Position.Top + projectile.Img.Height < 0 || projectile.Position.Top > MainGrid.ActualHeight
                    || projectile.Position.Left + projectile.Img.Width < 0
                    || projectile.Position.Left > MainGrid.ActualWidth)
                {
                    projectiles.RemoveAt(index);
                    MainGrid.Children.Remove(projectile.Img);
                }
             //   txtTest.Text = projectiles.Count.ToString();
            }
        }

        private void RemoveDestroyedProjectiles(IEnumerable<Projectile> destroyedProjectiles)
        {
            foreach (Projectile projectile in destroyedProjectiles)
            {
                projectiles.Remove(projectile);
                MainGrid.Children.Remove(projectile.Img);
            }
        }


        #endregion

        #region Enemy / Player movement
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
        #endregion


  


    }
}
