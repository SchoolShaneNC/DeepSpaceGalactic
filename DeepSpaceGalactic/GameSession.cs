using System;
using System.Collections.Generic;
using DSG_Library;
using Windows.System;
using Windows.UI.Xaml;

namespace DeepSpaceGalactic
{
    /// <summary>
    /// Owns gameplay state and rules for a single play session. The page renders
    /// presentation events rather than participating in game mechanics.
    /// </summary>
    public sealed class GameSession : IDisposable
    {
        private const int EnemyDirectionDecisionMilliseconds = 1700;
        private const int EnemyCollisionDirectionLockMilliseconds = 2700;
        private const double MinimumAsteroidSpawnSeconds = 0.7;
        private const double MaximumAsteroidSpawnSeconds = 2.0;
        private const int MinimumAsteroidsPerSpawn = 1;
        private const int MaximumAsteroidsPerSpawn = 4;
        private const int MinimumAsteroidSize = 40;
        private const int MaximumAsteroidSize = 80;
        private const int FirstLevel = 1;
        private const int MaximumLevel = 6;
        private const int BaseSmallEnemyCount = 1;
        private const int BaseMediumEnemyCount = 1;
        private const int BaseLargeEnemyCount = 1;
        private const int AdditionalEnemiesPerIncrease = 1;
        private const int AdditionalHealthPerStatIncrease = 1;
        private const int AdditionalDamagePerStatIncrease = 15;
        private const double FireRateReductionPerStatIncrease = 0.08;
        private const double MinimumEnemyFireRateSeconds = 0.35;
        private const int LargeEnemyRowY = 60;
        private const int MediumEnemyRowY = 120;
        private const int SmallEnemyRowY = 180;
        private const double DefaultBoardWidth = 1000;
        private const int ProjectileSize = 15;
        private const double PlayerMinimumBoardHeightPercentage = 0.5;
        private const double DiagonalMovementMultiplier = 0.7071067811865476;

        private readonly HashSet<VirtualKey> heldDirections = new HashSet<VirtualKey>();
        private readonly List<Enemy> enemies = new List<Enemy>();
        private readonly List<Projectile> projectiles = new List<Projectile>();
        private readonly List<Asteroid> asteroids = new List<Asteroid>();
        private readonly Dictionary<Enemy, EnemyMovementState> enemyMovementStates = new Dictionary<Enemy, EnemyMovementState>();
        private readonly Dictionary<Enemy, EnemyCombatState> enemyCombatStates = new Dictionary<Enemy, EnemyCombatState>();
        private readonly DispatcherTimer movementTimer;
        private readonly DispatcherTimer simulationTimer;
        private readonly Random random = new Random();

        private Player player;
        private int playerStartingHealth;
        private int currentLevel;
        private double boardWidth;
        private double boardHeight;
        private DateTimeOffset nextPlayerShotTime;
        private DateTimeOffset nextAsteroidSpawnTime;
        private SessionState state = SessionState.Menu;

        public GameSession()
        {
            movementTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            movementTimer.Tick += MovementTimer_Tick;
            simulationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            simulationTimer.Tick += SimulationTimer_Tick;
        }

        public event Action<UIElement> ElementAdded;
        public event Action<UIElement> ElementRemoved;
        public event Action BoardCleared;
        public event Action<Player> PlayerCreated;
        public event Action<int> LevelChanged;
        public event Action<string> Ended;

        public void SetBoardSize(double width, double height)
        {
            boardWidth = width;
            boardHeight = height;
        }

        public void Start()
        {
            ClearSession();
            state = SessionState.Playing;
            currentLevel = FirstLevel;
            nextPlayerShotTime = DateTimeOffset.MinValue;
            CreatePlayer();
            StartLevel();
            ScheduleNextAsteroidSpawn(DateTimeOffset.UtcNow);
            simulationTimer.Start();
        }

        public void ReturnToMenu()
        {
            ClearSession();
            state = SessionState.Menu;
        }

        public void HandleKeyDown(VirtualKey key)
        {
            if (state != SessionState.Playing)
                return;

            if (key == VirtualKey.Space)
            {
                CreatePlayerProjectile(DateTimeOffset.UtcNow);
                return;
            }

            if (IsDirection(key) && heldDirections.Add(key))
            {
                MovePlayer();
                movementTimer.Start();
            }
        }

