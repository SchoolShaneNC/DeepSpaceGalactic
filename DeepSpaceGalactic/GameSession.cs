using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        //constants for general game settings, enemy behavior, asteroid spawning, and level progression
        private const int EnemyDirectionDecisionMilliseconds = 1700;
        private const int EnemyCollisionDirectionLockMilliseconds = 2700;
        private const double MinimumAsteroidSpawnSeconds = 0.7;
        private const double MaximumAsteroidSpawnSeconds = 2.0;
        private const int MinimumAsteroidsPerSpawn = 1;
        private const int MaximumAsteroidsPerSpawn = 4;
        private const int AsteroidBeltLevel = 3;
        private const int AsteroidBeltDurationSeconds = 35;
        private const double MinimumAsteroidBeltSpawnSeconds = 0.3;
        private const double MaximumAsteroidBeltSpawnSeconds = 0.6;
        private const int MinimumAsteroidsPerBeltSpawn = 2;
        private const int MaximumAsteroidsPerBeltSpawn = 6;
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

        //constants for where each enemy type is placed at the start of a level
        private const int LargeEnemyRowY = 60;
        private const int MediumEnemyRowY = 120;
        private const int SmallEnemyRowY = 180;
        private const double DefaultBoardWidth = 1000;

        //constants used when creating projectiles and restricting player movement
        private const int ProjectileSize = 15;
        private const double PlayerMinimumBoardHeightPercentage = 0.5;
        private const double DiagonalMovementMultiplier = 0.7071067811865476;

        //collections that track active objects and which direction keys are currently held
        //HashSet prevents the same direction key from being added more than once
        private readonly HashSet<VirtualKey> heldDirections = new HashSet<VirtualKey>();
        private readonly List<Enemy> enemies = new List<Enemy>();
        private readonly List<Projectile> projectiles = new List<Projectile>();
        private readonly List<Asteroid> asteroids = new List<Asteroid>();

        //collections that track each enemy's movement and combat state, allowing independent behavior for each enemy
        private readonly Dictionary<Enemy, EnemyMovementState> enemyMovementStates = new Dictionary<Enemy, EnemyMovementState>();
        private readonly Dictionary<Enemy, EnemyCombatState> enemyCombatStates = new Dictionary<Enemy, EnemyCombatState>();

        //dispatcher timers update player movement and the main gameplay simulation at roughly 62 fps
        //combined the rest of the timers together for one main simulation loop to reduce the number of timers and simplify the code
        //runs the rest of the game updates 
        private readonly DispatcherTimer movementTimer;
        private readonly DispatcherTimer simulationTimer;
        private readonly Random random = new Random();

        //current session data / The starting health value is saved so health can be reset after a life is lost to proper level
        private Player player;
        private int playerStartingHealth;
        private int currentLevel;
        private double boardWidth;
        private double boardHeight;
        private DateTimeOffset nextPlayerShotTime;
        private DateTimeOffset nextAsteroidSpawnTime;
        private DateTimeOffset asteroidBeltEndsAt;
        private Stopwatch campaignTimer;
        private bool asteroidBeltActive;
        private bool asteroidBeltStarted;

        //use GameState enum to track the session lifecycle, starts in the menu state and changes to playing, won, or game over as the game session progresses
        private GameState state = GameState.Menu;

        #region Game Session and Input

        public GameSession()
        {
            //set up the timers once / they are started and stopped as the session changes state
            //subscribes both timers to tick event which triggers every 16ms
            movementTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            movementTimer.Tick += MovementTimer_Tick;

            simulationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            simulationTimer.Tick += SimulationTimer_Tick;
        }

        //events let MainPage add/remove images and update the HUD without owning gameplay rules
        public event Action<UIElement> ElementAdded;
        public event Action<UIElement> ElementRemoved;
        public event Action BoardCleared;
        public event Action<Player> PlayerCreated;
        public event Action<int> LevelChanged;
        public event Action<string> Ended;
        public event Action<CampaignCompletion> CampaignCompleted;

        public void SetBoardSize(double width, double height)
        {
            //MainPage supplies the playing areas current dimensions so movement and bounds checks use the actual board
            boardWidth = width;
            boardHeight = height;
        }

        public void Start()
        {
            //clear any previous session before initializing a new game
            ClearSession();
            state = GameState.Playing;
            currentLevel = FirstLevel;
            nextPlayerShotTime = DateTimeOffset.MinValue;
            //starts the timer for leaderboard if applicable 
            campaignTimer = Stopwatch.StartNew();

            //create the player first so later gameplay methods can use its stats and position
            //starts the level at the beginning, and configures the enemies/projectiles aswell as clears any enemies/projectiles
            CreatePlayer();
            StartLevel();

            //schedule the first asteroid spawn and start the main simulation loop
            ScheduleNextAsteroidSpawn(DateTimeOffset.UtcNow);
            simulationTimer.Start();
        }

        public void ReturnToMenu()
        {
            //stop gameplay and clear active objects when returning to the menu
            ClearSession();
            state = GameState.Menu;
        }

        public void HandleKeyDown(VirtualKey key)
        {
            //movement starts on key down and continues while the key is held by adding it to the collection
            //ignores gameplay inputs unless a game is actively being played
            if (state != GameState.Playing)
                return;

            //space fires immediately when pressed / the player fire rate check prevents shooting too fast, also leaves the door open
            //for future upgrades like power ups that allow faster fire rate / fire rate difficulty adjustment
            if (key == VirtualKey.Space)
            {
                CreatePlayerProjectile(DateTimeOffset.UtcNow);
                return;
            }

            //adds a direction / move immediately / then keep moving while the key remains held
            //hashset prevents douplicate direction keys from being added, so movement continues while any direction key is held
            if (IsDirection(key) && heldDirections.Add(key))
            {
                //moves the player before timer starts so the player does not have to wait for the first timer tick to move
                MovePlayer();
                movementTimer.Start();
            }
        }

        public void HandleKeyUp(VirtualKey key)
        {
            //validation to ensure the key released is a direction key otherwise ignore it
            if (!IsDirection(key))
                return;

            //remove the released key, stop the movement timer when no direction keys remain held
            heldDirections.Remove(key);

            //stops the timer when no direction keys are held
            //helps performace since the movement timer ticks every 16ms and executes movement validation in this case
            if (heldDirections.Count == 0)
                movementTimer.Stop();
        }

        public void Dispose()
        {
            //clean up the session and stop its timers when the mainpage is disposed/unloaded
            ReturnToMenu();
        }

        #endregion

        #region Player and Enemy Setup

        private void CreatePlayer()
        {
            //create the player image through GameLogic, then wrap it in the Player game object
            GamePiece piece = GameLogic.CreatePiece("SpaceShips/PlayerShip1.png", 80, 400, 500);
            player = new Player(piece.Img);

            //save the original health so it can be restored when the player loses a life
            //adds player to the mainpage and invokes the PlayerCreated event so the HUD can bind to the player object (in mainpage)
            playerStartingHealth = player.Health;
            AddElement(player.Img);
            PlayerCreated?.Invoke(player);
        }

        private void StartLevel()
        {
            //remove any enemies and projectiles left from the previous level before creating the next one
            RemoveLevelEnemies();
            RemoveAllProjectiles();

            //reset health and lives at the beginning of each level and invoke the LevelChanged event so the HUD can update the level display (in mainpage)
            player.Health = playerStartingHealth;
            player.Lives = 3;
            LevelChanged?.Invoke(currentLevel);

            //every two levels, add one enemy of each type, integer division means increases occur on levels 3 and 5
            //with this expression the first level uses the base enemy counts
            int increaseStage = (currentLevel - FirstLevel) / 2;

            //large enemies are placed at the top, medium enemies in the middle, and small enemies near the bottom, defined by their set rows
            CreateEnemyRow<LargeEnemy>("SpaceShips/EnemyShip2.png", BaseLargeEnemyCount + increaseStage * AdditionalEnemiesPerIncrease, LargeEnemyRowY);
            CreateEnemyRow<MediumEnemy>("SpaceShips/EnemyShip1.png", BaseMediumEnemyCount + increaseStage * AdditionalEnemiesPerIncrease, MediumEnemyRowY);
            CreateEnemyRow<SmallEnemy>("SpaceShips/EnemyShip1.png", BaseSmallEnemyCount + increaseStage * AdditionalEnemiesPerIncrease, SmallEnemyRowY);

            //takes the current time and uses it to initialize each enemy's movement and combat state
            DateTimeOffset now = DateTimeOffset.UtcNow;
            foreach (Enemy enemy in enemies)
            {
                //give each enemy an independent direction decision schedule, although their decision time is the same all around
                //keeps each enemy from switching directions at the same time
                enemyMovementStates.Add(enemy, new EnemyMovementState(now, EnemyDirectionDecisionMilliseconds));

                //randomize the first shot so all enemies do not fire at exactly the same time and all at once to start
                //after the initial shot , each enemys next shot is based on its own fire rate, so they will continue to fire independently
                enemyCombatStates.Add(enemy, new EnemyCombatState(random.Next(1, 3)));
            }
        }

        private void CreateEnemyRow<T>(string imageName, int count, int top) where T : Enemy
        {
            //use the actual board width when available otherwise use a fallback width for initial layout
            //this spaces each enemy evenly across the row
            double layoutWidth = boardWidth > 0 ? boardWidth : DefaultBoardWidth;

            //create the requested number of enemies in this row
            for (int index = 0; index < count; index++)
            {
                //calculate an evenly spaced horizontal position
                //(index + 1) and (count + 1) leave equal margins at both sides of the board
                int left = (int)(layoutWidth * (index + 1) / (count + 1));
                Enemy enemy = CreateEnemy<T>(imageName, left, top);

                //the calculated position marks the center point, so shift the image left by half its width.
                enemy.Move(-enemy.Img.Width / 2, 0);

                //apply the level scaling to the enemys stats, then add it to the active enemies collection and display its image
                ApplyLevelScaling(enemy);
                enemies.Add(enemy);
                AddElement(enemy.Img);
            }
        }

        private Enemy CreateEnemy<T>(string imageName, int left, int top) where T : Enemy
        {
            //create the image, then use T to instantiate the requested derived enemy class
            GamePiece piece = GameLogic.CreatePiece(imageName, left, top);

            //Activator.CreateInstance creates instance of enemy class based on type T
            //allows creation of different types of enemies (SmallEnemy, MediumEnemy, LargeEnemy) without having to write separate code for each type
            Enemy enemy = (Enemy)Activator.CreateInstance(typeof(T), piece.Img);

            //size the image using the Size property defined by the enemy type
            enemy.Img.Width = enemy.Size;
            enemy.Img.Height = enemy.Size;

            return enemy;
        }

        private void ApplyLevelScaling(Enemy enemy)
        {
            //increase enemy stats in stages as the level rises
            int statIncreaseStage = currentLevel / 2;

            //health goes up by 1 (which is the const) and damage goes up by 15 (which is the const)
            enemy.Health += statIncreaseStage * AdditionalHealthPerStatIncrease;
            enemy.Damage += statIncreaseStage * AdditionalDamagePerStatIncrease;

            //reduces the fire rate of enemy by set amount each stat increase
            //minimum fire rate is a safety net so it doesnt get too fast/negative
            enemy.FireRate = Math.Max(MinimumEnemyFireRateSeconds, enemy.FireRate - statIncreaseStage * FireRateReductionPerStatIncrease);
        }

        #endregion

        #region Timer Ticks

        private void MovementTimer_Tick(object sender, object e)
        {
            //if games going move player on timer tick
            if (state == GameState.Playing)
                MovePlayer();
        }

        private void SimulationTimer_Tick(object sender, object e)
        {
            //do not simulate gameplay if the session is not active or the board has not been measured yet
            if (state != GameState.Playing || boardWidth <= 0 || boardHeight <= 0)
                return;

            //get the current time once per tick so all updates use the same reference point for timing
            DateTimeOffset now = DateTimeOffset.UtcNow;

            UpdateAsteroidBelt(now);

            //update enemy movement and firing, then move projectiles and asteroids before checking collisions
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
                //use the movement state associated with this enemy to decide whether its direction should change
                EnemyMovementState movementState = enemyMovementStates[enemy];
                UpdateEnemyDirection(movementState, now);
                MoveEnemyWithinBoard(enemy, movementState, now);

                //each enemy has a separate firing schedule based on its own FireRate property
                EnemyCombatState combatState = enemyCombatStates[enemy];

                if (now >= combatState.NextShotTime)
                {
                    CreateEnemyProjectile(enemy);
                    combatState.NextShotTime = now.AddSeconds(enemy.FireRate);
                }
            }
        }

        #endregion

        #region Enemy Movement

        private void UpdateEnemyDirection(EnemyMovementState movementState, DateTimeOffset now)
        {
            //wait until this enemys next direction decision is due
            if (now < movementState.NextDirectionDecision)
                return;

            //if the enemy recently collided, keep its direction locked and schedule the next decision after the lock
            if (now < movementState.DirectionLockedUntil)
            {
                movementState.NextDirectionDecision = movementState.DirectionLockedUntil.AddMilliseconds(EnemyDirectionDecisionMilliseconds);
                return;
            }

            //randomly change direction with a 1/3 chance otherwise keep moving the same way
            if (random.Next(3) == 0)
                movementState.Direction *= -1;

            movementState.NextDirectionDecision = now.AddMilliseconds(EnemyDirectionDecisionMilliseconds);
        }

        private void MoveEnemyWithinBoard(Enemy enemy, EnemyMovementState movementState, DateTimeOffset now)
        {
            //keep the enemy inside the horizontal board limits and reverse its direction when it reaches an edge
            double maximumLeft = boardWidth - enemy.Img.Width;
            double nextLeft = enemy.Position.Left + enemy.Speed * movementState.Direction;

            //checks if the next position will be too far, if so it has hit the wall
            //when an enemy hits the wall their position is adjusted to be exactly at the edge and their direction is reversed
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
                //if no edge is reached, move according to the enemy's speed and current direction.
                enemy.Move(enemy.Speed * movementState.Direction, 0);
            }
        }

        private void ReverseCollidingEnemies(DateTimeOffset now)
        {
            //compare each enemy with the others. the second loop starts after the first enemy to avoid duplicate pairs
            for (int first = 0; first < enemies.Count - 1; first++)
            {
                for (int second = first + 1; second < enemies.Count; second++)
                {
                    if (GameLogic.IsCollision(enemies[first], enemies[second]))
                    {
                        //reverse both enemies when they collide, then temporarily lock their new directions
                        ReverseEnemy(enemies[first], now);
                        ReverseEnemy(enemies[second], now);
                    }
                }
            }
        }

        private void ReverseEnemy(Enemy enemy, DateTimeOffset now)
        {
            //reverse the enemy's current horizontal direction and apply the collision direction lock
            EnemyMovementState movementState = enemyMovementStates[enemy];
            movementState.Direction *= -1;
            movementState.DirectionLockedUntil = now.AddMilliseconds(EnemyCollisionDirectionLockMilliseconds);

            movementState.NextDirectionDecision = movementState.DirectionLockedUntil.AddMilliseconds(EnemyDirectionDecisionMilliseconds);
        }

        #endregion

        #region Projectiles

        private void CreatePlayerProjectile(DateTimeOffset now)
        {
            //check the fire rate timer so the player cannot shoot again before the allowed delay has passed
            if (now < nextPlayerShotTime)
                return;

            //position the projectile in the horizontal center of the player and just above the player's image
            int left = (int)(player.Position.Left + (player.Img.Width - ProjectileSize) / 2);
            int top = (int)(player.Position.Top - ProjectileSize);

            GamePiece piece = GameLogic.CreatePiece("LazerBeams/BlueLazerBeam.png", ProjectileSize, left, top);

            //player projectiles move upward and use the players current Damage value
            Projectile projectile = new Projectile(piece.Img, 0, -18, player.Damage, true);
            projectiles.Add(projectile);
            AddElement(projectile.Img);

            //schedule the next allowed shot based on the player's FireRate
            nextPlayerShotTime = now.AddSeconds(player.FireRate);
        }

        private void CreateEnemyProjectile(Enemy enemy)
        {
            //create an enemy projectile centered beneath the enemy so it travels down toward the player
            int left = (int)(enemy.Position.Left + (enemy.Img.Width - ProjectileSize) / 2);
            int top = (int)(enemy.Position.Top + enemy.Img.Height);

            GamePiece piece = GameLogic.CreatePiece("LazerBeams/RedLazerBeam.png", ProjectileSize, left, top);

            //enemy projectiles move downward and use the firing enemys Damage value
            Projectile projectile = new Projectile(piece.Img, 0, 18, enemy.Damage, false);
            projectiles.Add(projectile);
            AddElement(projectile.Img);
        }

        private void MoveProjectiles()
        {
            //iterate backwards so projectiles can be removed from the list without shifting unprocessed indexes
            for (int index = projectiles.Count - 1; index >= 0; index--)
            {
                Projectile projectile = projectiles[index];
                projectile.Move(projectile.VelocityX, projectile.VelocityY);

                //remove projectiles that have moved beyond any edge of the board
                if (IsOutsideBoard(projectile))
                {
                    projectiles.RemoveAt(index);
                    RemoveElement(projectile.Img);
                }
            }
        }

        #endregion

        #region Asteroids

        private void SpawnAndMoveAsteroids(DateTimeOffset now)
        {
            //spawn a random sized batch when the next scheduled asteroid spawn time is reached
            if (now >= nextAsteroidSpawnTime)
            {
                int asteroidCount = GetAsteroidsPerSpawn();

                for (int count = 0; count < asteroidCount; count++)
                    CreateAsteroid();

                //schedule the following batch after creating this one
                ScheduleNextAsteroidSpawn(now);
            }

            //move existing asteroids and remove those that have gone outside the board
            //iterate backwards so removing an asteroid does not affect the remaining indexes
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
            //randomize the asteroid's size within the configured minimum and maximum
            //asteroids can enter from the top, left, or right edge of the board
            int size = random.Next(MinimumAsteroidSize, MaximumAsteroidSize + 1);
            int left;
            int top;
            double velocityX;

            //top spawns move downward with a random horizontal velocity
            //side spawns begin just outside the board and move inward,  their vertical position is kept near the upper area
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

            //randomly select one of the asteroid images, then create the asteroid game object
            string imageName = random.Next(2) == 0 ? "Astriods/Astriod1.png" : "Astriods/Astriod2.png";

            GamePiece piece = GameLogic.CreatePiece(imageName, size, left, top);

            Asteroid asteroid = new Asteroid(piece.Img)
            {
                //store horizontal and vertical speeds so each asteroid can travel on a different path
                VelocityX = velocityX,
                VelocityY = random.Next(2, 6)
            };

            //track the asteroid in the collection and ask MainPage to display its image
            asteroids.Add(asteroid);
            AddElement(asteroid.Img);
        }

        private int GetAsteroidsPerSpawn()
        {
            if (asteroidBeltActive)
                return random.Next(MinimumAsteroidsPerBeltSpawn, MaximumAsteroidsPerBeltSpawn + 1);

            //use weighted random choices: one and two asteroids are common, three are less common, and four are occasional
            int choice = random.Next(10);
            int count = choice < 4 ? 1 : choice < 7 ? 2 : choice < 9 ? 3 : 4;

            //clamp the result to the configured minimum and maximum batch sizes
            return Math.Max(MinimumAsteroidsPerSpawn, Math.Min(count, MaximumAsteroidsPerSpawn));
        }

        private double GetAsteroidHorizontalVelocity()
        {
            //randomize the horizontal speed and direction so top spawning asteroids do not all follow the same path
            int speed = random.Next(0, 6);
            return random.Next(2) == 0 ? -speed : speed;
        }

        private void ScheduleNextAsteroidSpawn(DateTimeOffset now)
        {
            //pick a delay between the configured minimum and maximum spawn intervals
            double minimumSpawnSeconds = asteroidBeltActive ? MinimumAsteroidBeltSpawnSeconds : MinimumAsteroidSpawnSeconds;
            double maximumSpawnSeconds = asteroidBeltActive ? MaximumAsteroidBeltSpawnSeconds : MaximumAsteroidSpawnSeconds;
            double delay = minimumSpawnSeconds + random.NextDouble() * (maximumSpawnSeconds - minimumSpawnSeconds);
            nextAsteroidSpawnTime = now.AddSeconds(delay);
        }

        #endregion

        #region Collision and After Collision Damage/Removal

        private void ProcessCollisions()
        {
            //HashSets record objects that should be removed after collision checks finish
            //delaying removal avoids changing the collections while they are being iterated
            HashSet<Projectile> destroyedProjectiles = new HashSet<Projectile>();
            HashSet<Asteroid> destroyedAsteroids = new HashSet<Asteroid>();
            HashSet<Enemy> destroyedEnemies = new HashSet<Enemy>();

            foreach (Projectile projectile in projectiles)
            {
                //player projectiles first check for enemy hits. If one hits, it is handled and cannot hit another object
                if (projectile.IsPlayerProjectile && ProcessPlayerProjectileHit(projectile, destroyedProjectiles, destroyedEnemies))
                {
                    continue;
                }

                //enemy projectiles that collide with the player apply damage and are marked for removal
                if (!projectile.IsPlayerProjectile && GameLogic.IsCollision(projectile, player))
                {
                    ApplyDamageToPlayer(projectile.Damage);
                    destroyedProjectiles.Add(projectile);
                    continue;
                }

                //check projectiles against asteroids that have not already been marked as destroyed
                foreach (Asteroid asteroid in asteroids)
                {
                    if (destroyedAsteroids.Contains(asteroid) || !GameLogic.IsCollision(projectile, asteroid))
                    {
                        continue;
                    }

                    //the player earns asteroid points when a player projectile destroys one
                    if (projectile.IsPlayerProjectile) player.Score += asteroid.PointValue;

                    destroyedProjectiles.Add(projectile);
                    destroyedAsteroids.Add(asteroid);
                    break;
                }
            }

            //check direct player to asteroid collisions after projectile collisions have been processed
            foreach (Asteroid asteroid in asteroids)
            {
                if (!destroyedAsteroids.Contains(asteroid) && GameLogic.IsCollision(player, asteroid))
                {
                    ApplyDamageToPlayer(asteroid.Damage);
                    destroyedAsteroids.Add(asteroid);
                }
            }

            //remove marked objects from their collections and notify MainPage to remove their images
            RemoveDestroyedProjectiles(destroyedProjectiles);
            RemoveDestroyedAsteroids(destroyedAsteroids);
            RemoveDestroyedEnemies(destroyedEnemies);
        }

        private bool ProcessPlayerProjectileHit(Projectile projectile, ISet<Projectile> destroyedProjectiles, ISet<Enemy> destroyedEnemies)
        {
            //check each enemy until this player projectile hits one that has not already been destroyed
            foreach (Enemy enemy in enemies)
            {
                if (destroyedEnemies.Contains(enemy) || !GameLogic.IsCollision(projectile, enemy))
                {
                    continue;
                }

                //apply player damage and mark the projectile so it cannot hit another enemy
                enemy.Health -= player.Damage;
                destroyedProjectiles.Add(projectile);

                if (enemy.Health <= 0)
                {
                    //add the enemy's point value to the player's score when its health reaches zero
                    player.Score += enemy.PointValue;
                    destroyedEnemies.Add(enemy);
                }

                return true;
            }

            //return false when the projectile did not hit an enemy, allowing other collision checks to run
            return false;
        }

        private void ApplyDamageToPlayer(int damage)
        {
            //save health before applying damage so the code can detect the moment health reaches zero
            int healthBeforeHit = player.Health;
            player.Health -= damage;

            //only handle a life loss when this hit takes the player from positive health to zero
            if (healthBeforeHit <= 0 || player.Health != 0)
                return;

            //if this was the last life, end the session; otherwise remove one life and restore starting health
            if (player.Lives <= 1)
            {
                EndGame(GameState.GameOver, "GAME OVER");
                return;
            }

            player.Lives--;
            player.Health = playerStartingHealth;
        }

        private void RemoveDestroyedProjectiles(IEnumerable<Projectile> destroyedProjectiles)
        {
            //remove each projectile from the active collection and ask MainPage to remove its image
            foreach (Projectile projectile in destroyedProjectiles)
            {
                projectiles.Remove(projectile);
                RemoveElement(projectile.Img);
            }
        }

        private void RemoveDestroyedAsteroids(IEnumerable<Asteroid> destroyedAsteroids)
        {
            //remove destroyed asteroids from the active collection and the displayed board
            foreach (Asteroid asteroid in destroyedAsteroids)
            {
                asteroids.Remove(asteroid);
                RemoveElement(asteroid.Img);
            }
        }

        private void RemoveDestroyedEnemies(IEnumerable<Enemy> destroyedEnemies)
        {
            //remove each destroyed enemy and its movement/combat state from their respective collections
            foreach (Enemy enemy in destroyedEnemies)
            {
                enemies.Remove(enemy);
                enemyMovementStates.Remove(enemy);
                enemyCombatStates.Remove(enemy);
                RemoveElement(enemy.Img);
            }

            //advance only while the game is active and all enemies for this level have been defeated
            if (state == GameState.Playing && !asteroidBeltActive && enemies.Count == 0)
                AdvanceLevel();
        }

        #endregion

        #region Level Progression and Player Movement

        private void AdvanceLevel()
        {
            if (currentLevel == AsteroidBeltLevel && !asteroidBeltStarted)
            {
                StartAsteroidBelt();
                return;
            }

            //the player wins after completing the final configured level
            if (currentLevel >= MaximumLevel)
            {
                EndGame(GameState.Won, "YOU WIN!");
                return;
            }

            //otherwise increase the level and create the next level's enemies
            currentLevel++;
            StartLevel();
        }

        private void StartAsteroidBelt()
        {
            //after level 3 player enters asteriod belt where an increase of asteriods and no enemies happens for 35 seconds
            asteroidBeltStarted = true;
            asteroidBeltActive = true;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            asteroidBeltEndsAt = now.AddSeconds(AsteroidBeltDurationSeconds);
            nextAsteroidSpawnTime = now;
        }

        private void UpdateAsteroidBelt(DateTimeOffset now)
        {
            //this is where the asteriod belt ends after the 35 seconds and advances level as usual
            //levels and scaling stays the same as v1 just with basically an interuption between levels 3-4
            if (!asteroidBeltActive || now < asteroidBeltEndsAt)
                return;

            asteroidBeltActive = false;
            ScheduleNextAsteroidSpawn(now);
            AdvanceLevel();
        }

        private void MovePlayer()
        {
            //do not move until the board has valid dimensions and the player has been created
            if (boardWidth <= 0 || boardHeight <= 0 || player == null)
                return;

            //convert held direction keys into horizontal and vertical movement values
            double horizontal = (heldDirections.Contains(VirtualKey.Right) ? 1 : 0)
                - (heldDirections.Contains(VirtualKey.Left) ? 1 : 0);

            double vertical = (heldDirections.Contains(VirtualKey.Down) ? 1 : 0)
                - (heldDirections.Contains(VirtualKey.Up) ? 1 : 0);

            //reduce diagonal movement so moving in two directions at once does not increase overall speed
            if (horizontal != 0 && vertical != 0)
            {
                horizontal *= DiagonalMovementMultiplier;
                vertical *= DiagonalMovementMultiplier;
            }

            //the player is restricted to the lower half of the board and cannot move beyond any edge
            double maximumLeft = boardWidth - player.Img.Width;
            double minimumTop = boardHeight * PlayerMinimumBoardHeightPercentage;
            double maximumTop = boardHeight - player.Img.Height;

            if (maximumLeft < 0 || maximumTop < minimumTop)
                return;

            //clamp the new coordinates to the permitted area, then move by the difference from the current position
            double newLeft = Math.Max(0, Math.Min(player.Position.Left + horizontal * player.Speed, maximumLeft));
            double newTop = Math.Max(minimumTop, Math.Min(player.Position.Top + vertical * player.Speed, maximumTop));

            player.Move(newLeft - player.Position.Left, newTop - player.Position.Top);
        }

        private bool IsOutsideBoard(GamePiece piece)
        {
            //return true if the object's image has moved completely beyond any edge of the playing board
            return piece.Position.Top + piece.Img.Height < 0
                || piece.Position.Left + piece.Img.Width < 0
                || piece.Position.Left > boardWidth
                || piece.Position.Top > boardHeight;
        }

        #endregion

        #region Game Over and Session Cleanup

        private void EndGame(GameState endState, string title)
        {
            //prevent duplicate end-game events if the session has already stopped playing
            if (state != GameState.Playing)
                return;

            //save the final state, stop all timers, and notify MainPage to show the appropriate overlay
            //makes the campaigncompletion null for saftey and only gets instanctiated when game is won
            state = endState;
            StopTimers();
            CampaignCompletion completion = null;

            if (campaignTimer != null)
            {
                campaignTimer.Stop();

                if (endState == GameState.Won)
                {
                    int timeTaken = (int)Math.Ceiling(campaignTimer.Elapsed.TotalSeconds);
                    completion = new CampaignCompletion(timeTaken, player.Score, currentLevel, DateTimeOffset.Now);
                }
            }

            Ended?.Invoke(title);

            if (completion != null)
                CampaignCompleted?.Invoke(completion);
        }

        private void ClearSession()
        {
            //stop active loops and clear all gameplay collections before starting over or returning to the menu
            StopTimers();
            heldDirections.Clear();
            enemies.Clear();
            projectiles.Clear();
            asteroids.Clear();
            enemyMovementStates.Clear();
            enemyCombatStates.Clear();
            player = null;
            nextPlayerShotTime = DateTimeOffset.MinValue;
            nextAsteroidSpawnTime = DateTimeOffset.MinValue;
            asteroidBeltEndsAt = DateTimeOffset.MinValue;
            campaignTimer = null;
            asteroidBeltActive = false;
            asteroidBeltStarted = false;

            //tell MainPage to clear the displayed playing area as well
            BoardCleared?.Invoke();
        }

        private void RemoveLevelEnemies()
        {
            //remove the previous level's enemy images before clearing enemy and state collections
            foreach (Enemy enemy in enemies)
                RemoveElement(enemy.Img);

            enemies.Clear();
            enemyMovementStates.Clear();
            enemyCombatStates.Clear();
        }

        private void RemoveAllProjectiles()
        {
            //remove any projectiles left from the previous level from the board and active collection
            foreach (Projectile projectile in projectiles)
                RemoveElement(projectile.Img);

            projectiles.Clear();
        }

        private void StopTimers()
        {
            //stop both timers so no movement or simulation updates continue outside active gameplay
            movementTimer.Stop();
            simulationTimer.Stop();
        }

        #endregion

        #region UI Events and Helpers

        private void AddElement(UIElement element)
        {
            //send the new image to MainPage; the session does not directly manipulate the page's visual tree
            ElementAdded?.Invoke(element);
        }

        private void RemoveElement(UIElement element)
        {
            //ask MainPage to remove an image from the playing area
            ElementRemoved?.Invoke(element);
        }

        private static bool IsDirection(VirtualKey key)
        {
            //return true only for the four direction keys used for player movement 
            return key == VirtualKey.Up
                || key == VirtualKey.Down
                || key == VirtualKey.Left
                || key == VirtualKey.Right;
        }

        #endregion
    }
}
