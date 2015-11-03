using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace SpaceInvaders
{
    public class SpaceInvadersGame : Game
    {
        #region Variables

        // Width and height of the window.
        const int WINDOW_WIDTH = 800;
        const int WINDOW_HEIGHT = 600;

        // Padding between enemies.
        const int ENEMY_PADDING = 10;

        // Time (in seconds) between enemy movements.
        const int ENEMY_MOVE_INTERVAL = 1;

        // Time (in seconds) before the player can fire another laser.
        const float PLAYER_LASER_COOLDOWN = 0.5f;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        SpriteFont scoreFont;
        SpriteFont winFont;

        // Background
        Texture2D backgroundTexture;

        // Player
        Texture2D playerTexture;
        Vector2 playerPosition;
        int playerScore = 0;

        // Enemies
        Texture2D enemyTexture;
        Vector2 enemyGridOffset = new Vector2(50, 50);
        bool[,] enemyGrid = new bool[10, 5];
        float timeSinceEnemyMove = 0;

        enum EnemyMovement { Left, Down, Right }
        EnemyMovement nextEnemyMovement = EnemyMovement.Right;

        // Lasers
        Texture2D laserTexture;
        List<Vector2> playerLasers = new List<Vector2>();
        List<Vector2> enemyLasers = new List<Vector2>();
        float timeSincePlayerLaser = PLAYER_LASER_COOLDOWN;

        // Game over
        bool gameOver = false;
        bool playerWon = false;

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

            // Load the content.
            backgroundTexture = Content.Load<Texture2D>("textures/background");
            playerTexture = Content.Load<Texture2D>("textures/player");
            enemyTexture = Content.Load<Texture2D>("textures/enemy");
            laserTexture = Content.Load<Texture2D>("textures/laser");

            scoreFont = Content.Load<SpriteFont>("fonts/ScoreFont");
            winFont = Content.Load<SpriteFont>("fonts/WinFont");

            // Set the initial position of the player.
            playerPosition = new Vector2(WINDOW_WIDTH / 2f, WINDOW_HEIGHT - 40f);

            // Set all enemies to be active.
            for (int y = 0; y < enemyGrid.GetLength(1); y++)
                for (int x = 0; x < enemyGrid.GetLength(0); x++)
                    enemyGrid[x, y] = true;
        }


        protected override void Update(GameTime gameTime)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                this.Exit();

            if (!gameOver)
            {
                CheckGameOver(delta);
                CheckPlayerInput(delta);
                UpdateEnemies(delta);
                UpdateLasers(delta);
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
                spriteBatch.DrawCentered(playerTexture, playerPosition);

                // Draw the enemies.
                for (int y = 0; y < enemyGrid.GetLength(1); y++)
                {
                    for (int x = 0; x < enemyGrid.GetLength(0); x++)
                    {
                        if (enemyGrid[x, y])
                        {
                            Vector2 enemyPosition = enemyGridOffset + new Vector2(
                                x * (enemyTexture.Width + ENEMY_PADDING),
                                y * (enemyTexture.Height + ENEMY_PADDING));

                            spriteBatch.DrawCentered(enemyTexture, enemyPosition, Color.LimeGreen);
                        }
                    }
                }

                // Draw the player lasers.
                foreach (var laserPosition in playerLasers)
                    spriteBatch.DrawCentered(laserTexture, laserPosition);

                // Draw the enemy lasers.
                foreach (var laserPosition in enemyLasers)
                    spriteBatch.DrawCentered(laserTexture, laserPosition);

                // Draw the player's score.
                spriteBatch.DrawString(scoreFont, "Score: " + playerScore, new Vector2(20, 20), Color.White);
            }
            else
            {
                // If the game is over, tell the player whether they won or lost.
                spriteBatch.DrawString(winFont, "Game over with a score of " + playerScore, new Vector2(20, 20), Color.White);

                if (playerWon)
                    spriteBatch.DrawString(winFont, "You won the game ^_^", new Vector2(20, 60), Color.White);
                else
                    spriteBatch.DrawString(winFont, "You died :(", new Vector2(20, 60), Color.White);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }


        #region Update functions


        private void CheckGameOver(float delta)
        {
            // Check if all enemies have been destroyed.
            bool clearedAllEnemies = true;
            for (int y = 0; y < enemyGrid.GetLength(1); y++)
                for (int x = 0; x < enemyGrid.GetLength(0); x++)
                    if (enemyGrid[x, y])
                        clearedAllEnemies = false;

            if (clearedAllEnemies)
            {
                gameOver = true;
                playerWon = true;
            }

            // Check if the lowest line of enemies have reached the player.
            float bottomMostPosition = enemyGridOffset.Y + (GetBottomMostEnemy() * (enemyTexture.Height + ENEMY_PADDING));
            if (bottomMostPosition + ENEMY_PADDING > playerPosition.Y)
                gameOver = true;
        }


        private void CheckPlayerInput(float delta)
        {
            KeyboardState keyboard = Keyboard.GetState();

            Rectangle playerBounds = GetBoundingBox(playerPosition, playerTexture);

            // Check if the player wants to move left.
            if (keyboard.IsKeyDown(Keys.Left) && playerBounds.Left > 0)
                playerPosition.X -= 300 * delta;

            // Check if the player wants to move right.
            if (keyboard.IsKeyDown(Keys.Right) && playerBounds.Right < WINDOW_WIDTH)
                playerPosition.X += 300 * delta;

            // Check if the player wants to fire a laser.
            if (keyboard.IsKeyDown(Keys.Space) && timeSincePlayerLaser > PLAYER_LASER_COOLDOWN)
            {
                playerLasers.Add(playerPosition + new Vector2(0, -laserTexture.Height));
                timeSincePlayerLaser = 0;
            }

            timeSincePlayerLaser += delta;
        }


        private void UpdateEnemies(float delta)
        {
            // 1% chance of enemy laser fire.
            if (rand.Next(100) < 1)
            {
                // Randomly select a column (x axis).
                int rightMostEnemy = GetRightMostEnemy();
                int selectedPosX = rand.Next(rightMostEnemy+1);

                // Get the lowest enemy in that column.
                int selectedPosY = GetBottomMostEnemyInColumn(selectedPosX);

                Vector2 enemyPosition = enemyGridOffset + new Vector2(
                    selectedPosX * (enemyTexture.Width + ENEMY_PADDING),
                    selectedPosY * (enemyTexture.Height + ENEMY_PADDING));

                enemyLasers.Add(enemyPosition);
            }

            // Enemies should move once every $ENEMY_MOVE_INTERVAL seconds.
            if (timeSinceEnemyMove > ENEMY_MOVE_INTERVAL)
            {
                int offsetX = enemyTexture.Width + ENEMY_PADDING;
                int offsetY = enemyTexture.Height + ENEMY_PADDING;

                if (nextEnemyMovement == EnemyMovement.Down)
                {
                    enemyGridOffset.Y += offsetY;

                    // Check if the grid has reached the left side of the screen.
                    float leftMostPosition = enemyGridOffset.X + (GetLeftMostEnemy() * (enemyTexture.Width + ENEMY_PADDING));
                    if (enemyGridOffset.X < Math.Abs(offsetX*2))
                        nextEnemyMovement = EnemyMovement.Right;

                    // Check if the grid has reached the right side of the screen.
                    float rightMostPosition = enemyGridOffset.X + (GetRightMostEnemy() * (enemyTexture.Width + ENEMY_PADDING));
                    if (rightMostPosition > WINDOW_WIDTH - Math.Abs(offsetX*2))
                        nextEnemyMovement = EnemyMovement.Left;
                }
                else
                {
                    // Move enemies on the x axis.
                    if (nextEnemyMovement == EnemyMovement.Left)
                        offsetX *= -1;
                    enemyGridOffset.X += offsetX;

                    // Check if the grid has reached the left or right side of the screen.
                    float leftMostPosition = enemyGridOffset.X + (GetLeftMostEnemy() * (enemyTexture.Width + ENEMY_PADDING));
                    float rightMostPosition = enemyGridOffset.X + (GetRightMostEnemy() * (enemyTexture.Width + ENEMY_PADDING));
                    if (leftMostPosition < Math.Abs(offsetX*2) || rightMostPosition > WINDOW_WIDTH - Math.Abs(offsetX*2))
                        nextEnemyMovement = EnemyMovement.Down;
                }

                // Move enemies on the y axis.

                timeSinceEnemyMove = 0;
            }

            timeSinceEnemyMove += delta;
        }


        private void UpdateLasers(float delta)
        {
            // Update each player laser.
            for (int i = playerLasers.Count - 1; i >= 0; i--)
                UpdatePlayerLaser(delta, i);

            // Update each enemy laser.
            for (int i = enemyLasers.Count - 1; i >= 0; i--)
                UpdateEnemyLaser(delta, i);
        }


        private void UpdatePlayerLaser(float delta, int i)
        {
            // Player lasers move upwards.
            Vector2 temp = playerLasers[i];
            temp.Y -= 300 * delta;
            playerLasers[i] = temp;

            // Remove the laser when it reaches the top of the screen.
            Rectangle laserBounds = GetBoundingBox(playerLasers[i], laserTexture);
            if (laserBounds.Bottom < 0)
            {
                playerLasers.RemoveAt(i);
                return;
            }

            // Check if the laser collides with any active enemies.
            for (int y = 0; y < enemyGrid.GetLength(1); y++)
            {
                for (int x = 0; x < enemyGrid.GetLength(0); x++)
                {
                    if (enemyGrid[x, y])
                    {
                        Vector2 enemyPosition = enemyGridOffset + new Vector2(
                            x * (enemyTexture.Width + ENEMY_PADDING),
                            y * (enemyTexture.Height + ENEMY_PADDING));

                        Rectangle enemyBounds = GetBoundingBox(enemyPosition, enemyTexture);
                        if (laserBounds.Intersects(enemyBounds))
                        {
                            enemyGrid[x, y] = false;
                            playerLasers.RemoveAt(i);
                            playerScore += 10;
                            return;
                        }
                    }
                }
            }
        }


        private void UpdateEnemyLaser(float delta, int i)
        {
            // Enemy lasers move downwards.
            var temp = enemyLasers[i];
            temp.Y += 300 * delta;
            enemyLasers[i] = temp;

            // Check if the laser hits the player.
            Rectangle laserBounds = GetBoundingBox(enemyLasers[i], laserTexture);
            Rectangle playerBounds = GetBoundingBox(playerPosition, playerTexture);
            if (laserBounds.Intersects(playerBounds))
            {
                gameOver = true;
                enemyLasers.RemoveAt(i);
                return;
            }

            // Remove the laser when it reaches the bottom of the screen.
            if (laserBounds.Top > WINDOW_HEIGHT)
            {
                enemyLasers.RemoveAt(i);
                return;
            }
        }


        #endregion


        #region Misc functions


        /// <summary>
        /// Get the x index of the left-most enemy still alive on the grid.
        /// </summary>
        private int GetLeftMostEnemy()
        {
            int result = enemyGrid.GetLength(0) - 1;

            for (int y = 0; y < enemyGrid.GetLength(1); y++)
                for (int x = 0; x < enemyGrid.GetLength(0); x++)
                    if (enemyGrid[x, y] && x < result)
                        result = x;

            return result;
        }


        /// <summary>
        /// Get the x index of the right-most enemy still alive on the grid.
        /// </summary>
        private int GetRightMostEnemy()
        {
            int result = 0;

            for (int y = 0; y < enemyGrid.GetLength(1); y++)
                for (int x = 0; x < enemyGrid.GetLength(0); x++)
                    if (enemyGrid[x, y] && x > result)
                        result = x;

            return result;
        }


        /// <summary>
        /// Get the y index of the bottom-most enemy still alive on the grid.
        /// </summary>
        private int GetBottomMostEnemy()
        {
            int result = 0;

            for (int y = 0; y < enemyGrid.GetLength(1); y++)
                for (int x = 0; x < enemyGrid.GetLength(0); x++)
                    if (enemyGrid[x, y] && y > result)
                        result = y;

            return result;
        }


        /// <summary>
        /// Get the y index of the bottom-most enemy still alive on the specified column of the grid.
        /// </summary>
        private int GetBottomMostEnemyInColumn(int x)
        {
            int result = 0;

            for (int y = 0; y < enemyGrid.GetLength(1); y++)
                if (enemyGrid[x, y] && y > result)
                    result = y;

            return result;
        }


        private Rectangle GetBoundingBox(Vector2 position, Texture2D texture)
        {
            // Textures are rendered with the center of the texture as the origin, so we need to
            // subtract half of the texture's width and height from the position in order to get
            // the top-left corner of the bounding box.
            return new Rectangle(
                (int)(position.X - (texture.Width / 2f)),
                (int)(position.Y - (texture.Height / 2f)),
                texture.Width,
                texture.Height);
        }

        #endregion
    }


    static class Ext
    {
        /// <summary>
        /// Draws a texture with the origin set to the center of the texture.
        /// </summary>
        public static void DrawCentered(this SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color? color=null)
        {
            spriteBatch.Draw(
                texture,
                position,
                null,
                (color.HasValue ? color.Value : Color.White),
                0f,
                new Vector2(texture.Width / 2f, texture.Height / 2f),
                1f,
                SpriteEffects.None,
                1f);
        }
    }
}