        public void HandleKeyUp(VirtualKey key)
        {
            if (!IsDirection(key))
                return;

            heldDirections.Remove(key);
            if (heldDirections.Count == 0)
                movementTimer.Stop();
        }

        public void Dispose()
        {
            ReturnToMenu();
        }

        private void CreatePlayer()
        {
            GamePiece piece = GameLogic.CreatePiece("SpaceShips/PlayerShip1.png", 80, 400, 500);
            player = new Player(piece.Img);
            playerStartingHealth = player.Health;
            AddElement(player.Img);
            PlayerCreated?.Invoke(player);
        }

        private void StartLevel()
        {
            RemoveLevelEnemies();
            RemoveAllProjectiles();
            player.Health = playerStartingHealth;
            player.Lives = 3;
            LevelChanged?.Invoke(currentLevel);

            int increaseStage = (currentLevel - FirstLevel) / 2;
            CreateEnemyRow<LargeEnemy>("SpaceShips/EnemyShip2.png", BaseLargeEnemyCount + increaseStage * AdditionalEnemiesPerIncrease, LargeEnemyRowY);
            CreateEnemyRow<MediumEnemy>("SpaceShips/EnemyShip1.png", BaseMediumEnemyCount + increaseStage * AdditionalEnemiesPerIncrease, MediumEnemyRowY);
            CreateEnemyRow<SmallEnemy>("SpaceShips/EnemyShip1.png", BaseSmallEnemyCount + increaseStage * AdditionalEnemiesPerIncrease, SmallEnemyRowY);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            foreach (Enemy enemy in enemies)
            {
                enemyMovementStates.Add(enemy, new EnemyMovementState(now, EnemyDirectionDecisionMilliseconds));
                enemyCombatStates.Add(enemy, new EnemyCombatState(now.AddSeconds(random.Next(1, 3))));
            }
        }

        private void CreateEnemyRow<T>(string imageName, int count, int top) where T : Enemy
        {
            double layoutWidth = boardWidth > 0 ? boardWidth : DefaultBoardWidth;
            for (int index = 0; index < count; index++)
            {
                int left = (int)(layoutWidth * (index + 1) / (count + 1));
                Enemy enemy = CreateEnemy<T>(imageName, left, top);
                enemy.Move(-enemy.Img.Width / 2, 0);
                ApplyLevelScaling(enemy);
                enemies.Add(enemy);
                AddElement(enemy.Img);
            }
        }

        private Enemy CreateEnemy<T>(string imageName, int left, int top) where T : Enemy
        {
            GamePiece piece = GameLogic.CreatePiece(imageName, left, top);
            Enemy enemy = (Enemy)Activator.CreateInstance(typeof(T), piece.Img);
            enemy.Img.Width = enemy.Size;
            enemy.Img.Height = enemy.Size;
            return enemy;
        }

        private void ApplyLevelScaling(Enemy enemy)
        {
            int statIncreaseStage = currentLevel / 2;
            enemy.Health += statIncreaseStage * AdditionalHealthPerStatIncrease;
            enemy.Damage += statIncreaseStage * AdditionalDamagePerStatIncrease;
            enemy.FireRate = Math.Max(MinimumEnemyFireRateSeconds, enemy.FireRate - statIncreaseStage * FireRateReductionPerStatIncrease);
        }

        private void MovementTimer_Tick(object sender, object e)
        {
            if (state == SessionState.Playing)
                MovePlayer();
        }

        private void SimulationTimer_Tick(object sender, object e)
        {
            if (state != SessionState.Playing || boardWidth <= 0 || boardHeight <= 0)
                return;

            DateTimeOffset now = DateTimeOffset.UtcNow;
            UpdateEnemies(now);
            ReverseCollidingEnemies(now);
            MoveProjectiles();
            SpawnAndMoveAsteroids(now);
            ProcessCollisions();
        }

        private void UpdateEnemies(DateTimeOffset now)
        {
            foreach (Enemy enemy in enemies)
            {
                EnemyMovementState movementState = enemyMovementStates[enemy];
                UpdateEnemyDirection(movementState, now);
                MoveEnemyWithinBoard(enemy, movementState, now);

                EnemyCombatState combatState = enemyCombatStates[enemy];
                if (now >= combatState.NextShotTime)
                {
                    CreateEnemyProjectile(enemy);
                    combatState.NextShotTime = now.AddSeconds(enemy.FireRate);
                }
            }
        }

