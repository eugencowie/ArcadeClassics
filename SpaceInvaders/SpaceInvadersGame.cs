using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SpaceInvaders
{
    public class SpaceInvadersGame : Game
    {
        #region Constants

        // Width and height of the window.
        const int WINDOW_WIDTH = 800;
        const int WINDOW_HEIGHT = 600;

        // Padding between enemies.
        const int ENEMY_PADDING = 10;

        // Time (in seconds) between enemy movements.
        const float ENEMY_MOVE_INTERVAL = 1f;

        // Speed of the superenemy.
        const float SUPERENEMY_MOVE_SPEED = 50f;

        // Time (in seconds) before the player can fire another laser.
        const float PLAYER_LASER_COOLDOWN = 0.5f;

        #endregion
        #region Variables

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        // Fonts and sounds
        SpriteFont scoreFont;
        SpriteFont winFont;
        SoundEffect laserHitSound;

        // Background
        Texture2D backgroundTexture;

        // Player
        Sprite player;
        int playerLives;
        int playerScore;

        // Enemies
        List<Sprite> enemies = new List<Sprite>();
        float timeSinceEnemyMove;

        enum EnemyMovement { Left, Down, Right }
        EnemyMovement nextEnemyMovement = EnemyMovement.Right;

        // Superenemy
        Sprite superEnemy;

        // Lasers
        Texture2D laserTexture;
        List<Sprite> playerLasers = new List<Sprite>();
        List<Sprite> enemyLasers = new List<Sprite>();
        float timeSincePlayerLaser = PLAYER_LASER_COOLDOWN;

        // Barriers
        class Barrier : Sprite
        {
            private int health;
            public int Health
            {
                get { return health; }
                set {
                    Alive = (value > 0); // alive is true if health > 0
                    health = value;
                }
            }

            public Barrier(Texture2D texture, Vector2 position, int health)
                : base(texture, position)
            {
                Health = health;
                Alive = (health > 0);
            }
        }
        List<Barrier> barriers = new List<Barrier>();

        // Game over
        bool resetLevel;
        bool gameOver;
        bool playerWon;

        int highScore;
        bool newHighScore;

        Random rand = new Random();

        #endregion


        public SpaceInvadersGame()
        {
            graphics = new GraphicsDeviceManager(this) {
                PreferredBackBufferWidth = WINDOW_WIDTH,
                PreferredBackBufferHeight = WINDOW_HEIGHT
            };

            Content.RootDirectory = "Content";
        }


        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load the fonts and sound effects.
            scoreFont = Content.Load<SpriteFont>("fonts/ScoreFont");
            winFont = Content.Load<SpriteFont>("fonts/WinFont");
            laserHitSound = Content.Load<SoundEffect>("audio/LaserHit");

            // Load the background texture.
            backgroundTexture = Content.Load<Texture2D>("textures/background");
            
            // Create the player, horizontally centered and 40 pixels above the bottom of the screen.
            var playerTexture = Content.Load<Texture2D>("textures/player");
            var playerPosition = new Vector2(WINDOW_WIDTH / 2f, WINDOW_HEIGHT - 40);

            player = new Sprite(playerTexture, playerPosition);
            playerLives = 3;

            // Create the enemies in a 10x5 grid, offset from the top-left by (50px, 50px).
            var enemyTexture = Content.Load<Texture2D>("textures/enemy");
            var offset = new Vector2(50, 50);

            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    var enemyPosition = offset + new Vector2(
                        x * (enemyTexture.Width + ENEMY_PADDING),
                        y * (enemyTexture.Height + ENEMY_PADDING));

                    var enemy = new Sprite(enemyTexture, enemyPosition);
                    enemy.Color = Color.LimeGreen;

                    enemies.Add(enemy);
                }
            }

            // Create the superenemy.
            var superEnemyTexture = Content.Load<Texture2D>("textures/superenemy");
            superEnemy = new Sprite(superEnemyTexture, Vector2.Zero);
            superEnemy.Position = new Vector2(-40, 20);
#if !DEBUG
            superEnemy.Alive = false; // do not spawn the superenemy immediately in release builds