        private void UpdateEnemyDirection(EnemyMovementState movementState, DateTimeOffset now)
        {
            if (now < movementState.NextDirectionDecision)
                return;

            if (now < movementState.DirectionLockedUntil)
            {
                movementState.NextDirectionDecision = movementState.DirectionLockedUntil.AddMilliseconds(EnemyDirectionDecisionMilliseconds);
                return;
            }

            if (random.Next(3) == 0)
                movementState.Direction *= -1;

            movementState.NextDirectionDecision = now.AddMilliseconds(EnemyDirectionDecisionMilliseconds);
        }

        private void MoveEnemyWithinBoard(Enemy enemy, EnemyMovementState movementState, DateTimeOffset now)
        {
            double maximumLeft = boardWidth - enemy.Img.Width;
            double nextLeft = enemy.Position.Left + enemy.Speed * movementState.Direction;
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
                enemy.Move(enemy.Speed * movementState.Direction, 0);
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
            EnemyMovementState movementState = enemyMovementStates[enemy];
            movementState.Direction *= -1;
            movementState.DirectionLockedUntil = now.AddMilliseconds(EnemyCollisionDirectionLockMilliseconds);
            movementState.NextDirectionDecision = movementState.DirectionLockedUntil.AddMilliseconds(EnemyDirectionDecisionMilliseconds);
        }

        private void CreatePlayerProjectile(DateTimeOffset now)
        {
            if (now < nextPlayerShotTime)
                return;

            int left = (int)(player.Position.Left + (player.Img.Width - ProjectileSize) / 2);
            int top = (int)(player.Position.Top - ProjectileSize);
            GamePiece piece = GameLogic.CreatePiece("LazerBeams/BlueLazerBeam.png", ProjectileSize, left, top);
            Projectile projectile = new Projectile(piece.Img, 0, -18, player.Damage, true);
            projectiles.Add(projectile);
            AddElement(projectile.Img);
            nextPlayerShotTime = now.AddSeconds(player.FireRate);
        }

        private void CreateEnemyProjectile(Enemy enemy)
        {
            int left = (int)(enemy.Position.Left + (enemy.Img.Width - ProjectileSize) / 2);
            int top = (int)(enemy.Position.Top + enemy.Img.Height);
            GamePiece piece = GameLogic.CreatePiece("LazerBeams/RedLazerBeam.png", ProjectileSize, left, top);
            Projectile projectile = new Projectile(piece.Img, 0, 18, enemy.Damage, false);
            projectiles.Add(projectile);
            AddElement(projectile.Img);
        }

        private void MoveProjectiles()
        {
            for (int index = projectiles.Count - 1; index >= 0; index--)
            {
                Projectile projectile = projectiles[index];
                projectile.Move(projectile.VelocityX, projectile.VelocityY);
                if (IsOutsideBoard(projectile))
                {
                    projectiles.RemoveAt(index);
                    RemoveElement(projectile.Img);
                }
            }
        }

        private void SpawnAndMoveAsteroids(DateTimeOffset now)
        {
            if (now >= nextAsteroidSpawnTime)
            {
                int asteroidCount = GetAsteroidsPerSpawn();
                for (int count = 0; count < asteroidCount; count++)
                    CreateAsteroid();

                ScheduleNextAsteroidSpawn(now);
            }

            for (int index = asteroids.Count - 1; index >= 0; index--)
            {
                Asteroid asteroid = asteroids[index];
                asteroid.Move(asteroid.VelocityX, asteroid.VelocityY);
                if (IsOutsideBoard(asteroid))
                {
                    asteroids.RemoveAt(index);
                    RemoveElement(asteroid.Img);
                }
            }
        }

        private void CreateAsteroid()
        {
            int size = random.Next(MinimumAsteroidSize, MaximumAsteroidSize + 1);
            int left;
            int top;
            double velocityX;
            switch (random.Next(3))
            {
                case 0:
                    left = random.Next(0, Math.Max(1, (int)boardWidth - size + 1));
                    top = -size;
                    velocityX = GetAsteroidHorizontalVelocity();
                    break;
                case 1:
                    left = -size;
                    top = random.Next(0, Math.Max(1, (int)(boardHeight / 2.5)));
                    velocityX = random.Next(1, 4);
                    break;
                default:
                    left = (int)boardWidth;
                    top = random.Next(0, Math.Max(1, (int)(boardHeight / 2.5)));
                    velocityX = -random.Next(1, 4);
                    break;
            }

            string imageName = random.Next(2) == 0 ? "Astriods/Astriod1.png" : "Astriods/Astriod2.png";
            GamePiece piece = GameLogic.CreatePiece(imageName, size, left, top);
            Asteroid asteroid = new Asteroid(piece.Img)
            {
                VelocityX = velocityX,
                VelocityY = random.Next(2, 6)
            };
            asteroids.Add(asteroid);
            AddElement(asteroid.Img);
        }

        private int GetAsteroidsPerSpawn()
        {
            int choice = random.Next(10);
            int count = choice < 4 ? 1 : choice < 7 ? 2 : choice < 9 ? 3 : 4;
            return Math.Max(MinimumAsteroidsPerSpawn, Math.Min(count, MaximumAsteroidsPerSpawn));
        }

        private double GetAsteroidHorizontalVelocity()
        {
            int speed = random.Next(0, 6);
            return random.Next(2) == 0 ? -speed : speed;
        }

        private void ScheduleNextAsteroidSpawn(DateTimeOffset now)
        {
            double delay = MinimumAsteroidSpawnSeconds + random.NextDouble() * (MaximumAsteroidSpawnSeconds - MinimumAsteroidSpawnSeconds);
            nextAsteroidSpawnTime = now.AddSeconds(delay);
        }