#endif

            // Load the laser texture to use later when we create lasers.
            laserTexture = Content.Load<Texture2D>("textures/laser");

            // Create four equidistant barriers, with 100px padding on each side of the screen.
            var barrierTexture = Content.Load<Texture2D>("textures/barrier");
            const int sidePadding = 100;
            const int barrierInterval = (WINDOW_WIDTH - (sidePadding*2)) / 4;

            for (int x = barrierInterval; x <= WINDOW_WIDTH - sidePadding; x += barrierInterval)
                barriers.Add(new Barrier(barrierTexture, new Vector2(x, 500), 100));

            // Load the current high score.
            if (File.Exists("highscore.txt"))
            {
                string text = File.ReadAllText("highscore.txt");
                try { int.TryParse(text, out highScore); }
                catch { highScore = 0; }
            }
        }


        protected override void Update(GameTime gameTime)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (resetLevel)
            {
                ResetLevel();
                resetLevel = false;
            }

            if (!gameOver)
            {
                // Check for player input.
                CheckPlayerInput(delta);

                // Update enemies.
                UpdateEnemyGrid(delta);

                UpdateSuperEnemy(delta);

                // Update alive player lasers.
                foreach (var laser in playerLasers.Where(laser => laser.Alive))
                    UpdatePlayerLaser(delta, laser);

                // Update alive enemy lasers.
                foreach (var laser in enemyLasers.Where(laser => laser.Alive))
                    UpdateEnemyLaser(delta, laser);

                // Update alive barriers.
                foreach (var barrier in barriers.Where(barrier => barrier.Alive))
                    UpdateBarrier(delta, barrier);

                // Remove any entities which have been marked as dead.
                enemies.RemoveAll(enemy => !enemy.Alive);
                playerLasers.RemoveAll(laser => !laser.Alive);
                enemyLasers.RemoveAll(laser => !laser.Alive);
                barriers.RemoveAll(barrier => !barrier.Alive);

                // Check if we have reached a game over condition.
                CheckGameOver(delta);
            }

            base.Update(gameTime);
        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            if (!gameOver)
            {
                // Draw the background.
                spriteBatch.Draw(backgroundTexture, new Rectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT), Color.White);

                // Draw the player.
                player.Draw(spriteBatch);

                // Draw the enemies.
                foreach (var enemy in enemies)
                    enemy.Draw(spriteBatch);

                superEnemy.Draw(spriteBatch);

                // Draw the player lasers.
                foreach (var laser in playerLasers)
                    laser.Draw(spriteBatch);

                // Draw the enemy lasers.
                foreach (var laser in enemyLasers)
                    laser.Draw(spriteBatch);

                // Draw the barriers.
                foreach (var barrier in barriers)
                {
                    Color color = Color.LimeGreen;
                    if (barrier.Health <= 75) color = Color.LightGreen;
                    if (barrier.Health <= 50) color = Color.Orange;
                    if (barrier.Health <= 25) color = Color.Red;

                    barrier.Color = color;
                    barrier.Draw(spriteBatch);
                }

                // Draw the player's lives, score and the high score.
                spriteBatch.DrawStringCentered(scoreFont, "Lives: " + playerLives, new Vector2(WINDOW_WIDTH/2f, 20), Color.White);
                spriteBatch.DrawString(scoreFont, "Score: " + playerScore, new Vector2(20, 20), Color.White);
                spriteBatch.DrawString(scoreFont, "High score: " + highScore, new Vector2(WINDOW_WIDTH - 130, 20), Color.White);
            }
            else
            {
                // If the game is over, tell the player whether they won or lost.
                spriteBatch.DrawString(winFont, string.Format("Game over with a score of {0}.", playerScore), new Vector2(20, 20), Color.White);

                string text = (playerWon ? "You won the game!" : "You died :(");
                spriteBatch.DrawString(winFont, text, new Vector2(20, 60), Color.White);

                if (newHighScore)
                    spriteBatch.DrawString(winFont, "New high score!", new Vector2(20, 100), Color.White);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }


        #region Update functions


        private void ResetLevel()
        {
            playerLasers.Clear();
            enemyLasers.Clear();

            superEnemy.Position = new Vector2(-40, 20);
            superEnemy.Alive = false;

            enemies.Clear();
            nextEnemyMovement = EnemyMovement.Right;

            var enemyTexture = Content.Load<Texture2D>("textures/enemy");
            var offset = new Vector2(50, 50);
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    var enemyPosition = offset + new Vector2(
                        x * (enemyTexture.Width + ENEMY_PADDING),
                        y * (enemyTexture.Height + ENEMY_PADDING));

                    var enemy = new Sprite(enemyTexture, enemyPosition);
                    enemy.Color = Color.LimeGreen;

                    enemies.Add(enemy);
                }
            }
        }


        private void CheckGameOver(float delta)
        {
            // Check if all enemies have been destroyed.
            if (enemies.Count == 0)
            {
                gameOver = true;
                playerWon = true;
                UpdateHighScore();
            }

            // Check if the lowest line of enemies have reached the player.
            else if (GetBottomMostEnemy().Bounds.Bottom > player.Bounds.Top)
            {
                gameOver = true;
                UpdateHighScore();
            }
        }


        private void UpdateHighScore()
        {
            if (playerScore > highScore)
            {
                File.WriteAllText("highscore.txt", playerScore.ToString());
                newHighScore = true;
            }
        }


        private void CheckPlayerInput(float delta)
        {
            KeyboardState keyboard = Keyboard.GetState();

            // Check if the player wants to move left.
            if (keyboard.IsKeyDown(Keys.Left) && player.Bounds.Left > 0)
                player.Position.X -= 300 * delta;

            // Check if the player wants to move right.
            if (keyboard.IsKeyDown(Keys.Right) && player.Bounds.Right < WINDOW_WIDTH)
                player.Position.X += 300 * delta;

            // Check if the player wants to fire a laser.
            if (keyboard.IsKeyDown(Keys.Space) && timeSincePlayerLaser > PLAYER_LASER_COOLDOWN)
            {
                var laserPosition = player.Position + new Vector2(0, -laserTexture.Height);
                playerLasers.Add(new Sprite(laserTexture, laserPosition));

                timeSincePlayerLaser = 0;
            }

            timeSincePlayerLaser += delta;
        }


        private void UpdateEnemyGrid(float delta)
        {
            // Make sure that there is at least one enemy to update.
            if (enemies.Count == 0)
                return;

            timeSinceEnemyMove += delta;

            // Enemies should move once every ENEMY_MOVE_INTERVAL seconds.
            if (timeSinceEnemyMove > ENEMY_MOVE_INTERVAL)
            {
                // enemy[0] should exist if enemies.Count != 0 (per if statement above).
                int moveX = enemies[0].Texture.Width + ENEMY_PADDING;
                int moveY = enemies[0].Texture.Height + ENEMY_PADDING;

                if (nextEnemyMovement == EnemyMovement.Down)
                {
                    // Move enemies on the y axis.
                    foreach (var enemy in enemies)
                        enemy.Position.Y += moveY;

                    // Check if the grid has reached the left side of the screen.
                    if (GetLeftMostEnemy().Position.X < Math.Abs(moveX * 2))
                        nextEnemyMovement = EnemyMovement.Right;

                    // Check if the grid has reached the right side of the screen.
                    if (GetRightMostEnemy().Position.X > WINDOW_WIDTH - Math.Abs(moveX * 2))
                        nextEnemyMovement = EnemyMovement.Left;
                }
                else
                {
                    // Flip the positive (right) movement if required.
                    if (nextEnemyMovement == EnemyMovement.Left)
                        moveX *= -1;

                    // Move enemies on the x axis.
                    foreach (var enemy in enemies)
                        enemy.Position.X += moveX;

                    // Check if the grid has reached the left or right side of the screen.
                    if (GetLeftMostEnemy().Position.X < Math.Abs(moveX*2) ||
                        GetRightMostEnemy().Position.X > WINDOW_WIDTH - Math.Abs(moveX*2))
                    {
                        nextEnemyMovement = EnemyMovement.Down;
                    }
                }

                timeSinceEnemyMove = 0;
            }

            // 1% chance of enemy laser fire.
            if (rand.Next(100) < 1)
            {
                // Randomly select an enemy and take their position on the X axis.
                int selectedColumn = (int)enemies[rand.Next(enemies.Count)].Position.X;

                // Get the bottom-most enemy in that column.
                Sprite selectedEnemy = GetBottomMostEnemyInColumn(selectedColumn);

                // Create a laser.
                enemyLasers.Add(new Sprite(laserTexture, selectedEnemy.Position));
            }
        }


        private void UpdateSuperEnemy(float delta)
        {
            if (superEnemy.Alive)
            {
                superEnemy.Position.X += SUPERENEMY_MOVE_SPEED * delta;

                // Check if the superenemy leaves the screen.
                if (superEnemy.Bounds.Right > WINDOW_WIDTH)
                    superEnemy.Alive = false;

                // 3% chance of firing a laser
                if (rand.Next(100) < 3)
                {
                    var laser = new Sprite(laserTexture, superEnemy.Position);
                    laser.Color = Color.MediumPurple;
                    enemyLasers.Add(laser);
                }
            }
            else
            {
                // 0.5% chance of superenemy spawning
                if (rand.Next(1000) < 5)
                {
                    superEnemy.Position = new Vector2(-40, 20);
                    superEnemy.Alive = true;
                }
            }
        }


        private void UpdatePlayerLaser(float delta, Sprite playerLaser)
        {
            // Player lasers move upwards.
            playerLaser.Position.Y -= 300 * delta;

            // Remove the laser when it reaches the top of the screen.
            if (playerLaser.Bounds.Bottom < 0)
            {
                playerLaser.Alive = false;
                return;
            }

            // Check if the laser hits a barrier.
            foreach (var barrier in barriers)
            {
                if (playerLaser.Bounds.Intersects(barrier.Bounds))
                {
                    barrier.Health -= 25;
                    playerLaser.Alive = false;
                    return;
                }
            }

            // Check if the laser collides with any enemy lasers.
            foreach (var enemyLaser in enemyLasers)
            {
                if (playerLaser.Bounds.Intersects(enemyLaser.Bounds))
                {
                    enemyLaser.Alive = false;
                    playerLaser.Alive = false;
                    return;
                }
            }

            // Check if the laser collides with any active enemies.
            foreach (var enemy in enemies)
            {
                if (playerLaser.Bounds.Intersects(enemy.Bounds))
                {
                    enemy.Alive = false;
                    playerLaser.Alive = false;
                    playerScore += 10;
                    laserHitSound.Play();
                    return;
                }
            }

            if (superEnemy.Alive && playerLaser.Bounds.Intersects(superEnemy.Bounds))
            {
                superEnemy.Alive = false;
                playerLaser.Alive = false;
                playerScore += 500;
                laserHitSound.Play();
                return;
            }
        }


        private void UpdateEnemyLaser(float delta, Sprite enemyLaser)
        {
            // Enemy lasers move downwards.
            enemyLaser.Position.Y += 300 * delta;

            // Check if the laser hits a barrier.
            foreach (var barrier in barriers)
            {
                if (enemyLaser.Bounds.Intersects(barrier.Bounds))
                {
                    barrier.Health -= 25;
                    enemyLaser.Alive = false;
                    return;
                }
            }

            // Check if the laser hits the player.
            if (enemyLaser.Bounds.Intersects(player.Bounds))
            {
                enemyLaser.Alive = false;
                playerLives--;

                if (playerLives > 0)
                    resetLevel = true;
                else
                {
                    player.Alive = false;
                    gameOver = true;
                    UpdateHighScore();
                }

                return;
            }

            // Remove the laser when it reaches the bottom of the screen.
            if (enemyLaser.Bounds.Top > WINDOW_HEIGHT)
            {
                enemyLaser.Alive = false;
                return;
            }
        }


        private void UpdateBarrier(float delta, Barrier barrier)
        {
            // Check if any of the enemies are colliding with the barrier.
            foreach (var enemy in enemies.Where(enemy => enemy.Alive))
            {
                if (barrier.Bounds.Intersects(enemy.Bounds))
                    barrier.Health = 0;
            }
        }


        #endregion


        #region Misc functions


        /// <summary>
        /// Get the x index of the left-most enemy still alive on the grid.
        /// </summary>
        private Sprite GetLeftMostEnemy()
        {
            Sprite result = null;

            foreach (var enemy in enemies)
            {
                if (result == null || enemy.Position.X < result.Position.X)
                    result = enemy;
            }

            return result;
        }


        /// <summary>
        /// Get the x index of the right-most enemy still alive on the grid.
        /// </summary>
        private Sprite GetRightMostEnemy()
        {
            Sprite result = null;

            foreach (var enemy in enemies)
            {
                if (result == null || enemy.Position.X > result.Position.X)
                    result = enemy;
            }

            return result;
        }


        /// <summary>
        /// Get the y index of the bottom-most enemy still alive on the grid.
        /// </summary>
        private Sprite GetBottomMostEnemy()
        {
            Sprite result = null;

            foreach (var enemy in enemies)
            {
                if (result == null || enemy.Position.Y > result.Position.Y)
                    result = enemy;
            }

            return result;
        }


        /// <summary>
        /// Get the y index of the bottom-most enemy still alive on the specified column of the grid.
        /// </summary>
        private Sprite GetBottomMostEnemyInColumn(int columnPositionX)
        {
            Sprite result = null;

            foreach (var enemy in enemies)
            {
                if (Math.Abs(enemy.Position.X - columnPositionX) < enemy.Texture.Width / 2f)
                    if (result == null || enemy.Position.Y > result.Position.Y)
                        result = enemy;
            }

            return result;
        }


        #endregion
    }

    static class Ext
    {
        public static void DrawStringCentered(this SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, Color color)
        {
            var absolutePosition = position - new Vector2(font.MeasureString(text).X / 2f, 0);
            spriteBatch.DrawString(font, text, absolutePosition, color);
        }
    }
}