        private void ProcessCollisions()
        {
            HashSet<Projectile> destroyedProjectiles = new HashSet<Projectile>();
            HashSet<Asteroid> destroyedAsteroids = new HashSet<Asteroid>();
            HashSet<Enemy> destroyedEnemies = new HashSet<Enemy>();

            foreach (Projectile projectile in projectiles)
            {
                if (projectile.IsPlayerProjectile && ProcessPlayerProjectileHit(projectile, destroyedProjectiles, destroyedEnemies))
                    continue;

                if (!projectile.IsPlayerProjectile && GameLogic.IsCollision(projectile, player))
                {
                    ApplyDamageToPlayer(projectile.Damage);
                    destroyedProjectiles.Add(projectile);
                    continue;
                }

                foreach (Asteroid asteroid in asteroids)
                {
                    if (destroyedAsteroids.Contains(asteroid) || !GameLogic.IsCollision(projectile, asteroid))
                        continue;

                    if (projectile.IsPlayerProjectile)
                        player.Score += asteroid.PointValue;

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

        private bool ProcessPlayerProjectileHit(Projectile projectile, ISet<Projectile> destroyedProjectiles, ISet<Enemy> destroyedEnemies)
        {
            foreach (Enemy enemy in enemies)
            {
                if (destroyedEnemies.Contains(enemy) || !GameLogic.IsCollision(projectile, enemy))
                    continue;

                enemy.Health -= player.Damage;
                destroyedProjectiles.Add(projectile);
                if (enemy.Health <= 0)
                {
                    player.Score += enemy.PointValue;
                    destroyedEnemies.Add(enemy);
                }

                return true;
            }

            return false;
        }

        private void ApplyDamageToPlayer(int damage)
        {
            int healthBeforeHit = player.Health;
            player.Health -= damage;
            if (healthBeforeHit <= 0 || player.Health != 0)
                return;

            if (player.Lives <= 1)
            {
                EndGame(SessionState.GameOver, "GAME OVER");
                return;
            }

            player.Lives--;
            player.Health = playerStartingHealth;
        }

        private void RemoveDestroyedProjectiles(IEnumerable<Projectile> destroyedProjectiles)
        {
            foreach (Projectile projectile in destroyedProjectiles)
            {
                projectiles.Remove(projectile);
                RemoveElement(projectile.Img);
            }
        }

        private void RemoveDestroyedAsteroids(IEnumerable<Asteroid> destroyedAsteroids)
        {
            foreach (Asteroid asteroid in destroyedAsteroids)
            {
                asteroids.Remove(asteroid);
                RemoveElement(asteroid.Img);
            }
        }

        private void RemoveDestroyedEnemies(IEnumerable<Enemy> destroyedEnemies)
        {
            foreach (Enemy enemy in destroyedEnemies)
            {
                enemies.Remove(enemy);
                enemyMovementStates.Remove(enemy);
                enemyCombatStates.Remove(enemy);
                RemoveElement(enemy.Img);
            }

            if (state == SessionState.Playing && enemies.Count == 0)
                AdvanceLevel();
        }

        private void AdvanceLevel()
        {
            if (currentLevel >= MaximumLevel)
            {
                EndGame(SessionState.Won, "YOU WIN!");
                return;
            }

            currentLevel++;
            StartLevel();
        }

        private void MovePlayer()
        {
            if (boardWidth <= 0 || boardHeight <= 0 || player == null)
                return;

            double horizontal = (heldDirections.Contains(VirtualKey.Right) ? 1 : 0) - (heldDirections.Contains(VirtualKey.Left) ? 1 : 0);
            double vertical = (heldDirections.Contains(VirtualKey.Down) ? 1 : 0) - (heldDirections.Contains(VirtualKey.Up) ? 1 : 0);
            if (horizontal != 0 && vertical != 0)
            {
                horizontal *= DiagonalMovementMultiplier;
                vertical *= DiagonalMovementMultiplier;
            }

            double maximumLeft = boardWidth - player.Img.Width;
            double minimumTop = boardHeight * PlayerMinimumBoardHeightPercentage;
            double maximumTop = boardHeight - player.Img.Height;
            if (maximumLeft < 0 || maximumTop < minimumTop)
                return;

            double newLeft = Math.Max(0, Math.Min(player.Position.Left + horizontal * player.Speed, maximumLeft));
            double newTop = Math.Max(minimumTop, Math.Min(player.Position.Top + vertical * player.Speed, maximumTop));
            player.Move(newLeft - player.Position.Left, newTop - player.Position.Top);
        }

        private bool IsOutsideBoard(GamePiece piece)
        {
            return piece.Position.Top + piece.Img.Height < 0
                || piece.Position.Left + piece.Img.Width < 0
                || piece.Position.Left > boardWidth
                || piece.Position.Top > boardHeight;
        }

        private void EndGame(SessionState endState, string title)
        {
            if (state != SessionState.Playing)
                return;

            state = endState;
            StopTimers();
            Ended?.Invoke(title);
        }

        private void ClearSession()
        {
            StopTimers();
            heldDirections.Clear();
            enemies.Clear();
            projectiles.Clear();
            asteroids.Clear();
            enemyMovementStates.Clear();
            enemyCombatStates.Clear();
            player = null;
            nextPlayerShotTime = DateTimeOffset.MinValue;
            BoardCleared?.Invoke();
        }

        private void RemoveLevelEnemies()
        {
            foreach (Enemy enemy in enemies)
                RemoveElement(enemy.Img);

            enemies.Clear();
            enemyMovementStates.Clear();
            enemyCombatStates.Clear();
        }

        private void RemoveAllProjectiles()
        {
            foreach (Projectile projectile in projectiles)
                RemoveElement(projectile.Img);

            projectiles.Clear();
        }

        private void StopTimers()
        {
            movementTimer.Stop();
            simulationTimer.Stop();
        }

        private void AddElement(UIElement element)
        {
            ElementAdded?.Invoke(element);
        }

        private void RemoveElement(UIElement element)
        {
            ElementRemoved?.Invoke(element);
        }

        private static bool IsDirection(VirtualKey key)
        {
            return key == VirtualKey.Up || key == VirtualKey.Down || key == VirtualKey.Left || key == VirtualKey.Right;
        }

        private enum SessionState
        {
            Menu,
            Playing,
            GameOver,
            Won
        }

        private sealed class EnemyMovementState
        {
            public EnemyMovementState(DateTimeOffset now, int directionDecisionMilliseconds)
            {
                NextDirectionDecision = now.AddMilliseconds(directionDecisionMilliseconds);
            }

            public int Direction { get; set; } = 1;
            public DateTimeOffset DirectionLockedUntil { get; set; }
            public DateTimeOffset NextDirectionDecision { get; set; }
        }

        private sealed class EnemyCombatState
        {
            public EnemyCombatState(DateTimeOffset nextShotTime)
            {
                NextShotTime = nextShotTime;
            }

            public DateTimeOffset NextShotTime { get; set; }
        }
    }
}
